using TinySystemInfo;

var info = await TinySystemReader.Read(TimeSpan.FromSeconds(1));

Console.WriteLine(@$"
Hostname:           {info.HostName}
OS Architecture:    {info.OSArchitecture}
OS Name:            {info.OSName}
OS Version:         {info.OSVersion}
CPU Count:          {info.CpuCount}
CPU Usage:          {FormatPercent(info.CpuUsagePercent)}
Memory Total:       {FormatGigabytes(info.Memory.TotalBytes)}
Memory Used:        {FormatGigabytes(info.Memory.UsedBytes)}
Memory Free:        {FormatGigabytes(info.Memory.FreeBytes)}
Memory Usage:       {FormatPercent(info.Memory.Usage)}
Volumes:");

foreach (var volume in info.Volumes)
{
	Console.WriteLine(@$"  {volume.Mount}
    Total:          {FormatGigabytes(volume.TotalBytes)}
    Used:           {FormatGigabytes(volume.UsedBytes)}
    Free:           {FormatGigabytes(volume.FreeBytes)}
    Usage:          {FormatPercent(volume.Usage)}");
}

string FormatPercent(float percent) => $"{percent:n2} %";

string FormatGigabytes(long bytes) => (bytes / 1024f / 1024f / 1024f).ToString("0.00") + " GB";