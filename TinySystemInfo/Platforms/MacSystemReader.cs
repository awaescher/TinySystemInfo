using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

public class MacSystemReader : ISystemReader
{
    public static string CpuUsageCommand { get; } = "top -l 1 -s 0 -n 0 | grep -i CPU";
    public static string CpuUsageSampledCommand { get; } = "top -l 2 -s 1 -n 0 | grep -i 'CPU usage'";
    public static string OsNameCommand { get; } = "sw_vers -productName";
    public static string OsVersionCommand { get; } = "sw_vers -productVersion";
    public static string VmStatCommand { get; } = "vm_stat";
    public static string TotalMemoryCommand { get; } = "sysctl -n hw.memsize";
    public static string PageSizeCommand { get; } = "pagesize";
    public static string VolumesCommand { get; } = "df -k /Volumes/* | grep /dev/disk";

    public ICli Cli { get; set; } = new BashCli();

    [SupportedOSPlatform("osx")]
    public async Task<SystemInfo> Read(TimeSpan delay)
    {
        // For macOS, we use top with 2 samples to get accurate CPU measurements
        float cpuUsage = GetCpuUsageFromTopSampled();
        var memoryInfo = GetMemoryInfo();

        return new SystemInfo(
            HostName: Environment.MachineName,
            OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
            OSName: GetOsName(),
            OSVersion: GetOsVersion(),
            CpuUsagePercent: cpuUsage,
            CpuCount: Environment.ProcessorCount,
			Memory: new Memory(TotalBytes: memoryInfo.TotalBytes, UsedBytes: memoryInfo.TotalBytes - memoryInfo.FreeBytes),
            Volumes: GetVolumes().Select(v => new Volume(Mount: v.Mount, TotalBytes: v.TotalBytes, UsedBytes: v.TotalBytes - v.FreeBytes))
        );
    }

    public float GetCpuUsage()
    {
        var output = Cli.Run(CpuUsageCommand);

        var regex = new Regex($"{FloatParser.FloatPattern}% idle");
        var match = regex.Match(output);

        if (match.Success)
            return 100.0f - FloatParser.Parse(match.Groups[1].Value);  // CPU usage as 100 - %idle

        return 0;
    }

    public float GetCpuUsageFromTopSampled()
    {
        try
        {
            // top -l 2 -s 1 gives us 2 CPU samples with 1 second interval
            // The second sample is the average CPU usage over that interval
            var output = Cli.Run(CpuUsageSampledCommand);
            
            // Output looks like:
            // CPU usage: 5.80% user, 7.15% sys, 87.4% idle 
            // CPU usage: 4.28% user, 2.48% sys, 93.22% idle
            
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // Get the last line (second sample)
            if (lines.Length >= 2)
            {
                var lastLine = lines[lines.Length - 1];
                
                var regex = new Regex($"{FloatParser.FloatPattern}% idle");
                var match = regex.Match(lastLine);
                
                if (match.Success)
                {
                    float idlePercent = FloatParser.Parse(match.Groups[1].Value);
                    return 100.0f - idlePercent;
                }
            }
            
            // Fallback to single sample
            return GetCpuUsage();
        }
        catch
        {
            // Fallback to single sample
            return GetCpuUsage();
        }
    }

    public float GetCpuUsageFromIoStat()
    {
        try
        {
            // Legacy method for backward compatibility with tests
            // iostat -c 2 -w 1 gives us 2 CPU samples with 1 second interval
            var output = Cli.Run("iostat -c 2 -w 1");
            
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // Find the last line with CPU data (us sy id columns)
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 3)
                {
                    for (int j = parts.Length - 3; j >= 0; j--)
                    {
                        if (int.TryParse(parts[j], out int us) && 
                            int.TryParse(parts[j + 1], out int sy) && 
                            int.TryParse(parts[j + 2], out int id))
                        {
                            return 100.0f - id;
                        }
                    }
                }
            }
            
