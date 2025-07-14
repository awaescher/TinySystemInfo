# TinySystemInfo

A dependency free package that gathers just the absolute basic system metrics on Linux, macOS and Windows.

|Operating system|Meta info|Cpu usage|Memory usage|
|-|-|-|-|
|Linux|✅ `/etc/os-release`|🟨 `top` Seems too low|✅ `/proc/meminfo`|
|macOS|✅ `sw_vers`|✅ `top`|🟨 `sysctl` Incorrect, calculated from memory pressure|
|Windows|✅ `System.Environment`|🟨 `PerformanceCounter("% Processor Time")` Seems too low|✅ `GlobalMemoryStatusEx()`|

## Usage

Install `TinySystemInfo` from NuGet and simply call `TinySystemReader.Read()`.

``` plaintext
Hostname:           MacStudio
OS Architecture:    Arm64
OS Name:            macOS
OS Version:         15.5
CPU Count:          16
CPU Usage:          48,42 %
Memory Total:       128,00 GB
Memory Used:        89,60 GB
Memory Free:        38,40 GB
Memory Usage:       70,00 %
Volumes:
  /
    Total:          994,66 GB
    Used:           245,05 GB
    Free:           749,61 GB
    Usage:          24,64 %
  /Volumes/ExternalSsd
    Total:          2000,19 GB
    Used:           1320,43 GB
    Free:           679,76 GB
    Usage:          66,02 %
```

The [app icon was made by Payungkead from Flaticon](https://www.flaticon.com/free-icon/circuit_2479550) and is licensed by the Flaticon license.
