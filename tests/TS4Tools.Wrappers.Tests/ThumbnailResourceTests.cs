using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="ThumbnailResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs
/// - Format specification:
///   - Standard JFIF/JPEG image data
///   - At offset 24: Optional ALFA magic (0x41464C41)
///   - If ALFA present at offset 24:
///     - Offset 28: Alpha PNG length (big-endian!)
///     - Offset 32: Alpha PNG data (length bytes)
///   - Alpha PNG's R channel provides alpha values for the JPEG color
/// - Type IDs: 0x0D338A3A, 0x16CCF748, 0x3BD45407, 0x3C1AF1F2, 0x3C2A8647,
///             0x5B282D45, 0xCD9DE247, 0xE18CAEE2, 0xE254AE6E
/// </summary>
public class ThumbnailResourceTests
{
    // One of the thumbnail resource type IDs - from legacy ThumbnailResourceHandler
    private static readonly ResourceKey TestKey = new(0x0D338A3A, 0, 0);

    private const uint AlfaMagic = 0x41464C41; // "ALFA"

    [Fact]
    public void CreateEmpty_HasNoData()
    {
        var thumb = new ThumbnailResource(TestKey, ReadOnlyMemory<byte>.Empty);

        thumb.DataLength.Should().Be(0);
        thumb.HasEmbeddedAlpha.Should().BeFalse();
        thumb.AlphaPngOffset.Should().Be(0);
        thumb.AlphaPngLength.Should().Be(0);
    }

    [Fact]
    public void Parse_JpegWithoutAlfa_StoresRawData()
    {
        // Create a fake JPEG without ALFA marker (just random data)
        var jpegData = CreateFakeJpegWithoutAlfa(100);

        var thumb = new ThumbnailResource(TestKey, jpegData);

        thumb.DataLength.Should().Be(100);
        thumb.HasEmbeddedAlpha.Should().BeFalse();
        thumb.RawData.ToArray().Should().BeEquivalentTo(jpegData);
    }

    [Fact]
    public void Parse_JpegWithAlfa_DetectsAlphaOffset()
    {
        // Create a fake JPEG with ALFA marker and embedded PNG
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG signature
        var jpegData = CreateFakeJpegWithAlfa(pngData);

        var thumb = new ThumbnailResource(TestKey, jpegData);

        thumb.HasEmbeddedAlpha.Should().BeTrue();
        thumb.AlphaPngOffset.Should().Be(32); // offset 24 + 4 (magic) + 4 (length) = 32
        thumb.AlphaPngLength.Should().Be(pngData.Length);
    }

    [Fact]
    public void GetAlphaPngData_WhenPresent_ReturnsCorrectData()
    {
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02, 0x03 };
        var jpegData = CreateFakeJpegWithAlfa(pngData);

        var thumb = new ThumbnailResource(TestKey, jpegData);

        var alphaPng = thumb.GetAlphaPngData();

