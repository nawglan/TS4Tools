namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for DDS header reading and writing operations.
/// </summary>
public sealed class DdsHeaderExtensionsTests
{
    [Fact]
    public void WriteDdsHeader_ThenReadDdsHeader_RoundTripSucceeds()
    {
        // Arrange
        var originalHeader = DdsHeader.CreateForRGBA32(512, 256, 9);
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(originalHeader);
        stream.Position = 0; // Reset to beginning for reading
        var readHeader = stream.ReadDdsHeader();

        // Assert
        readHeader.Should().Be(originalHeader);
    }

    [Fact]
    public void WriteDdsHeader_CompressedFormat_WritesCorrectData()
    {
        // Arrange
        var header = DdsHeader.CreateForCompressed(1024, 512, DdsFourCC.DXT5, 262144, 11);
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(header);

        // Assert
        stream.Position.Should().Be(4 + DdsHeader.HeaderSize); // Magic + header size
        
        // Verify magic number was written
        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var magic = reader.ReadUInt32();
        magic.Should().Be(DdsHeader.DdsMagic);
    }

    [Fact]
    public void ReadDdsHeader_WithValidDdsData_ReadsCorrectHeader()
    {
        // Arrange
        var expectedHeader = DdsHeader.CreateForCompressed(256, 256, DdsFourCC.DXT1, 32768, 9);
        using var stream = new MemoryStream();
        stream.WriteDdsHeader(expectedHeader);
        stream.Position = 0;

        // Act
        var actualHeader = stream.ReadDdsHeader();

        // Assert
        actualHeader.Should().Be(expectedHeader);
        actualHeader.Width.Should().Be(256);
        actualHeader.Height.Should().Be(256);
        actualHeader.PixelFormat.FourCC.Should().Be(DdsFourCC.DXT1);
        actualHeader.MipMapCount.Should().Be(9);
    }

    [Fact]
    public void ReadDdsHeader_WithInvalidMagicNumber_ThrowsInvalidDataException()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(0x12345678u); // Invalid magic number
        writer.Write(DdsHeader.HeaderSize);
        // Write minimal valid header data
        for (int i = 0; i < 30; i++) writer.Write(0u);
        stream.Position = 0;

