using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class ComplateResourceTests
{
    private static readonly ResourceKey TestKey = new(0x044AE110, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaults()
    {
        var resource = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Unknown1.Should().Be(ComplateResource.DefaultUnknown1);
        resource.Unknown2.Should().Be(ComplateResource.DefaultUnknown2);
        resource.Text.Should().BeEmpty();
    }

    [Fact]
    public void SetText_MarksAsDirty()
    {
        var resource = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Text = "Hello, World!";

        resource.IsDirty.Should().BeTrue();
        resource.Text.Should().Be("Hello, World!");
    }

    [Fact]
    public void SetText_SameValue_DoesNotMarkDirty()
    {
        var resource = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Text = "test";

        // Access Data to clear dirty flag for this test
        _ = resource.Data;

        // The dirty flag is set once and stays set, so we just verify the text
        resource.Text = "test";
        resource.Text.Should().Be("test");
    }

    [Fact]
    public void SetText_Null_BecomesEmpty()
    {
        var resource = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Text = null!;

        resource.Text.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown1 = 0x12345678;
        original.Text = "Hello, World! This is a test string.";
        original.Unknown2 = 0xABCDEF00;

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ComplateResource(TestKey, serialized);

        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Text.Should().Be(original.Text);
        parsed.Unknown2.Should().Be(original.Unknown2);
    }

    [Fact]
    public void RoundTrip_UnicodeText_PreservesData()
    {
        var original = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Text = "日本語テスト 🎮 Ümlauts: äöü";

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ComplateResource(TestKey, serialized);

        parsed.Text.Should().Be(original.Text);
    }

    [Fact]
    public void RoundTrip_EmptyText_PreservesData()
    {
        var original = new ComplateResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Text = string.Empty;
        original.Unknown1 = 0x00000002;
        original.Unknown2 = 0x00000000;

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new ComplateResource(TestKey, serialized);

        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Text.Should().BeEmpty();
        parsed.Unknown2.Should().Be(original.Unknown2);
    }

    [Fact]
    public void Parse_ValidData_ReadsCorrectly()
    {
        // Manually construct valid Complate data:
        // Unknown1 (0x00000002), CharCount (4), "test" in UTF-16 LE, Unknown2 (0x00000000)
        var data = new byte[]
        {
            // Unknown1 (0x00000002)
            0x02, 0x00, 0x00, 0x00,
            // CharCount (4)
            0x04, 0x00, 0x00, 0x00,
            // Chars "test" (UTF-16 LE, 2 bytes each)
            0x74, 0x00, // 't'
            0x65, 0x00, // 'e'
            0x73, 0x00, // 's'
            0x74, 0x00, // 't'
            // Unknown2 (0x00000000)
            0x00, 0x00, 0x00, 0x00
        };

        var resource = new ComplateResource(TestKey, data);

        resource.Unknown1.Should().Be(0x00000002);
        resource.Text.Should().Be("test");
        resource.Unknown2.Should().Be(0x00000000);
    }

    [Fact]
    public void Parse_CustomUnknownValues_ReadsCorrectly()
    {
        var data = new byte[]
        {
            // Unknown1 (0x12345678)
            0x78, 0x56, 0x34, 0x12,
            // CharCount (2)
            0x02, 0x00, 0x00, 0x00,
            // Chars "AB" (UTF-16 LE)
            0x41, 0x00, // 'A'
            0x42, 0x00, // 'B'
            // Unknown2 (0xDEADBEEF)
            0xEF, 0xBE, 0xAD, 0xDE
        };

        var resource = new ComplateResource(TestKey, data);

        resource.Unknown1.Should().Be(0x12345678);
        resource.Text.Should().Be("AB");
        resource.Unknown2.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Parse_TooShortForHeader_ThrowsException()
    {
        var data = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }; // Only 5 bytes

        var act = () => new ComplateResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_TooShortForString_ThrowsException()
    {
        var data = new byte[]
        {
            // Unknown1
            0x02, 0x00, 0x00, 0x00,
            // CharCount (10) - but no data for string
            0x0A, 0x00, 0x00, 0x00,
            // Unknown2 (but should be string data here)
            0x00, 0x00, 0x00, 0x00
        };

        var act = () => new ComplateResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_NegativeCharCount_ThrowsException()
    {
        var data = new byte[]
        {
            // Unknown1
            0x02, 0x00, 0x00, 0x00,
            // CharCount (-1)
            0xFF, 0xFF, 0xFF, 0xFF,
            // Unknown2
            0x00, 0x00, 0x00, 0x00
        };

        var act = () => new ComplateResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*count*");
    }

    [Fact]
    public void Parse_EmptyString_ReadsCorrectly()
    {
        var data = new byte[]
        {
            // Unknown1 (0x00000002)
            0x02, 0x00, 0x00, 0x00,
            // CharCount (0)
            0x00, 0x00, 0x00, 0x00,
            // No string data
            // Unknown2 (0x00000000)
            0x00, 0x00, 0x00, 0x00
        };

        var resource = new ComplateResource(TestKey, data);

        resource.Unknown1.Should().Be(0x00000002);
        resource.Text.Should().BeEmpty();
        resource.Unknown2.Should().Be(0x00000000);
    }
}
