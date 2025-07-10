using System.Text.RegularExpressions;

namespace TinySystemInfo;

/// <summary>
/// Provides utility methods for parsing floating-point numbers from strings with flexible decimal separator handling.
/// </summary>
/// <remarks>
/// This parser is designed to handle strings that may contain floating-point values with either comma (,) or dot (.) 
/// as decimal separators, regardless of the system's current culture settings. This is particularly useful when parsing 
/// output from command-line tools that may use different locale settings than the operating system.
/// </remarks>
public static class FloatParser
{
    /// <summary>
    /// Gets the regular expression pattern used to match floating-point numbers with either comma or dot decimal separators.
    /// </summary>
    /// <value>
    /// A regex pattern string that matches one or more digits, followed by either a comma or dot, followed by one or more digits.
    /// </value>
    public static string FloatPattern => "(\\d+[.,]\\d+)";

    /// <summary>
    /// Parses a floating-point number from the specified string value.
    /// </summary>
    /// <param name="value">The string containing a floating-point number to parse.</param>
    /// <returns>A <see cref="float"/> value parsed from the input string.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the input string does not contain a valid floating-point number matching the expected pattern.
    /// </exception>
    /// <remarks>
    /// This method automatically handles both comma and dot decimal separators by normalizing them to dots 
    /// before parsing with invariant culture settings. This ensures consistent parsing regardless of the 
    /// system's locale configuration.
    /// </remarks>
    public static float Parse(string value)
{
        var match = Regex.Match(value, FloatPattern);
        if (!match.Success)
            throw new FormatException("Value does not contain any float values to parse: " + value);

        // replacing , for . looks like dumb culture handling but the issue is
        // that commands like 'top' are often in english while the OS culture is
        // german for example.
        // Decimals are always devided with either , or . so this is a viable solution.
        var floatString = match.Groups[1].Value.Trim().Replace(',', '.');
        return float.Parse(floatString, System.Globalization.CultureInfo.InvariantCulture);
    }
}