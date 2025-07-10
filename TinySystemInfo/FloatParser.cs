using System.Text.RegularExpressions;

public static class FloatParser
{
    public static string FloatPattern => "(\\d+[.,]\\d+)";

    public static float Parse(string value)
    {
        var match = Regex.Match(value, FloatPattern);
        if (!match.Success)
            throw new InvalidOperationException();

        // replacing , for . looks like dumb culture handling but the issue is
        // that commands like 'top' are often in english while the OS culture is
        // german for example.
        // Decimals are always devided with either , or . so this is a viable solution.
        var floatString = match.Groups[1].Value.Trim().Replace(',', '.');
        return float.Parse(floatString, System.Globalization.CultureInfo.InvariantCulture);
    }
}