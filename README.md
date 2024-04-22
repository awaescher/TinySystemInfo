# TinySystemInfo

A dependency free package that gathers just the absolute basic system metrics on Linux, macOS and Windows.

|Operating system|Meta info|Cpu usage|Memory usage|
|-|-|-|-|
|Linux|✅ `/etc/os-release`|🟨 `top` Seems too low|✅ `/proc/meminfo`|
|macOS|✅ `sw_vers`|✅ `top`|🟨 `sysctl` Incorrect, calculated from memory pressure|
|Windows|✅ `System.Environment`|🟨 `PerformanceCounter("% Processor Time")` Seems too low|✅ `GlobalMemoryStatusEx()`|

## Usage

Install `TinySystemInfo` from NuGet and simply call `TinySystemReader.Read()`.

``` csharp
var info = await TinySystemReader.Read();

> Sample output
> ---
> HostName:          VDESK01
> OSArchitecture:    X64
> OSName:            Ubuntu
> OSVersion:         20.04
> CpuUsagePercent:   19.8
> CpuCount:          8
> RamTotalBytes:     8299524096 (7915 MB)
> RamAvailableBytes: 5469491200 (5216 MB)
> RamUsedBytes:      2830032896 (2698 MB)
```

The [app icon was made by Payungkead from Flaticon](https://www.flaticon.com/free-icon/circuit_2479550) and is licensed by the Flaticon license.