        alphaPng.Length.Should().Be(pngData.Length);
        alphaPng.ToArray().Should().BeEquivalentTo(pngData);
    }

    [Fact]
    public void GetAlphaPngData_WhenNotPresent_ReturnsEmpty()
    {
        var jpegData = CreateFakeJpegWithoutAlfa(100);

        var thumb = new ThumbnailResource(TestKey, jpegData);

        var alphaPng = thumb.GetAlphaPngData();

        alphaPng.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetJpegData_ReturnsRawData()
    {
        var jpegData = CreateFakeJpegWithoutAlfa(50);

        var thumb = new ThumbnailResource(TestKey, jpegData);

        var jpeg = thumb.GetJpegData();

        jpeg.ToArray().Should().BeEquivalentTo(jpegData);
    }

    [Fact]
    public void RoundTrip_PreservesExactBytes()
    {
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var originalData = CreateFakeJpegWithAlfa(pngData);

        var thumb = new ThumbnailResource(TestKey, originalData);

        var serialized = thumb.Data;

        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void Parse_TooShortForAlfaCheck_TreatsAsPlainJpeg()
    {
        // Data shorter than 32 bytes (minimum to check for ALFA)
        var shortData = new byte[20];
        Array.Fill(shortData, (byte)0xFF);

        var thumb = new ThumbnailResource(TestKey, shortData);

        thumb.HasEmbeddedAlpha.Should().BeFalse();
        thumb.DataLength.Should().Be(20);
    }

    [Fact]
    public void Parse_InvalidAlphaLength_IgnoresAlpha()
    {
        // Create data with ALFA marker but invalid length (too large)
        var data = new byte[100];
        Array.Fill(data, (byte)0xFF);

        // Write ALFA magic at offset 24
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), AlfaMagic);

        // Write an invalid length (larger than remaining data)
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(28), 1000);

        var thumb = new ThumbnailResource(TestKey, data);

        // Should ignore the alpha due to invalid length
        thumb.HasEmbeddedAlpha.Should().BeFalse();
    }

    [Fact]
    public void Parse_NegativeAlphaLength_IgnoresAlpha()
    {
        var data = new byte[100];
        Array.Fill(data, (byte)0xFF);

        // Write ALFA magic at offset 24
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), AlfaMagic);

        // Write a negative length
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(28), -5);

        var thumb = new ThumbnailResource(TestKey, data);

        thumb.HasEmbeddedAlpha.Should().BeFalse();
    }

    [Fact]
    public void SetImageData_UpdatesDataAndDetectsAlpha()
    {
        var thumb = new ThumbnailResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var newData = CreateFakeJpegWithAlfa(pngData);

        thumb.SetImageData(newData);

        thumb.DataLength.Should().Be(newData.Length);
        thumb.HasEmbeddedAlpha.Should().BeTrue();
        thumb.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Factory_CreatesResourceCorrectly()
    {
        var factory = new ThumbnailResourceFactory();
        var data = CreateFakeJpegWithoutAlfa(64);

        var resource = factory.Create(TestKey, data);

        resource.Should().BeOfType<ThumbnailResource>();
        ((ThumbnailResource)resource).DataLength.Should().Be(64);
    }

    [Fact]
    public void Factory_CreatesEmptyResourceCorrectly()
    {
        var factory = new ThumbnailResourceFactory();

        var resource = factory.CreateEmpty(TestKey);

        resource.Should().BeOfType<ThumbnailResource>();
        ((ThumbnailResource)resource).DataLength.Should().Be(0);
    }

    [Theory]
    [InlineData(0x0D338A3A)]
    [InlineData(0x16CCF748)]
    [InlineData(0x3BD45407)]
    [InlineData(0x3C1AF1F2)]
    [InlineData(0x3C2A8647)]
    [InlineData(0x5B282D45)]
    [InlineData(0xCD9DE247)]
    [InlineData(0xE18CAEE2)]
    [InlineData(0xE254AE6E)]
    public void AllTypeIds_CreateSuccessfully(uint typeId)
    {
        var key = new ResourceKey(typeId, 0, 0);
        var data = CreateFakeJpegWithoutAlfa(50);

        var thumb = new ThumbnailResource(key, data);

        thumb.DataLength.Should().Be(50);
    }

    /// <summary>
    /// Creates fake JPEG data without ALFA marker.
    /// </summary>
    private static byte[] CreateFakeJpegWithoutAlfa(int length)
    {
        var data = new byte[length];
        // Fill with JPEG-like data (SOI marker at start)
        if (length >= 2)
        {
            data[0] = 0xFF;
            data[1] = 0xD8; // JPEG SOI marker
        }
        // Fill rest with random-ish data (avoiding ALFA pattern)
        for (int i = 2; i < length; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }
        return data;
    }

    /// <summary>
    /// Creates fake JPEG data with ALFA marker and embedded PNG.
    /// </summary>
    private static byte[] CreateFakeJpegWithAlfa(byte[] pngData)
    {
        // Total length: 32 bytes header + PNG data + some trailing bytes
        var length = 32 + pngData.Length + 10; // extra trailing bytes
        var data = new byte[length];

        // Fill with JPEG-like data
        data[0] = 0xFF;
        data[1] = 0xD8; // JPEG SOI marker
        for (int i = 2; i < 24; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }

        // Write ALFA magic at offset 24
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24), AlfaMagic);

        // Write PNG length at offset 28 (big-endian!)
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(28), pngData.Length);

        // Write PNG data at offset 32
        pngData.CopyTo(data.AsSpan(32));

        // Fill trailing bytes
        for (int i = 32 + pngData.Length; i < data.Length; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }

        return data;
    }
}
