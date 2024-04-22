using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinySystemInfo.Platforms;

public class WindowsSystemReader : ISystemReader
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class MEMORYSTATUSEX
	{
		public uint dwLength;
		public uint dwMemoryLoad;
		public ulong ullTotalPhys;
		public ulong ullAvailPhys;
		public ulong ullTotalPageFile;
		public ulong ullAvailPageFile;
		public ulong ullTotalVirtual;
		public ulong ullAvailVirtual;
		public ulong ullAvailExtendedVirtual;

		public MEMORYSTATUSEX()
		{
			this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

	[SupportedOSPlatform("windows")]
	public async Task<SystemInfo> Read()
	{
		var memStatus = new MEMORYSTATUSEX();
		GlobalMemoryStatusEx(memStatus);

		float cpuUsage = 0;

		using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
		{
			_ = cpuCounter.NextValue(); // dicard first measure
			await Task.Delay(1000); // wait to get a reliable value

			cpuUsage = cpuCounter.NextValue();
		}

		return new SystemInfo(
			HostName: Environment.MachineName,
			OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
			OSName: "Microsoft Windows",
			OSVersion: Environment.OSVersion.Version.ToString(),
			CpuUsagePercent: cpuUsage,
			CpuCount: Environment.ProcessorCount,
			RamTotalBytes: (long)memStatus.ullTotalPhys,
			RamAvailableBytes: (long)memStatus.ullAvailPhys
		);
	}

}