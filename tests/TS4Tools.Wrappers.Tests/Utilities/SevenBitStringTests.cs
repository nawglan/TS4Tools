using FluentAssertions;
using TS4Tools.Wrappers.Utilities;
using Xunit;

namespace TS4Tools.Wrappers.Tests.Utilities;

/// <summary>
/// Tests for <see cref="SevenBitString"/>, <see cref="BinaryWriterExtensions"/> and <see cref="StringExtensions"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/CS System Classes/SevenBitString.cs (length prefix in BYTES, 7 bits per byte,
///   continuation bit 0x80; zero length writes one zero byte; null writes as empty),
///   s4pi.Commons/Extensions/BinaryWriterExtensions.cs (WriteEmptyBytes) and StringExtensions.cs (FileExtension).
/// </summary>
public class SevenBitStringTests
{
    [Fact]
    public void Write_Ascii_PrefixesByteLength()
    {
        using var ms = new MemoryStream();

        SevenBitString.Write(ms, Encoding.ASCII, "abc");

        ms.ToArray().Should().Equal(3, (byte)'a', (byte)'b', (byte)'c');
    }

    [Fact]
    public void Write_BigEndianUnicode_PrefixCountsBytesNotChars()
    {
        using var ms = new MemoryStream();

        SevenBitString.WriteBigEndianUnicode(ms, "Hi");

        ms.ToArray().Should().Equal(4, 0x00, (byte)'H', 0x00, (byte)'i');
    }

    [Fact]
    public void Write_NullAndEmpty_WriteSingleZeroByte()
    {
        using var ms = new MemoryStream();

        SevenBitString.Write(ms, Encoding.UTF8, null);
        SevenBitString.Write(ms, Encoding.UTF8, "");

        ms.ToArray().Should().Equal(0, 0);
    }

    [Fact]
    public void Write_LongString_UsesTwoBytePrefix()
    {
        using var ms = new MemoryStream();

        SevenBitString.Write(ms, Encoding.ASCII, new string('x', 200));

        var bytes = ms.ToArray();
        bytes[0].Should().Be(0xC8); // 200 = 0b1_1001000: low 7 bits 0x48 | 0x80
        bytes[1].Should().Be(0x01);
        bytes.Length.Should().Be(202);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello, world")]
    [InlineData("Unicode é中文")]
    public void RoundTrip_AcrossEncodings(string text)
    {
        foreach (var encoding in new[] { Encoding.UTF8, Encoding.BigEndianUnicode, Encoding.Unicode })
        {
            using var ms = new MemoryStream();
            SevenBitString.Write(ms, encoding, text);
            ms.Position = 0;

            SevenBitString.Read(ms, encoding).Should().Be(text);
            ms.Position.Should().Be(ms.Length);
        }
    }

    [Fact]
    public void RoundTrip_LongBigEndianUnicode()
    {
        var text = new string('é', 5000);
        using var ms = new MemoryStream();

        SevenBitString.WriteBigEndianUnicode(ms, text);
        ms.Position = 0;

        SevenBitString.ReadBigEndianUnicode(ms).Should().Be(text);
    }

    [Fact]
    public void Read_FromSpan_ReportsBytesConsumed()
    {
        byte[] data = [3, (byte)'a', (byte)'b', (byte)'c', 0xFF];

        SevenBitString.Read(data, Encoding.ASCII, out var read).Should().Be("abc");

        read.Should().Be(4);
    }

    [Fact]
    public void Read_LengthExceedsStream_ThrowsInvalidData()
    {
        using var ms = new MemoryStream([10, (byte)'a']);

        ((Action)(() => SevenBitString.Read(ms, Encoding.ASCII))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Read_LengthExceedsSpan_ThrowsInvalidData()
    {
        ((Action)(() => SevenBitString.Read([10, 1], Encoding.ASCII, out _))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Read_TruncatedPrefix_Throws()
    {
        using var ms = new MemoryStream([0x80]);

        ((Action)(() => SevenBitString.Read(ms, Encoding.ASCII))).Should().Throw<EndOfStreamException>();
        ((Action)(() => SevenBitString.Read([0x80], Encoding.ASCII, out _))).Should().Throw<InvalidDataException>();
        using var empty = new MemoryStream();
        ((Action)(() => SevenBitString.Read(empty, Encoding.ASCII))).Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void Read_PrefixTooLong_Throws()
    {
        byte[] endless = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
        using var ms = new MemoryStream(endless);

        ((Action)(() => SevenBitString.Read(ms, Encoding.ASCII))).Should().Throw<InvalidDataException>();
        ((Action)(() => SevenBitString.Read(endless, Encoding.ASCII, out _))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Read_NegativeLengthFromOverflow_Throws()
    {
        // 0xFF,0xFF,0xFF,0xFF,0x0F decodes to 0xFFFFFFFF (-1 as int)
        using var ms = new MemoryStream([0xFF, 0xFF, 0xFF, 0xFF, 0x0F]);

        ((Action)(() => SevenBitString.Read(ms, Encoding.ASCII))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void NullArguments_Throw()
    {
        ((Action)(() => SevenBitString.Read((Stream)null!, Encoding.ASCII))).Should().Throw<ArgumentNullException>();
        ((Action)(() => SevenBitString.Write(new MemoryStream(), null!, "x"))).Should().Throw<ArgumentNullException>();
        ((Action)(() => SevenBitString.Write(null!, Encoding.ASCII, "x"))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Format_IsCompatibleWithBinaryWriterWriteString()
    {
        // BinaryWriter.Write(string) uses the same 7-bit length prefix over the writer's encoding
        using var expected = new MemoryStream();
        using (var writer = new BinaryWriter(expected, Encoding.BigEndianUnicode, leaveOpen: true))
            writer.Write("Sims 4");

        using var actual = new MemoryStream();
        SevenBitString.WriteBigEndianUnicode(actual, "Sims 4");

        actual.ToArray().Should().Equal(expected.ToArray());
    }

    // ---- BinaryWriterExtensions ----

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(1000)]
    public void WriteEmptyBytes_WritesRequestedZeros(int count)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)7);

        writer.WriteEmptyBytes(count);
        writer.Flush();

        ms.Length.Should().Be(count + 1);
        ms.ToArray().Skip(1).Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void WriteEmptyBytes_InvalidArguments_Throw()
    {
        using var writer = new BinaryWriter(new MemoryStream());

        ((Action)(() => writer.WriteEmptyBytes(-1))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => ((BinaryWriter)null!).WriteEmptyBytes(1))).Should().Throw<ArgumentNullException>();
    }

    // ---- StringExtensions ----

    [Theory]
    [InlineData("file.package", "package")]
    [InlineData("a.b.c", "c")]
    [InlineData("noextension", "")]
    [InlineData("trailing.", "")]
    [InlineData(".hidden", "hidden")]
    [InlineData("", "")]
    public void FileExtension_ReturnsTextAfterLastDot(string name, string expected)
    {
        name.FileExtension().Should().Be(expected);
    }

    [Fact]
    public void FileExtension_Null_Throws()
    {
        ((Action)(() => ((string)null!).FileExtension())).Should().Throw<ArgumentNullException>();
    }
}
