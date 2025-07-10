using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    public ICli Cli { get; set; } = new BashCli();

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

        var freeMemory = totalMemory / 100 * memoryPressureFree;

        return new MemoryInfo(TotalMemory: totalMemory, FreeMemory: (long)freeMemory);
    }

    public string GetOsName() => Cli.Run(OsNameCommand);

    public string GetOsVersion() => Cli.Run(OsVersionCommand);

    public record CpuInfo(long IdleTime, long TotalTime);

    public record MemoryInfo(long TotalMemory, long FreeMemory);
}