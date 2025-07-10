using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace TinySystemInfo.Platforms;

internal class LinuxSystemReader : ISystemReader
{
	[SupportedOSPlatform("linux")]
	public async Task<SystemInfo> Read()
	{
		var cpuUsage = GetCpuUsage();
		var memoryInfo = await GetMemoryInfo();

		return new SystemInfo(
			HostName: Environment.MachineName,
			OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
			OSName: ParseOsInfo("NAME"),
			OSVersion: ParseOsInfo("VERSION_ID"),
			CpuUsagePercent: cpuUsage,
			CpuCount: Environment.ProcessorCount,
			RamTotalBytes: memoryInfo.TotalMemory,
			RamAvailableBytes: memoryInfo.FreeMemory
		);
	}

	private string ParseOsInfo(string key)
	{
		var os = File.ReadAllText("/etc/os-release");
		var match = Regex.Match(os, key + "=(.*)");
		return match.Groups[1].Value.Trim().TrimStart('\"').TrimEnd('\"');
	}

	private float GetCpuUsage()
	{
        var output = ExecuteBashCommand("top -b -n 1 | grep -i %CPU");

        var regex = new Regex($"{FloatParser.FloatPattern} id");
        var match = regex.Match(output);

        if (match.Success)
            return 100.0f - FloatParser.Parse(match.Groups[1].Value);  // CPU usage as 100 - %idle

        return 0;
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
			return result.Trim();
		}
	}

	public record CpuInfo(long IdleTime, long TotalTime);

	public record MemoryInfo(long TotalMemory, long FreeMemory);
}