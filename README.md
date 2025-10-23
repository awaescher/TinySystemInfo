# TinySystemInfo

A dependency free package that gathers just the absolute basic system metrics on Linux, macOS and Windows.

## System Information Sources

| OS | OS Info | CPU Usage | Memory | Volumes |
|----|---------|-----------|---------|---------|
| **Linux** | `cat /etc/os-release` | `cat /proc/stat` <sup>[1](#cpu-linux)</sup> | `cat /proc/meminfo` | `df \| grep -E '^/dev/(sd\|vd\|nvme\|hd)'` |
| **macOS** | `sw_vers` | `top -l 2 -s 1` <sup>[2](#cpu-macos)</sup> | `vm_stat`, `sysctl`, `pagesize` <sup>[3](#mem-macos)</sup> | `df -k /Volumes/*` |
| **Windows** | `System.Environment` | `PerformanceCounter` <sup>[4](#cpu-windows)</sup> | `GlobalMemoryStatusEx()` | `DriveInfo.GetDrives()` |

<a name="cpu-linux"></a>**[1] Linux CPU**: Reads CPU tick counters from `/proc/stat`, takes two samples with a delay, and calculates the delta for accurate current CPU usage.

<a name="cpu-macos"></a>**[2] macOS CPU**: Uses `top -l 2 -s 1 -n 0 | grep 'CPU usage'` which provides two samples with 1 second interval. The second sample represents the actual CPU load during that interval (not the average since boot).

<a name="mem-macos"></a>**[3] macOS Memory**: Combines multiple sources:
- `sysctl -n hw.memsize` for total physical memory
- `vm_stat` for virtual memory statistics (free, active, inactive, speculative pages)
- `pagesize` to get the memory page size (4096 or 16384 bytes on Apple Silicon)
- Free memory = (free + inactive + speculative) pages × page size

<a name="cpu-windows"></a>**[4] Windows CPU**: Uses `PerformanceCounter("Processor", "% Processor Time", "_Total")` with dual sampling (minimum 500ms delay) for accurate measurements.

## Usage

Install `TinySystemInfo` from NuGet and simply call `TinySystemReader.Read()`.

``` plaintext
Hostname:           MacStudio
OS Architecture:    Arm64
OS Name:            macOS
OS Version:         26.0.1
CPU Count:          16
CPU Usage:          11,16 %
Memory Total:       128,00 GB
Memory Used:        57,11 GB
Memory Free:        70,89 GB
Memory Usage:       44,61 %
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
