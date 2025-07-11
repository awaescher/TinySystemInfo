using TinySystemInfo;

var info = await TinySystemReader.Read(TimeSpan.FromSeconds(1));

Console.WriteLine(@$"
Hostname:           {info.HostName}
OS Architecture:    {info.OSArchitecture}
OS Name:            {info.OSName}
OS Version:         {info.OSVersion}
CPU Count:          {info.CpuCount}
CPU Usage:          {FormatPercent(info.CpuUsagePercent)}
Memory Total:       {FormatGigabytesWithDecimals(info.Memory.TotalBytes)}
Memory Used:        {FormatGigabytesWithDecimals(info.Memory.UsedBytes)}
Memory Free:        {FormatGigabytesWithDecimals(info.Memory.FreeBytes)}
Memory Usage:       {FormatPercent(info.Memory.Usage)}
Volumes:");

foreach (var volume in info.Volumes)
{
	Console.WriteLine(@$"  {volume.Mount}
    Total:          {FormatGigabytes(volume.TotalBytes)}
    Used:           {FormatGigabytes(volume.UsedBytes)}
    Usage:          {FormatPercent(volume.Usage)}");
}

string FormatPercent(float percent) => $"{percent:n2} %";

string FormatGigabytes(long bytes) => (bytes / 1024f / 1024f / 1024f).ToString("0") + " GB";

string FormatGigabytesWithDecimals(long bytes) => (bytes / 1024f / 1024f / 1024f).ToString($"0.00") + " GB";