using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

/// <summary>
/// Provides system information reading capabilities for Linux operating systems.
/// </summary>
public class LinuxSystemReader : ISystemReader
{
	/// <summary>
	/// Gets the command used to read CPU statistics from /proc/stat.
	/// </summary>
	public static string CpuStatCommand { get; } = "cat /proc/stat";
	
	/// <summary>
	/// Gets the command used to read OS information from /etc/os-release.
	/// </summary>
	public static string OsInfoCommand { get; } = "cat /etc/os-release";
	
	/// <summary>
	/// Gets the command used to read memory information from /proc/meminfo.
	/// </summary>
	public static string MemoryInfoCommand { get; } = "cat /proc/meminfo";
	
	/// <summary>
	/// Gets the command used to list mounted disk volumes.
	/// </summary>
	public static string VolumesCommand { get; } = "df | grep -E '^/dev/(sd|vd|nvme|hd)'";

	/// <summary>
	/// Gets or sets the command-line interface used to execute shell commands.
	/// </summary>
	public ICli Cli { get; set; } = new BashCli();

	/// <inheritdoc />
	[SupportedOSPlatform("linux")]
	public async Task<SystemInfo> ReadAsync(SystemReaderOptions? options = default)
	{
		options ??= new SystemReaderOptions();

		var cpuInfo1 = GetCpuInfo();
		await Task.Delay(options.DelayBetweenCpuMeasurements);
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

	/// <summary>
	/// Retrieves the raw OS information from /etc/os-release.
	/// </summary>
	/// <returns>The contents of the /etc/os-release file.</returns>
	public string GetOsInfo() => Cli.Run(OsInfoCommand);

	/// <summary>
	/// Extracts the OS name from the OS information.
	/// </summary>
	/// <param name="osInfo">The raw OS information string from /etc/os-release.</param>
	/// <returns>The operating system name (e.g., "Ubuntu 22.04 LTS").</returns>
	public string GetOsName(string osInfo) => ParseOsInfo(osInfo, "PRETTY_NAME");

	/// <summary>
	/// Extracts the OS version from the OS information.
	/// </summary>
	/// <param name="osInfo">The raw OS information string from /etc/os-release.</param>
	/// <returns>The operating system version (e.g., "22.04").</returns>
	public string GetOsVersion(string osInfo) => ParseOsInfo(osInfo, "VERSION_ID");

	private string ParseOsInfo(string osInfo, string key)
	{
		var match = Regex.Match(osInfo, key + "=(.*)");
		return match.Groups[1].Value.Trim().TrimStart('\"').TrimEnd('\"');
	}

	/// <summary>
	/// Retrieves CPU timing information from /proc/stat.
	/// </summary>
	/// <returns>CPU information including idle time and total time in system ticks.</returns>
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

	/// <summary>
	/// Calculates CPU usage percentage from two CPU measurements taken at different times.
	/// </summary>
	/// <param name="before">The CPU information from the first measurement.</param>
	/// <param name="after">The CPU information from the second measurement.</param>
	/// <returns>The CPU usage as a percentage (0-100).</returns>
	public float CalculateCpuUsage(CpuInfo before, CpuInfo after)
	{
		long totalDelta = after.TotalTime - before.TotalTime;
		long idleDelta = after.IdleTime - before.IdleTime;
		
		if (totalDelta == 0)
			return 0;
		
		return 100.0f * (1.0f - (float)idleDelta / totalDelta);
	}

	/// <summary>
	/// Gets the current CPU usage using the top command (legacy method).
	/// </summary>
	/// <returns>The CPU usage as a percentage (0-100).</returns>
	/// <remarks>
	/// This is a legacy method maintained for backward compatibility with tests.
	/// It uses a simple top-based approach which is less accurate than the /proc/stat method.
	/// </remarks>
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

	/// <summary>
	/// Retrieves memory usage information from /proc/meminfo.
	/// </summary>
	/// <returns>Memory information including total and free bytes.</returns>
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

	/// <summary>
	/// Retrieves information about all mounted disk volumes.
	/// </summary>
	/// <returns>An enumerable collection of volume information for all mounted disks.</returns>
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

	/// <summary>
	/// Represents CPU timing information from /proc/stat.
	/// </summary>
	/// <param name="IdleTime">The cumulative idle time in system ticks.</param>
	/// <param name="TotalTime">The cumulative total time (all CPU states) in system ticks.</param>
	public record CpuInfo(long IdleTime, long TotalTime);

	/// <summary>
	/// Represents memory usage information.
	/// </summary>
	/// <param name="TotalBytes">The total physical memory in bytes.</param>
	/// <param name="FreeBytes">The available free memory in bytes.</param>
	public record MemoryInfo(long TotalBytes, long FreeBytes);
	
	/// <summary>
	/// Represents storage volume information.
	/// </summary>
	/// <param name="Mount">The mount point path of the volume.</param>
	/// <param name="TotalBytes">The total capacity of the volume in bytes.</param>
	/// <param name="UsedBytes">The number of bytes currently used on the volume.</param>
    public record VolumeInfo(string Mount, long TotalBytes, long UsedBytes);

}