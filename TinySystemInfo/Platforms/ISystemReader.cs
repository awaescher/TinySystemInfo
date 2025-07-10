namespace TinySystemInfo.Platforms;

internal interface ISystemReader
{
	Task<SystemInfo> Read(TimeSpan delay);
}
