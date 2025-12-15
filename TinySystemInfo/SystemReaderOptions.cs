namespace TinySystemInfo;

/// <summary>
/// Provides configuration options for system information readers.
/// </summary>
public class SystemReaderOptions
{
	/// <summary>
	/// Gets or sets the delay between CPU measurements to calculate usage.
	/// </summary>
	/// <remarks>
	/// CPU usage is typically calculated by taking two measurements and comparing the difference.
	/// A longer delay provides more accurate results but increases the time to read system information.
	/// </remarks>
	public TimeSpan DelayBetweenCpuMeasurements { get; set; } = TimeSpan.FromMilliseconds(500);
}