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
	public static string VolumesCommand { get; } = "df | grep -E '^/dev/(sd|vd|nvme|hd)'";

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
			Memory: new Memory(TotalBytes: memoryInfo.TotalBytes, UsedBytes: memoryInfo.TotalBytes - memoryInfo.FreeBytes),
			Volumes: GetVolumes().Select(v => new Volume(Mount: v.Mount, TotalBytes: v.TotalBytes, UsedBytes: v.UsedBytes))
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

		return new MemoryInfo(TotalBytes: totalMemory, FreeBytes: freeMemory);
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

	public IEnumerable<VolumeInfo> GetVolumes()
	{
		var output = Cli.Run(VolumesCommand);

		// output looks like this (without header row)
		// Filesystem     1024-blocks       Used Available Capacity iused      ifree %iused  Mounted on
        // /dev/vda2       23509104 13606096   8683476   62% /
        // /dev/vda1        1098632     6516   1092116    1% /boot/efi";

		foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 3)
			{
				yield return new VolumeInfo(
					Mount: parts.Last(),
					TotalBytes: long.Parse(parts[1]) * 1024,
					UsedBytes: long.Parse(parts[2]) * 1024);
			}
		}
	}

	public record CpuInfo(long IdleTime, long TotalTime);

	public record MemoryInfo(long TotalBytes, long FreeBytes);
	
    public record VolumeInfo(string Mount, long TotalBytes, long UsedBytes);

}