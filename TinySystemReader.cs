using TinySystemInfo.Platforms;

namespace TinySystemInfo;

public class TinySystemReader
{
	public static async Task<SystemInfo> Read()
	{
		ISystemReader reader = Environment.OSVersion.Platform switch
		{
			PlatformID.Unix => new LinuxSystemReader(),
			PlatformID.Win32NT => new WindowsSystemReader(),
			_ => throw new PlatformNotSupportedException($"Platform \"{Environment.OSVersion.VersionString}\" is not supported."),
		};

		return await reader.Read();
	}
}
