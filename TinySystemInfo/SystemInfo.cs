namespace TinySystemInfo;

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

public record Volume(string Mount, long TotalBytes, long UsedBytes) : Memory(TotalBytes: TotalBytes, UsedBytes: UsedBytes);

public record Memory(long TotalBytes, long UsedBytes)
{
	public long FreeBytes => TotalBytes - UsedBytes;

	public float Usage => (float)UsedBytes / (float)TotalBytes * 100.0f;
}