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
            _cliResults[LinuxSystemReader.CpuUsageCommand] = "%CPU(s):  4,5 us,  2,3 sy,  0,0 ni, 93,21 id,  0,0 wa,  0,0 hi,  0,0 si,  0,0 st";
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

            info.FreeMemory.ShouldBe(1281268L * 1024L);
            info.TotalMemory.ShouldBe(9633420L * 1024L);
        }
    }
}