        // Act & Assert
        var action = () => stream.ReadDdsHeader();
        action.Should().Throw<InvalidDataException>()
            .WithMessage("Invalid DDS magic number: 0x12345678. Expected: 0x20534444");
    }

    [Fact]
    public void ReadDdsHeader_WithInvalidHeaderSize_ThrowsInvalidDataException()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(DdsHeader.DdsMagic);
        writer.Write(100u); // Invalid header size (should be 124)
        stream.Position = 0;

        // Act & Assert
        var action = () => stream.ReadDdsHeader();
        action.Should().Throw<InvalidDataException>()
            .WithMessage("Invalid DDS header size: 100. Expected: 124");
    }

    [Fact]
    public void ReadDdsHeader_WithInvalidPixelFormatSize_ThrowsInvalidDataException()
    {
        // Arrange
        using var stream = new MemoryStream();
        CreateHeaderWithInvalidPixelFormatSize(stream);
        stream.Position = 0;

        // Act & Assert
        var action = () => stream.ReadDdsHeader();
        action.Should().Throw<InvalidDataException>()
            .WithMessage("Invalid pixel format size: 20. Expected: 32");
    }

    [Fact]
    public void WriteDdsHeader_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var header = DdsHeader.CreateForRGBA32(64, 64);

        // Act & Assert
        var action = () => ((Stream)null!).WriteDdsHeader(header);
        action.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void ReadDdsHeader_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => ((Stream)null!).ReadDdsHeader();
        action.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void WriteDdsHeader_WithReservedFields_WritesAllReservedData()
    {
        // Arrange
        var reserved1 = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        var header = new DdsHeader
        {
            Size = DdsHeader.HeaderSize,
            Flags = DdsFlags.Caps | DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat,
            Height = 128,
            Width = 128,
            Reserved1 = reserved1,
            PixelFormat = DdsPixelFormat.CreateForRGBA32(),
            Caps = DdsCaps.Texture
        };
        
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(header);
        stream.Position = 0;
        var readHeader = stream.ReadDdsHeader();

        // Assert
        readHeader.Reserved1.Should().Equal(reserved1);
    }

    [Fact]
    public void WriteDdsHeader_WithPartialReservedFields_PadsWithZeros()
    {
        // Arrange
        var partialReserved = new uint[] { 1, 2, 3 }; // Only 3 elements instead of 11
        var header = new DdsHeader
        {
            Size = DdsHeader.HeaderSize,
            Flags = DdsFlags.Caps | DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat,
            Height = 64,
            Width = 64,
            Reserved1 = partialReserved,
            PixelFormat = DdsPixelFormat.CreateForRGBA32(),
            Caps = DdsCaps.Texture
        };
        
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(header);
        stream.Position = 0;
        var readHeader = stream.ReadDdsHeader();

        // Assert
        readHeader.Reserved1.Should().HaveCount(11);
        readHeader.Reserved1.Take(3).Should().Equal(new uint[] { 1, 2, 3 });
        readHeader.Reserved1.Skip(3).Should().AllBeEquivalentTo(0u);
    }

    [Theory]
    [InlineData(DdsFourCC.DXT1)]
    [InlineData(DdsFourCC.DXT3)]
    [InlineData(DdsFourCC.DXT5)]
    [InlineData(DdsFourCC.DST1)]
    [InlineData(DdsFourCC.DST3)]
    [InlineData(DdsFourCC.DST5)]
    [InlineData(DdsFourCC.ATI1)]
    [InlineData(DdsFourCC.ATI2)]
    public void RoundTrip_WithVariousFourCCFormats_PreservesData(DdsFourCC fourCC)
    {
        // Arrange
        var originalHeader = DdsHeader.CreateForCompressed(128, 128, fourCC, 8192, 1);
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(originalHeader);
        stream.Position = 0;
        var readHeader = stream.ReadDdsHeader();

        // Assert
        readHeader.Should().Be(originalHeader);
        readHeader.PixelFormat.FourCC.Should().Be(fourCC);
    }

    [Fact]
    public void RoundTrip_WithLargeImage_HandlesCorrectly()
    {
        // Arrange
        var originalHeader = DdsHeader.CreateForRGBA32(4096, 2048, 13); // Large image with many mipmaps
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(originalHeader);
        stream.Position = 0;
        var readHeader = stream.ReadDdsHeader();

        // Assert
        readHeader.Should().Be(originalHeader);
        readHeader.Width.Should().Be(4096);
        readHeader.Height.Should().Be(2048);
        readHeader.MipMapCount.Should().Be(13);
        readHeader.PitchOrLinearSize.Should().Be(4096 * 4); // 4 bytes per pixel
    }

    [Fact]
    public void ReadDdsHeader_LeavesStreamOpen()
    {
        // Arrange
        var header = DdsHeader.CreateForRGBA32(32, 32);
        using var stream = new MemoryStream();
        stream.WriteDdsHeader(header);
        stream.Position = 0;

        // Act
        var readHeader = stream.ReadDdsHeader();

        // Assert
        stream.CanRead.Should().BeTrue(); // Stream should still be open
        stream.Position.Should().Be(4 + DdsHeader.HeaderSize); // Should be positioned after header
    }

    [Fact]
    public void WriteDdsHeader_LeavesStreamOpen()
    {
        // Arrange
        var header = DdsHeader.CreateForRGBA32(32, 32);
        using var stream = new MemoryStream();

        // Act
        stream.WriteDdsHeader(header);

        // Assert
        stream.CanWrite.Should().BeTrue(); // Stream should still be open
        stream.Position.Should().Be(4 + DdsHeader.HeaderSize); // Should be positioned after written data
    }

    private static void CreateHeaderWithInvalidPixelFormatSize(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        
        // Write magic and header size
        writer.Write(DdsHeader.DdsMagic);
        writer.Write(DdsHeader.HeaderSize);
        
        // Write basic header fields
        writer.Write((uint)DdsFlags.Caps);  // flags
        writer.Write(64u);                  // height
        writer.Write(64u);                  // width
        writer.Write(256u);                 // pitch
        writer.Write(0u);                   // depth
        writer.Write(1u);                   // mipmap count
        
        // Write reserved fields (11 uints)
        for (int i = 0; i < 11; i++)
        {
            writer.Write(0u);
        }
        
        // Write pixel format with invalid size
        writer.Write(20u); // Invalid size (should be 32)
        writer.Write((uint)DdsPixelFormatFlags.RGBA);
        writer.Write((uint)DdsFourCC.None);
        writer.Write(32u); // RGB bit count
        writer.Write(0x00FF0000u); // red mask
        writer.Write(0x0000FF00u); // green mask
        writer.Write(0x000000FFu); // blue mask
        writer.Write(0xFF000000u); // alpha mask
        
        // Write remaining caps fields
        writer.Write((uint)DdsCaps.Texture);
        writer.Write(0u); // caps2
        writer.Write(0u); // caps3
        writer.Write(0u); // caps4
        writer.Write(0u); // reserved2
    }
}
