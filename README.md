# TinySystemInfo

A dependency free package that gathers just the absolute basic system metrics on Windows, macOS and Linux.

|Operating system|Methods used|Comment|
|-|-|-|
|Linux|`top`, `/proc/meminfo`, `/etc/os-release`|-|
|macOS|`top`, `sysctl`, `sw_vers`|-|
|Windows|`GlobalMemoryStatusEx()` & PerformanceCounters|No WMI because of massive delays|

## Usage

Install `TinySystemInfo` from NuGet and simply call `TinySystemReader.Read()`.

``` csharp
var info = await TinySystemReader.Read();

// HostName: VDESK01
// OSArchitecture: X64
// OSName: Ubuntu
// OSVersion: 20.04
// CpuUsagePercent: 19.800003
// CpuCount: 8
// RamTotalBytes: 8299524096 (7915 MB)
// RamAvailableBytes: 5469491200 (5216 MB)
// RamUsedBytes: 2830032896 (2698 MB)
```

The [app icon was made by Payungkead from www.flaticon.com](https://www.flaticon.com/free-icon/circuit_2479550) and is licensed by the Flaticon license.

