using System.Text.RegularExpressions;

namespace TinySystemInfo;

/// <summary>
/// Provides command execution capabilities using the Bash shell on Unix-like systems.
/// </summary>
public class BashCli : ICli
{
    /// <summary>
    /// Executes a command in a Bash shell and returns the output.
    /// </summary>
    /// <param name="command">The command to execute in the Bash shell.</param>
    /// <returns>The trimmed standard output from the command execution.</returns>
    public string Run(string command)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result.Trim();
    }
}