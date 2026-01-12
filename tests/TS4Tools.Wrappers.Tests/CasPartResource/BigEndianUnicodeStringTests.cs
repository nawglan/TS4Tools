using FluentAssertions;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="BigEndianUnicodeString"/>.
/// </summary>
public class BigEndianUnicodeStringTests
{
    [Fact]
    public void RoundTrip_EmptyString_PreservesData()
    {
        var buffer = new byte[10];
        int written = BigEndianUnicodeString.Write(buffer, string.Empty);

        written.Should().Be(1); // Just the length byte (0)

        string result = BigEndianUnicodeString.Read(buffer, out int read);

        result.Should().Be(string.Empty);
        read.Should().Be(1);
    }

    [Fact]
    public void RoundTrip_SimpleAscii_PreservesData()
    {
        string input = "Hello";
        var buffer = new byte[BigEndianUnicodeString.GetSerializedSize(input)];
        int written = BigEndianUnicodeString.Write(buffer, input);

        string result = BigEndianUnicodeString.Read(buffer, out int read);

        result.Should().Be(input);
        read.Should().Be(written);
    }

    [Fact]
    public void RoundTrip_Unicode_PreservesData()
    {
        string input = "Héllo Wörld 日本語";
        var buffer = new byte[BigEndianUnicodeString.GetSerializedSize(input)];
        int written = BigEndianUnicodeString.Write(buffer, input);

        string result = BigEndianUnicodeString.Read(buffer, out int read);

        result.Should().Be(input);
        read.Should().Be(written);
    }

    [Fact]
    public void RoundTrip_LongString_PreservesData()
    {
        // String longer than 127 bytes (tests multi-byte length encoding)
        string input = new string('A', 100);
        var buffer = new byte[BigEndianUnicodeString.GetSerializedSize(input)];
        int written = BigEndianUnicodeString.Write(buffer, input);

        string result = BigEndianUnicodeString.Read(buffer, out int read);

        result.Should().Be(input);
        read.Should().Be(written);
    }

    [Fact]
    public void GetSerializedSize_EmptyString_Returns1()
    {
        int size = BigEndianUnicodeString.GetSerializedSize(string.Empty);
        size.Should().Be(1);
    }

    [Fact]
    public void GetSerializedSize_AsciiString_CalculatesCorrectly()
    {
        // "Hello" = 5 chars * 2 bytes per char = 10 bytes + 1 byte length = 11
        int size = BigEndianUnicodeString.GetSerializedSize("Hello");
        size.Should().Be(11);
    }

    [Fact]
    public void Write_NullString_TreatsAsEmpty()
    {
        var buffer = new byte[10];
        int written = BigEndianUnicodeString.Write(buffer, null!);

        written.Should().Be(1);
        buffer[0].Should().Be(0);
    }
}