            return GetCpuUsage();
        }
        catch
        {
            return GetCpuUsage();
        }
    }

    public CpuInfo GetCpuInfo()
    {
        try
        {
            // Fallback method using top output
            var topOutput = Cli.Run(CpuUsageCommand);
            
            // Parse idle percentage from top output
            var regex = new Regex($"{FloatParser.FloatPattern}% idle");
            var match = regex.Match(topOutput);
            
            if (match.Success)
            {
                float idlePercent = FloatParser.Parse(match.Groups[1].Value);
                // Convert percentage to ticks (using arbitrary scale of 10000)
                long idleTicks = (long)(idlePercent * 100);
                long totalTicks = 10000;
                return new CpuInfo(IdleTime: idleTicks, TotalTime: totalTicks);
            }
            
            return new CpuInfo(IdleTime: 0, TotalTime: 0);
        }
        catch
        {
            return new CpuInfo(IdleTime: 0, TotalTime: 0);
        }
    }

    public float CalculateCpuUsage(CpuInfo before, CpuInfo after)
    {
        long totalDelta = after.TotalTime - before.TotalTime;
        long idleDelta = after.IdleTime - before.IdleTime;
        
        if (totalDelta == 0)
            return 0;
        
        return 100.0f * (1.0f - (float)idleDelta / totalDelta);
    }

    public MemoryInfo GetMemoryInfo()
    {
        try
        {
            long totalMemory = long.Parse(Cli.Run(TotalMemoryCommand));
            
            // Get page size (typically 4096 or 16384 bytes on Apple Silicon)
            long pageSize = long.Parse(Cli.Run(PageSizeCommand));
            
            // Parse vm_stat output
            var vmStatOutput = Cli.Run(VmStatCommand);
            
            // vm_stat output looks like:
            // Pages free:                               123456.
            // Pages active:                             234567.
            // Pages inactive:                           345678.
            // Pages speculative:                         12345.
            // Pages wired down:                         456789.
            // ...
            
            long pagesFree = ParseVmStatValue(vmStatOutput, "Pages free:");
            long pagesActive = ParseVmStatValue(vmStatOutput, "Pages active:");
            long pagesInactive = ParseVmStatValue(vmStatOutput, "Pages inactive:");
            long pagesSpeculative = ParseVmStatValue(vmStatOutput, "Pages speculative:");
            long pagesWired = ParseVmStatValue(vmStatOutput, "Pages wired down:");
            long pagesCompressed = ParseVmStatValue(vmStatOutput, "Pages occupied by compressor:");
            
            // Free memory = free + inactive + speculative pages
            // Used memory = active + wired + compressed pages
            long freeBytes = (pagesFree + pagesInactive + pagesSpeculative) * pageSize;
            
            // Ensure we don't exceed total memory
            freeBytes = Math.Min(freeBytes, totalMemory);
            
            return new MemoryInfo(TotalBytes: totalMemory, FreeBytes: freeBytes);
        }
        catch
        {
            // Fallback to basic total memory if vm_stat fails
            try
            {
                long totalMemory = long.Parse(Cli.Run(TotalMemoryCommand));
                return new MemoryInfo(TotalBytes: totalMemory, FreeBytes: 0);
            }
            catch
            {
                return new MemoryInfo(TotalBytes: 0, FreeBytes: 0);
            }
        }
    }
    
    private long ParseVmStatValue(string vmStatOutput, string key)
    {
        var lines = vmStatOutput.Split('\n');
        var line = lines.FirstOrDefault(l => l.Contains(key));
        
        if (line == null)
            return 0;
        
        // Extract the number, removing the trailing dot
        var parts = line.Split(':');
        if (parts.Length < 2)
            return 0;
        
        var valueStr = parts[1].Trim().TrimEnd('.');
        
        if (long.TryParse(valueStr, out long value))
            return value;
        
        return 0;
    }

    public IEnumerable<VolumeInfo> GetVolumes()
    {
        // Conversion factor from binary gigabyte scaling (e.g. 2^30) to decimal gigabyte scaling (10^9)
        // This is used to normalize the '1024-blocks' output which uses 2^30 blocks to a decimal GB scale.
        const double BIN_TO_DEC_GIGA_SCALE = 1.073741824d;

        string output;
        try
        {
            output = Cli.Run(VolumesCommand);
        }
        catch
        {
            // Return empty collection if df command fails
            yield break;
        }

        // output looks like this (without header row)
        // Filesystem     1024-blocks       Used Available Capacity iused      ifree %iused  Mounted on
        // /dev/disk3s1s1   971350180   10988752 748049596     2%  425798 4294134754    0%   /
        // /dev/disk7s1    1953309744 1135425672 817591264    59%  344104 8175912640    0%   /Volumes/MyExternalDrive

        foreach (var line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 4)
            {
                VolumeInfo? volume = null;
                try
                {
                    volume = new VolumeInfo(
                        Mount: parts.Last(),
                        TotalBytes: (long)(double.Parse(parts[1]) * BIN_TO_DEC_GIGA_SCALE) * 1024L,
                        FreeBytes: (long)(double.Parse(parts[3]) * BIN_TO_DEC_GIGA_SCALE) * 1024L); // APFS will show invalid values for used bytes here, so use the free/available bytes
                }
                catch
                {
                    // Skip volumes that cannot be parsed
                    continue;
                }
                
                if (volume != null)
                    yield return volume;
            }
        }
    }

    public string GetOsName() => Cli.Run(OsNameCommand);

    public string GetOsVersion() => Cli.Run(OsVersionCommand);

    public record CpuInfo(long IdleTime, long TotalTime);

    public record MemoryInfo(long TotalBytes, long FreeBytes);

    public record VolumeInfo(string Mount, long TotalBytes, long FreeBytes);
}