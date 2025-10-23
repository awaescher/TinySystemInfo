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
    public async Task<SystemInfo> Read(TimeSpan delay)
    {
		float cpuUsage = 0;

		using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
		{
			_ = cpuCounter.NextValue(); // discard first measure
			
			// Use the provided delay or at least 500ms for accurate CPU measurement
			var measurementDelay = delay.TotalMilliseconds < 500 ? TimeSpan.FromMilliseconds(500) : delay;
			await Task.Delay(measurementDelay);

			cpuUsage = cpuCounter.NextValue();
		}

		var memStatus = new MEMORYSTATUSEX();
		GlobalMemoryStatusEx(memStatus);

		var drives = DriveInfo.GetDrives()
			.Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
			.Select(d => new Volume(Mount: d.Name, TotalBytes: d.TotalSize, UsedBytes: d.TotalSize - d.AvailableFreeSpace))
			.ToArray();

		return new SystemInfo(
			HostName: Environment.MachineName,
			OSArchitecture: RuntimeInformation.OSArchitecture.ToString(),
			OSName: "Microsoft Windows",
			OSVersion: Environment.OSVersion.Version.ToString(),
			CpuUsagePercent: cpuUsage,
			CpuCount: Environment.ProcessorCount,
			Memory: new Memory(TotalBytes: (long)memStatus.ullTotalPhys, UsedBytes: (long)memStatus.ullTotalPhys - (long)memStatus.ullAvailPhys),
			Volumes: drives
		);
	}

}