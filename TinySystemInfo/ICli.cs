namespace TinySystemInfo;

/// <summary>
/// Defines a contract for executing command-line interface commands.
/// </summary>
public interface ICli
{
    /// <summary>
    /// Executes a command and returns its output.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The output from the command execution.</returns>
    string Run(string command);
}
