using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using TinySystemInfo;
using TinySystemInfo.Platforms;

namespace Tests;

public class MacSystemReaderTests
{
    private Dictionary<string, string> _cliResults = [];
    private MacSystemReader _macSystemReader;

    [SetUp]
    public void Setup()
    {
        _macSystemReader = new MacSystemReader()
        {
            Cli = new MockCli(_cliResults)
        };
    }

    public class GetOsNameMethod : MacSystemReaderTests
    {
        [Test]
        public void Returns_Name()
        {
            _cliResults[MacSystemReader.OsNameCommand] = "macOS";
            _macSystemReader.GetOsName().ShouldBe("macOS");
        }
    }

    public class GetOsVersionMethod : MacSystemReaderTests
    {
        [Test]
        public void Returns_Version_Number()
        {
            _cliResults[MacSystemReader.OsVersionCommand] = "15.5";
            _macSystemReader.GetOsVersion().ShouldBe("15.5");
        }
    }

    public class GetCpuUsageMethod : MacSystemReaderTests
    {
        [Test]
        public void Parses_Cpu_Usage()
        {
            _cliResults[MacSystemReader.CpuUsageCommand] = "CPU usage: 1.73% user, 6.56% sys, 91.69% idle ";
            _macSystemReader.GetCpuUsage().ShouldBe(8.31f, 0.01f);
        }
    }

    public class GetMemoryInfonMethod : MacSystemReaderTests
    {
        [Test]
        public void Parses_Free_And_Total_Memory()
        {
            _cliResults[MacSystemReader.FreeMemoryCommand] = "96";
            _cliResults[MacSystemReader.TotalMemoryCommand] = "137438953472";

            var info = _macSystemReader.GetMemoryInfo();

            info.FreeBytes.ShouldBe(137438953472L / 100 * 96);
            info.TotalBytes.ShouldBe(137438953472L);
        }
    }

    public class GetVolumesMethod : MacSystemReaderTests
    {
        [Test]
        public void Parses_Volumes()
        {
            // Filesystem     1024-blocks       Used Available Capacity iused      ifree %iused  Mounted on
            _cliResults[MacSystemReader.VolumesCommand] = @"
                /dev/disk3s1s1                 971350180   10988752  732312412     2%     425798 4294134754    0%   /
                /dev/disk7s1                  1953309744 1289197468  663819468    67%     346755 6638194680    0%   /Volumes/MyExternalDrive";

            var info = _macSystemReader.GetVolumes().ToArray();

            info.Length.ShouldBe(2);

            info[0].Mount.ShouldBe("/");
            ((double)info[0].TotalBytes / 1024d / 1024d / 1024d).ShouldBe(994.7d, tolerance: 0.2);
            ((double)info[0].FreeBytes / 1024d / 1024d / 1024d).ShouldBe(749.9, tolerance: 0.2);

            info[1].Mount.ShouldBe("/Volumes/MyExternalDrive");
            ((double)info[1].TotalBytes / 1024d / 1024d / 1024d).ShouldBe(2000.0d, tolerance: 0.2);
            ((double)info[1].FreeBytes / 1024d / 1024d / 1024d).ShouldBe(679.8, tolerance: 0.2);
        }
    }
}