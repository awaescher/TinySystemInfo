using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

public class LinuxSystemReader : ISystemReader
{
	public static string CpuUsageCommand { get; } = "top -b -n 1 | grep -i %CPU";
	public static string OsInfoCommand { get; } = "cat /etc/os-release";
	public static string MemoryInfoCommand { get; } = "cat /proc/meminfo";

	public ICli Cli { get; set; } = new BashCli();

	[SupportedOSPlatform("linux")]
    public async Task<SystemInfo> Read(TimeSpan delay)
    {
        await Task.Delay(delay);

		var cpuUsage = GetCpuUsage();
		var memoryInfo = GetMemoryInfo();
		var osInfo = GetOsInfo();

		return new SystemInfo(
			HostName: Environment.MachineName,
			OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
			OSName: GetOsName(osInfo),
			OSVersion: GetOsVersion(osInfo),
			CpuUsagePercent: cpuUsage,
			CpuCount: Environment.ProcessorCount,
			RamTotalBytes: memoryInfo.TotalMemory,
			RamAvailableBytes: memoryInfo.FreeMemory
		);
	}

	public string GetOsInfo() => Cli.Run(OsInfoCommand);

	public string GetOsName(string osInfo) => ParseOsInfo(osInfo, "NAME");

	public string GetOsVersion(string osInfo) => ParseOsInfo(osInfo, "VERSION_ID");

	private string ParseOsInfo(string osInfo, string key)
	{
		var match = Regex.Match(osInfo, key + "=(.*)");
		return match.Groups[1].Value.Trim().TrimStart('\"').TrimEnd('\"');
	}

	public float GetCpuUsage()
	{
        var output = Cli.Run(CpuUsageCommand);

        var regex = new Regex($"{FloatParser.FloatPattern} id");
        var match = regex.Match(output);

        if (match.Success)
            return 100.0f - FloatParser.Parse(match.Groups[1].Value);  // CPU usage as 100 - %idle

        return 0;
	}

	public MemoryInfo GetMemoryInfo()
	{
		string memInfo = Cli.Run(MemoryInfoCommand);
		long totalMemory = ParseMemoryInfo(memInfo, "MemTotal:");
		long freeMemory = ParseMemoryInfo(memInfo, "MemFree:");

		return new MemoryInfo(TotalMemory: totalMemory, FreeMemory: freeMemory);
	}

	private long ParseMemoryInfo(string memInfo, string key)
	{
		var line = memInfo.Split('\n').FirstOrDefault(l => l.Contains(key));
		if (line != null)
		{
			var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (long.TryParse(parts[1], out long value))
				return value * 1024; // Convert from kB to Bytes
		}
		return 0;
	}

	public record CpuInfo(long IdleTime, long TotalTime);

	public record MemoryInfo(long TotalMemory, long FreeMemory);
}