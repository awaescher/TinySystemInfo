using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

public class MacSystemReader : ISystemReader
{
    [SupportedOSPlatform("osx")]
    public async Task<SystemInfo> Read()
    {
        await Task.Yield();

        var cpuUsage = GetCpuUsage();
        var memoryInfo = GetMemoryInfo();

        return new SystemInfo(
            HostName: Environment.MachineName,
            OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
            OSName: "macOS",
            OSVersion: GetOsVersion(),
            CpuUsagePercent: cpuUsage,
            CpuCount: Environment.ProcessorCount,
            RamTotalBytes: memoryInfo.TotalMemory,
            RamAvailableBytes: memoryInfo.FreeMemory
        );
    }

    private float GetCpuUsage()
    {
        string output = ExecuteBashCommand("top -l 1 -s 0 -n 0 | grep CPU");

        var regex = new Regex(@"(\d+\.\d+)% idle");
        var match = regex.Match(output);

        if (match.Success)
            return 100.0f - float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);  // CPU usage as 100 - %idle

        return 0;
    }

    private MemoryInfo GetMemoryInfo()
    {
        var memoryPressureFree = float.Parse(ExecuteBashCommand("sysctl -n kern.memorystatus_level"), System.Globalization.CultureInfo.InvariantCulture);
        long totalMemory = long.Parse(ExecuteBashCommand("sysctl -n hw.memsize"), System.Globalization.CultureInfo.InvariantCulture);

        var freeMemory = totalMemory / 100 * memoryPressureFree;

        return new MemoryInfo(TotalMemory: totalMemory, FreeMemory: (long)freeMemory);
    }


    private string GetOsVersion() => ExecuteBashCommand("sw_vers -productVersion");

    private string ExecuteBashCommand(string command)
    {
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }

    public record CpuInfo(long IdleTime, long TotalTime);

    public record MemoryInfo(long TotalMemory, long FreeMemory);
}