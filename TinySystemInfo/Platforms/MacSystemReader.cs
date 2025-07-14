using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

public class MacSystemReader : ISystemReader
{
    public static string CpuUsageCommand { get; } = "top -l 1 -s 0 -n 0 | grep -i CPU";
    public static string OsNameCommand { get; } = "sw_vers -productName";
    public static string OsVersionCommand { get; } = "sw_vers -productVersion";
    public static string FreeMemoryCommand { get; } = "sysctl -n kern.memorystatus_level";
    public static string TotalMemoryCommand { get; } = "sysctl -n hw.memsize";
    public static string VolumesCommand { get; } = "df -k /Volumes/* | grep /dev/disk";

    public ICli Cli { get; set; } = new BashCli();

    [SupportedOSPlatform("osx")]
    public async Task<SystemInfo> Read(TimeSpan delay)
    {
        await Task.Delay(delay);

        var cpuUsage = GetCpuUsage();
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

    public MemoryInfo GetMemoryInfo()
    {
        var memoryPressureFree = long.Parse(Cli.Run(FreeMemoryCommand));
        long totalMemory = long.Parse(Cli.Run(TotalMemoryCommand));
        return new MemoryInfo(TotalBytes: totalMemory, FreeBytes: totalMemory / 100 * memoryPressureFree);
    }

    public IEnumerable<VolumeInfo> GetVolumes()
    {
        // Conversion factor from binary gigabyte scaling (e.g. 2^30) to decimal gigabyte scaling (10^9)
        // This is used to normalize the '1024-blocks' output which uses 2^30 blocks to a decimal GB scale.
        const double BIN_TO_DEC_GIGA_SCALE = 1.073741824d;

        var output = Cli.Run(VolumesCommand);

        // output looks like this (without header row)
        // Filesystem     1024-blocks       Used Available Capacity iused      ifree %iused  Mounted on
        // /dev/disk3s1s1   971350180   10988752 748049596     2%  425798 4294134754    0%   /
        // /dev/disk7s1    1953309744 1135425672 817591264    59%  344104 8175912640    0%   /Volumes/MyExternalDrive

        foreach (var line in output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 4)
            {
                yield return new VolumeInfo(
                    Mount: parts.Last(),
                    TotalBytes: (long)(double.Parse(parts[1]) * BIN_TO_DEC_GIGA_SCALE) * 1024L,
                    FreeBytes: (long)(double.Parse(parts[3]) * BIN_TO_DEC_GIGA_SCALE) * 1024L); // APFS will show invalid values for used bytes here, so use the free/available bytes
            }
        }
    }

    public string GetOsName() => Cli.Run(OsNameCommand);

    public string GetOsVersion() => Cli.Run(OsVersionCommand);

    public record CpuInfo(long IdleTime, long TotalTime);

    public record MemoryInfo(long TotalBytes, long FreeBytes);

    public record VolumeInfo(string Mount, long TotalBytes, long FreeBytes);
}