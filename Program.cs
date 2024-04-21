namespace TinySystemInfo;

internal class Program
{
	static async Task Main()
	{
		var info = await TinySystemReader.Read();

		foreach (var p in info.GetType().GetProperties())
			Console.WriteLine($"{p.Name}: {p.GetValue(info)}");
	}
}
