using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TinySystemInfo.Platforms;

/// <summary>
/// Provides system information reading capabilities for Windows operating systems.
/// </summary>
public class WindowsSystemReader : ISystemReader
{
	/// <summary>
	/// Represents extended memory status information for Windows systems.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class MEMORYSTATUSEX
	{
		/// <summary>
		/// The size of the structure, in bytes.
		/// </summary>
		public uint dwLength;
		
		/// <summary>
		/// A number between 0 and 100 that specifies the approximate percentage of physical memory that is in use.
		/// </summary>
		public uint dwMemoryLoad;
		
		/// <summary>
		/// The amount of actual physical memory, in bytes.
		/// </summary>
		public ulong ullTotalPhys;
		
		/// <summary>
		/// The amount of physical memory currently available, in bytes.
		/// </summary>
		public ulong ullAvailPhys;
		
		/// <summary>
		/// The current committed memory limit for the system or the current process, in bytes.
		/// </summary>
		public ulong ullTotalPageFile;
		
		/// <summary>
		/// The maximum amount of memory the current process can commit, in bytes.
		/// </summary>
		public ulong ullAvailPageFile;
		
		/// <summary>
		/// The size of the user-mode portion of the virtual address space of the calling process, in bytes.
		/// </summary>
		public ulong ullTotalVirtual;
		
		/// <summary>
		/// The amount of unreserved and uncommitted memory currently in the user-mode portion of the virtual address space, in bytes.
		/// </summary>
		public ulong ullAvailVirtual;
		
		/// <summary>
		/// Reserved. This value is always 0.
		/// </summary>
		public ulong ullAvailExtendedVirtual;

		/// <summary>
		/// Initializes a new instance of the MEMORYSTATUSEX class.
		/// </summary>
		public MEMORYSTATUSEX()
		{
			this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
		}
	}

	/// <summary>
	/// Retrieves information about the system's current usage of both physical and virtual memory.
	/// </summary>
	/// <param name="lpBuffer">A pointer to a MEMORYSTATUSEX structure that receives information about current memory availability.</param>
	/// <returns>If the function succeeds, the return value is true. If the function fails, the return value is false.</returns>
	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

	/// <inheritdoc />
	[SupportedOSPlatform("windows")]
    public async Task<SystemInfo> ReadAsync(SystemReaderOptions? options = default)
    {
		options ??= new SystemReaderOptions();

		float cpuUsage = 0;

		using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
		{
			_ = cpuCounter.NextValue(); // discard first measure
			await Task.Delay(options.DelayBetweenCpuMeasurements);
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