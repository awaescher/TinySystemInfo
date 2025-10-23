using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

public class LinuxSystemReader : ISystemReader
{
	public static string CpuStatCommand { get; } = "cat /proc/stat";
	public static string OsInfoCommand { get; } = "cat /etc/os-release";
	public static string MemoryInfoCommand { get; } = "cat /proc/meminfo";
	public static string VolumesCommand { get; } = "df | grep -E '^/dev/(sd|vd|nvme|hd)'";

	public ICli Cli { get; set; } = new BashCli();

	[SupportedOSPlatform("linux")]
	public async Task<SystemInfo> Read(TimeSpan delay)
	{
		var cpuInfo1 = GetCpuInfo();
		await Task.Delay(delay);
		var cpuInfo2 = GetCpuInfo();
		
		var cpuUsage = CalculateCpuUsage(cpuInfo1, cpuInfo2);
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

	public string GetOsName(string osInfo) => ParseOsInfo(osInfo, "PRETTY_NAME");

	public string GetOsVersion(string osInfo) => ParseOsInfo(osInfo, "VERSION_ID");

	private string ParseOsInfo(string osInfo, string key)
	{
		var match = Regex.Match(osInfo, key + "=(.*)");
		return match.Groups[1].Value.Trim().TrimStart('\"').TrimEnd('\"');
	}

	public CpuInfo GetCpuInfo()
	{
		try
		{
			var output = Cli.Run(CpuStatCommand);
			
			// Parse the first line which contains aggregate CPU stats
			// Format: cpu user nice system idle iowait irq softirq steal guest guest_nice
			var lines = output.Split('\n');
			var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));
			
			if (cpuLine == null)
				return new CpuInfo(IdleTime: 0, TotalTime: 0);
			
			var parts = cpuLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 5)
				return new CpuInfo(IdleTime: 0, TotalTime: 0);
			
			// cpu values are: user, nice, system, idle, iowait, irq, softirq, steal, guest, guest_nice
			long user = long.Parse(parts[1]);
			long nice = long.Parse(parts[2]);
			long system = long.Parse(parts[3]);
			long idle = long.Parse(parts[4]);
			long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
			long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
			long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
			long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;
			
			long totalTime = user + nice + system + idle + iowait + irq + softirq + steal;
			long idleTime = idle + iowait;
			
			return new CpuInfo(IdleTime: idleTime, TotalTime: totalTime);
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

	public float GetCpuUsage()
	{
		// Legacy method for backward compatibility with tests
		// This uses a simple top-based approach which is less accurate
		var output = Cli.Run("top -b -n 1 | grep -i %CPU");

		var regex = new Regex($"{FloatParser.FloatPattern} id");
		var match = regex.Match(output);

		if (match.Success)
			return 100.0f - FloatParser.Parse(match.Groups[1].Value);  // CPU usage as 100 - %idle

		return 0;
	}

	public MemoryInfo GetMemoryInfo()
	{
		try
		{
			string memInfo = Cli.Run(MemoryInfoCommand);
			long totalMemory = ParseMemoryInfo(memInfo, "MemTotal:");
			long freeMemory = ParseMemoryInfo(memInfo, "MemFree:");

			return new MemoryInfo(TotalBytes: totalMemory, FreeBytes: freeMemory);
		}
		catch
		{
			return new MemoryInfo(TotalBytes: 0, FreeBytes: 0);
		}
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
		// /dev/vda2       23509104 13606096   8683476   62% /
		// /dev/vda1        1098632     6516   1092116    1% /boot/efi";

		foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 3)
			{
				VolumeInfo? volume = null;
				try
				{
					volume = new VolumeInfo(
						Mount: parts.Last(),
						TotalBytes: (long)(double.Parse(parts[1]) * BIN_TO_DEC_GIGA_SCALE) * 1024L,
						UsedBytes: (long)(double.Parse(parts[2]) * BIN_TO_DEC_GIGA_SCALE) * 1024L);
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

	public record CpuInfo(long IdleTime, long TotalTime);

	public record MemoryInfo(long TotalBytes, long FreeBytes);
	
    public record VolumeInfo(string Mount, long TotalBytes, long UsedBytes);

}