namespace TinySystemInfo; 

public record SystemInfo(
	string HostName,
	string OSName,
	string OSVersion,
	float CpuUsagePercent,
	int CpuCount,
	long RamTotalBytes,
	long RamAvailableBytes
)
{
	public long RamUsedBytes => RamTotalBytes - RamAvailableBytes;
}