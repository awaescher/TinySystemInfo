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

            info.FreeMemory.ShouldBe(137438953472L / 100 * 96);
            info.TotalMemory.ShouldBe(137438953472L);
        }
    }
}