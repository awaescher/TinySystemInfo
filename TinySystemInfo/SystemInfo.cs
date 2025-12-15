namespace TinySystemInfo;

/// <summary>
/// Represents comprehensive system information including hardware, operating system, and resource usage details.
/// </summary>
/// <param name="HostName">The network hostname of the system.</param>
/// <param name="OSArchitecture">The processor architecture (e.g., X64, Arm64).</param>
/// <param name="OSName">The operating system name (e.g., "Ubuntu 22.04", "macOS", "Microsoft Windows").</param>
/// <param name="OSVersion">The operating system version string.</param>
/// <param name="CpuUsagePercent">The current CPU usage as a percentage (0-100).</param>
/// <param name="CpuCount">The number of logical CPU cores available.</param>
/// <param name="Memory">Memory usage information including total and used bytes.</param>
/// <param name="Volumes">Collection of mounted storage volumes with their usage information.</param>
public record SystemInfo(
	string HostName,
	string OSArchitecture,
	string OSName,
	string OSVersion,
	float CpuUsagePercent,
	int CpuCount,
	Memory Memory,
	IEnumerable<Volume> Volumes
)
{ 
}

/// <summary>
/// Represents a storage volume with mount point and space usage information.
/// </summary>
/// <param name="Mount">The mount point path of the volume (e.g., "/", "C:\", "/Volumes/Data").</param>
/// <param name="TotalBytes">The total capacity of the volume in bytes.</param>
/// <param name="UsedBytes">The number of bytes currently used on the volume.</param>
public record Volume(string Mount, long TotalBytes, long UsedBytes) : Memory(TotalBytes: TotalBytes, UsedBytes: UsedBytes);

/// <summary>
/// Represents memory usage information with total and used bytes.
/// </summary>
/// <param name="TotalBytes">The total amount of memory in bytes.</param>
/// <param name="UsedBytes">The amount of memory currently in use in bytes.</param>
public record Memory(long TotalBytes, long UsedBytes)
{
	/// <summary>
	/// Gets the amount of free memory in bytes.
	/// </summary>
	public long FreeBytes => TotalBytes - UsedBytes;

	/// <summary>
	/// Gets the memory usage as a percentage (0-100).
	/// </summary>
	public float Usage => (float)UsedBytes / (float)TotalBytes * 100.0f;
}