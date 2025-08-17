using System.Globalization;

namespace TS4Tools.Resources.Common.Converters;

/// <summary>
/// Provides data conversion utilities for common resource operations.
/// </summary>
public static class DataConverter
{
    /// <summary>
    /// Converts a hexadecimal string to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="hexString">The hexadecimal string to convert.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid hexadecimal number.</exception>
    public static uint HexStringToUInt32(string hexString)
    {
        if (string.IsNullOrWhiteSpace(hexString))
            throw new ArgumentException("Hex string cannot be null or empty.", nameof(hexString));

        // Remove common prefixes
        var cleanHex = hexString.Trim();
        if (cleanHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            cleanHex = cleanHex[2..];
        else if (cleanHex.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            cleanHex = cleanHex[2..];

        if (!uint.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
            throw new ArgumentException($"'{hexString}' is not a valid hexadecimal number.", nameof(hexString));

        return result;
    }

    /// <summary>
    /// Converts a 32-bit unsigned integer to a hexadecimal string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="includePrefix">Whether to include the '0x' prefix.</param>
    /// <param name="minWidth">The minimum width of the hex string (padded with zeros).</param>
    /// <returns>The hexadecimal string representation.</returns>
    public static string UInt32ToHexString(uint value, bool includePrefix = true, int minWidth = 8)
    {
        var format = $"X{Math.Max(1, minWidth)}";
        var hex = value.ToString(format, CultureInfo.InvariantCulture);
        return includePrefix ? $"0x{hex}" : hex;
    }

    /// <summary>
    /// Tries to parse a string as a hexadecimal or decimal number.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="result">The parsed result.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParseNumber(string? input, out uint result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var cleanInput = input.Trim();

        // Try hex first
        if (cleanInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(cleanInput[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // Try decimal
        if (uint.TryParse(cleanInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            return true;

        // Try hex without prefix
        return uint.TryParse(cleanInput, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Converts bytes to a human-readable size string.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A formatted size string (e.g., "1.2 MB").</returns>
    public static string FormatByteSize(long bytes)
    {
        if (bytes < 0)
            return "0 B";

        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        // For bytes, show whole numbers; for larger units, show one decimal place
        return order == 0 ? $"{size:F0} {sizes[order]}" : $"{size:F1} {sizes[order]}";
    }

    /// <summary>
    /// Safely converts an object to a string, handling null values.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="defaultValue">The default value to use if the input is null.</param>
    /// <returns>The string representation of the value.</returns>
    public static string SafeToString(object? value, string defaultValue = "")
    {
        return value?.ToString() ?? defaultValue;
    }

    /// <summary>
    /// Truncates a string to the specified maximum length, adding ellipsis if truncated.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="ellipsis">The ellipsis string to append when truncating.</param>
    /// <returns>The truncated string.</returns>
    public static string Truncate(string? input, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        var truncateLength = Math.Max(0, maxLength - ellipsis.Length);
        return input[..truncateLength] + ellipsis;
    }
}
