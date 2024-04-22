using TinySystemInfo;

var info = await TinySystemReader.Read();

var allProperties = info.GetType().GetProperties();
var longestPropertyName = allProperties.Max(p => p.Name.Length);

foreach (var p in allProperties)
	Console.WriteLine($"{p.Name.PadRight(longestPropertyName + 4, ' ')} {p.GetValue(info)}" + (p.Name.Contains("Bytes") ? $" ({((long)p.GetValue(info)) / 1024 / 1024} MB)" : "")); 