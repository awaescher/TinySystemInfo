namespace TinySystemInfo;

internal class Program
{
	static async Task Main()
	{
		var info = await TinySystemReader.Read();

		foreach (var p in info.GetType().GetProperties())
			Console.WriteLine($"{p.Name}: {p.GetValue(info)}" + (p.Name.Contains("Bytes") ? $" ({((long)p.GetValue(info)) / 1024 / 1024} MB)" : ""));
	}
}
