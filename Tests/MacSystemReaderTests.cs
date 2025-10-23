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
        
        [Test]
        public void Parses_Cpu_Usage_From_Top_Sampled()
        {
            // top -l 2 output with 2 samples
            _cliResults[MacSystemReader.CpuUsageSampledCommand] = @"CPU usage: 5.80% user, 7.15% sys, 87.4% idle 
CPU usage: 4.28% user, 2.48% sys, 93.22% idle";
            
            // The last line shows: 93.22% idle -> CPU usage = 100 - 93.22 = 6.78%
            _macSystemReader.GetCpuUsageFromTopSampled().ShouldBe(6.78f, 0.01f);
        }
        
        [Test]
        public void Falls_Back_To_Single_Sample_When_Dual_Sample_Fails()
        {
            // Set up dual sample to return invalid data
            _cliResults[MacSystemReader.CpuUsageSampledCommand] = "invalid output";
            _cliResults[MacSystemReader.CpuUsageCommand] = "CPU usage: 1.73% user, 6.56% sys, 91.69% idle ";
            
            // Should fallback to single sample method
            _macSystemReader.GetCpuUsageFromTopSampled().ShouldBe(8.31f, 0.01f);
        }
        
        [Test]
        public void Parses_Cpu_Usage_From_IoStat()
        {
            // iostat output with 2 samples (legacy test)
            _cliResults["iostat -c 2 -w 1"] = @"
              disk0               disk6       cpu    load average
    KB/t  tps  MB/s     KB/t  tps  MB/s  us sy id   1m   5m   15m
   19.97   73  1.43   327.64    3  1.06   3  1 96  2.38 2.38 2.61
    4.34   94  0.40     0.00    0  0.00   5  3 92  2.38 2.38 2.61";
            
            // The last line shows: us=5, sy=3, id=92 -> CPU usage = 100 - 92 = 8%
            _macSystemReader.GetCpuUsageFromIoStat().ShouldBe(8.0f, 0.01f);
        }
    }

    public class GetMemoryInfonMethod : MacSystemReaderTests
    {
        [Test]
        public void Parses_Free_And_Total_Memory()
        {
            _cliResults[MacSystemReader.TotalMemoryCommand] = "137438953472";
            _cliResults[MacSystemReader.PageSizeCommand] = "16384";
            _cliResults[MacSystemReader.VmStatCommand] = @"
Pages free:                               1000000.
Pages active:                             2000000.
Pages inactive:                           1500000.
Pages speculative:                        500000.
Pages wired down:                         3000000.
Pages occupied by compressor:             500000.";

            var info = _macSystemReader.GetMemoryInfo();

            // Free = (free + inactive + speculative) * pageSize = (1000000 + 1500000 + 500000) * 16384 = 49152000000
            info.FreeBytes.ShouldBe(49152000000L);
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