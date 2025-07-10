using System.Runtime.InteropServices;
using TinySystemInfo.Platforms;

namespace TinySystemInfo;

public class TinySystemReader
{
	public static async Task<SystemInfo> Read(TimeSpan delay)
	{
		ISystemReader reader = Environment.OSVersion.Platform switch
		{
			PlatformID.Unix => RuntimeInformation.RuntimeIdentifier.Contains("osx") ? new MacSystemReader() : new LinuxSystemReader(),
			PlatformID.Win32NT => new WindowsSystemReader(),
			_ => throw new PlatformNotSupportedException($"Platform \"{Environment.OSVersion.VersionString}\" is not supported."),
		};

		return await reader.Read(delay);
	}
}
