namespace Tests;

public class MockCli(Dictionary<string, string> commandResults) : TinySystemInfo.ICli
{
    public Dictionary<string, string> CommandResults { get; } = commandResults;

    public string Run(string command) => CommandResults[command];
}