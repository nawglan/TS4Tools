using FluentAssertions;
using TS4Tools.Resources.Common.Converters;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.Converters;

public class DataConverterTests
{
    [Theory]
    [InlineData("0x12345678", 0x12345678u)]
    [InlineData("0X12345678", 0x12345678u)]
    [InlineData("12345678", 0x12345678u)]
    [InlineData("0xABCDEF", 0xABCDEFu)]
    [InlineData("abcdef", 0xABCDEFu)]
    public void HexStringToUInt32_WithValidHex_ReturnsCorrectValue(string input, uint expected)
    {
        // Act
        var result = DataConverter.HexStringToUInt32(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("xyz")]
    [InlineData("0xGHI")]
    public void HexStringToUInt32_WithInvalidInput_ThrowsArgumentException(string? input)
    {
        // Act & Assert
        var act = () => DataConverter.HexStringToUInt32(input!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0x12345678u, true, 8, "0x12345678")]
    [InlineData(0x12345678u, false, 8, "12345678")]
    [InlineData(0x123u, true, 8, "0x00000123")]
    [InlineData(0xABCu, false, 4, "0ABC")]
    public void UInt32ToHexString_WithVariousOptions_ReturnsFormattedString(uint value, bool includePrefix, int minWidth, string expected)
    {
        // Act
        var result = DataConverter.UInt32ToHexString(value, includePrefix, minWidth);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("123", 123u, true)]
    [InlineData("0x123", 0x123u, true)]
    [InlineData("0X123", 0x123u, true)]
    [InlineData("ABC", 0xABCu, true)]
    [InlineData("", 0u, false)]
    [InlineData("   ", 0u, false)]
    [InlineData(null, 0u, false)]
    [InlineData("xyz", 0u, false)]
    public void TryParseNumber_WithVariousInputs_ReturnsExpectedResults(string? input, uint expectedValue, bool expectedResult)
    {
        // Act
        var result = DataConverter.TryParseNumber(input, out var value);

        // Assert
        result.Should().Be(expectedResult);
        value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1.0 MB")]
    [InlineData(1073741824L, "1.0 GB")]
    [InlineData(-100L, "0 B")]
    public void FormatByteSize_WithVariousSizes_ReturnsFormattedString(long bytes, string expected)
    {
        // Act
        var result = DataConverter.FormatByteSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "", "test")]
    [InlineData(null, "default", "default")]
    [InlineData(123, "", "123")]
    [InlineData("", "default", "")]
    public void SafeToString_WithVariousInputs_ReturnsExpectedString(object? input, string defaultValue, string expected)
    {
        // Act
        var result = DataConverter.SafeToString(input, defaultValue);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 5, "...", "He...")]
    [InlineData("Hello", 10, "...", "Hello")]
    [InlineData("Hello World", 8, "...", "Hello...")]
    [InlineData("", 5, "...", "")]
    [InlineData(null, 5, "...", "")]
    [InlineData("Test", 3, "...", "...")]
    public void Truncate_WithVariousInputs_ReturnsExpectedString(string? input, int maxLength, string ellipsis, string expected)
    {
        // Act
        var result = DataConverter.Truncate(input, maxLength, ellipsis);

        // Assert
        result.Should().Be(expected);
    }
}
