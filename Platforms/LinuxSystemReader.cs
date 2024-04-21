using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinySystemInfo.Platforms;

internal class LinuxSystemReader : ISystemReader
{
	[SupportedOSPlatform("linux")]
	public async Task<SystemInfo> Read()
	{
		var cpuUsage = await GetCpuUsage();
		var memoryInfo = await GetMemoryInfo();

		return new SystemInfo(
			HostName: Environment.MachineName,
			OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
			OSName: "Linux",
			OSVersion: File.ReadAllText("/proc/version"),
			CpuUsagePercent: cpuUsage,
			CpuCount: Environment.ProcessorCount,
			RamTotalBytes: memoryInfo.TotalMemory,
			RamAvailableBytes: memoryInfo.FreeMemory
		);
	}

	private async Task<float> GetCpuUsage()
	{
		var firstMeasure = ReadCpuMetrics();
		await Task.Delay(1000);
		var secondMeasure = ReadCpuMetrics();

		var idleTime = secondMeasure.IdleTime - firstMeasure.IdleTime;
		var totalTime = secondMeasure.TotalTime - firstMeasure.TotalTime;

		return (float)(100 * (1 - idleTime / totalTime));
	}

	private CpuInfo ReadCpuMetrics()
	{
		string firstLine = File.ReadLines("/proc/stat").First();
		var values = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(long.Parse).ToArray();
		long idleTime = values[3] + values[4]; // idle and iowait
		long totalTime = values.Sum();

		return new CpuInfo(IdleTime: idleTime, TotalTime: totalTime);
	}

	private async Task<MemoryInfo> GetMemoryInfo()
	{
		string memInfo = await File.ReadAllTextAsync("/proc/meminfo");
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