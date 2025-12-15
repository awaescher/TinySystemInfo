using System.Runtime.InteropServices;
using TinySystemInfo.Platforms;

namespace TinySystemInfo;

/// <summary>
/// Provides cross-platform system information reading capabilities.
/// </summary>
public class TinySystemReader
{
	/// <summary>
	/// Asynchronously reads system information for the current operating system platform.
	/// </summary>
	/// <param name="options">Optional configuration settings for the system reader, such as CPU measurement delay.</param>
	/// <returns>A task that represents the asynchronous operation, containing the system information.</returns>
	/// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
	public static async Task<SystemInfo> ReadAsync(SystemReaderOptions? options = default)
	{
		ISystemReader reader = Environment.OSVersion.Platform switch
		{
			PlatformID.Unix => RuntimeInformation.RuntimeIdentifier.Contains("osx") ? new MacSystemReader() : new LinuxSystemReader(),
			PlatformID.Win32NT => new WindowsSystemReader(),
			_ => throw new PlatformNotSupportedException($"Platform \"{Environment.OSVersion.VersionString}\" is not supported."),
		};

		return await reader.ReadAsync(options);
	}
}
