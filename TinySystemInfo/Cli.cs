using System.Text.RegularExpressions;

namespace TinySystemInfo;

public class BashCli : ICli
{
    public string Run(string command)
    {
        using (var process = new System.Diagnostics.Process())
        {
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
}