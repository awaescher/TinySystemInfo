namespace TinySystemInfo.Platforms;

/// <summary>
/// Defines the contract for reading system information on a specific platform.
/// </summary>
internal interface ISystemReader
{
	/// <summary>
	/// Asynchronously reads comprehensive system information.
	/// </summary>
	/// <param name="options">Optional configuration settings for the system reader.</param>
	/// <returns>A task that represents the asynchronous operation, containing the system information.</returns>
	Task<SystemInfo> ReadAsync(SystemReaderOptions? options = default);
}
