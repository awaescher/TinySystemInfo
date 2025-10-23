using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using TinySystemInfo;
using TinySystemInfo.Platforms;

namespace Tests;

public class LinuxSystemReaderTests
{
    private Dictionary<string, string> _cliResults = [];
    private LinuxSystemReader _linuxSystemReader;

    [SetUp]
    public void Setup()
    {
        _linuxSystemReader = new LinuxSystemReader()
        {
            Cli = new MockCli(_cliResults)
        };
    }

    public class GetOsNameMethod : LinuxSystemReaderTests
    {
        [Test]
        public void Returns_Name()
        {
            _cliResults[LinuxSystemReader.OsInfoCommand] = @"
            PRETTY_NAME=""Ubuntu 25.04""
            NAME=""Ubuntu""
            VERSION_ID=""25.04""
            VERSION=""25.04 (Plucky Puffin)""
            ID=ubuntu
            LOGO=ubuntu-logo";

            _linuxSystemReader.GetOsName(_linuxSystemReader.GetOsInfo()).ShouldBe("Ubuntu 25.04");
        }
    }

    public class GetOsVersionMethod : LinuxSystemReaderTests
    {
        [Test]
        public void Returns_Version_Id()
        {
            _cliResults[LinuxSystemReader.OsInfoCommand] = @"
            PRETTY_NAME=""Ubuntu 25.04""
            NAME=""Ubuntu""
            VERSION_ID=""25.04""
            VERSION=""25.04 (Plucky Puffin)""
            ID=ubuntu
            LOGO=ubuntu-logo";

            _linuxSystemReader.GetOsVersion(_linuxSystemReader.GetOsInfo()).ShouldBe("25.04");
        }
    }

    public class GetCpuUsageMethod : LinuxSystemReaderTests
    {
        [Test]
        public void Parses_Cpu_Usage()
        {
            _cliResults[LinuxSystemReader.CpuStatCommand] = "cpu  100 0 50 850 0 0 0 0 0 0";
            var cpuInfo1 = _linuxSystemReader.GetCpuInfo();
            
            _cliResults[LinuxSystemReader.CpuStatCommand] = "cpu  200 0 100 1700 0 0 0 0 0 0";
            var cpuInfo2 = _linuxSystemReader.GetCpuInfo();
            
            // Delta: user=100, system=50, idle=850 -> total=1000, active=150 -> 15% usage
            _linuxSystemReader.CalculateCpuUsage(cpuInfo1, cpuInfo2).ShouldBe(15.0f, 0.01f);
        }
        
        [Test]
        public void Legacy_Method_Parses_Cpu_Usage()
        {
            // Keep the old test for backward compatibility
            _cliResults["top -b -n 1 | grep -i %CPU"] = "%CPU(s):  4,5 us,  2,3 sy,  0,0 ni, 93,21 id,  0,0 wa,  0,0 hi,  0,0 si,  0,0 st";
            _linuxSystemReader.GetCpuUsage().ShouldBe(6.79f, 0.01f);
        }
    }

    public class GetMemoryInfonMethod : LinuxSystemReaderTests
    {
        [Test]
        public void Parses_Free_And_Total_Memory()
        {
            _cliResults[LinuxSystemReader.MemoryInfoCommand] = @"
                MemTotal:        9633420 kB
                MemFree:         1281268 kB
                MemAvailable:    6892328 kB
                Buffers:           32764 kB
                Cached:          5631516 kB
                SwapCached:            0 kB
                Active:          2005960 kB
                Inactive:        5674856 kB
                ";

            var info = _linuxSystemReader.GetMemoryInfo();

            info.FreeBytes.ShouldBe(1281268L * 1024L);
            info.TotalBytes.ShouldBe(9633420L * 1024L);
        }
    }
    
    public class GetVolumesMethod : LinuxSystemReaderTests
    {
        [Test]
        public void Parses_Volumes()
        {
            // Filesystem     1K-blocks       Used Available iused  Mounted on
            _cliResults[LinuxSystemReader.VolumesCommand] = @"
                /dev/vda2       23509104 12949252   9340320   59% /
                /dev/nvme0n1    10218772  1592928   8085172   17% /media/MyExternalDrive";

            var info = _linuxSystemReader.GetVolumes().ToArray();

            info.Length.ShouldBe(2);

            info[0].Mount.ShouldBe("/");
            ((double)info[0].TotalBytes / 1024d / 1024d / 1024d).ShouldBe(24.1, tolerance: 0.2);
            ((double)info[0].UsedBytes / 1024d / 1024d / 1024d).ShouldBe(13.3, tolerance: 0.2);
            
            info[1].Mount.ShouldBe("/media/MyExternalDrive");
            ((double)info[1].TotalBytes / 1024d / 1024d / 1024d).ShouldBe(10.5, tolerance: 0.2);
            ((double)info[1].UsedBytes / 1024d / 1024d / 1024d).ShouldBe(1.6, tolerance: 0.2);
        }
    }
}