using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class TextResourceTests
{
    private static readonly ResourceKey TestKey = new(0x03B33DDF, 0, 0);

    [Fact]
    public void CreateEmpty_HasEmptyText()
    {
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Text.Should().BeEmpty();
        resource.HasBom.Should().BeFalse();
        resource.LineCount.Should().Be(0);
    }

    [Fact]
    public void Parse_PlainText_ReadsCorrectly()
    {
        const string text = "Hello, World!";
        var data = Encoding.UTF8.GetBytes(text);

        var resource = new TextResource(TestKey, data);

        resource.Text.Should().Be(text);
        resource.HasBom.Should().BeFalse();
    }

    [Fact]
    public void Parse_TextWithBom_DetectsBom()
    {
        const string text = "Hello, World!";
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var textBytes = Encoding.UTF8.GetBytes(text);
        var data = new byte[bom.Length + textBytes.Length];
        bom.CopyTo(data, 0);
        textBytes.CopyTo(data, bom.Length);

        var resource = new TextResource(TestKey, data);

        resource.Text.Should().Be(text);
        resource.HasBom.Should().BeTrue();
    }

    [Fact]
    public void SetText_MarksAsDirty()
    {
        var resource = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Text = "New content";

        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesText()
    {
        const string text = "Line 1\nLine 2\nLine 3";
        var original = new TextResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Text = text;

        var serialized = original.Data;
        var parsed = new TextResource(TestKey, serialized);

        parsed.Text.Should().Be(text);
    }

    [Fact]
    public void RoundTrip_PreservesBom()
    {
        const string text = "Content with BOM";
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var textBytes = Encoding.UTF8.GetBytes(text);
        var data = new byte[bom.Length + textBytes.Length];
        bom.CopyTo(data, 0);
        textBytes.CopyTo(data, bom.Length);

        var original = new TextResource(TestKey, data);
        var serialized = original.Data;
        var parsed = new TextResource(TestKey, serialized);

        parsed.Text.Should().Be(text);
        parsed.HasBom.Should().BeTrue();
    }

    [Fact]
    public void LineCount_CountsNewlines()
    {
        const string text = "Line 1\nLine 2\nLine 3";
        var data = Encoding.UTF8.GetBytes(text);

        var resource = new TextResource(TestKey, data);

        resource.LineCount.Should().Be(3);
    }

    [Fact]
    public void IsXml_DetectsXmlDeclaration()
    {
        const string xml = "<?xml version=\"1.0\"?>\n<root></root>";
        var data = Encoding.UTF8.GetBytes(xml);

        var resource = new TextResource(TestKey, data);

        resource.IsXml.Should().BeTrue();
    }

    [Fact]
    public void IsXml_DetectsXmlTags()
    {
        const string xml = "<root>\n  <child>value</child>\n</root>";
        var data = Encoding.UTF8.GetBytes(xml);

        var resource = new TextResource(TestKey, data);

        resource.IsXml.Should().BeTrue();
    }

    [Fact]
    public void IsXml_FalseForPlainText()
    {
        const string text = "Just some plain text without XML";
        var data = Encoding.UTF8.GetBytes(text);

        var resource = new TextResource(TestKey, data);

        resource.IsXml.Should().BeFalse();
    }

    [Fact]
    public void Encoding_IsUtf8()
    {
        TextResource.Encoding.Should().Be(Encoding.UTF8);
    }

    [Fact]
    public void Parse_Unicode_PreservesCharacters()
    {
        const string text = "Unicode: 日本語 emoji: 🎮";
        var data = Encoding.UTF8.GetBytes(text);

        var resource = new TextResource(TestKey, data);

        resource.Text.Should().Be(text);
    }

    /// <summary>
    /// Regression (real data): 0x02D5DF13 (JAZZ) is listed in TextResources.txt but holds binary
    /// "DATA" content. Legacy TextResource.Stream returns the original stream (TextResource.cs line 55),
    /// so the bytes must survive even though they are not valid UTF-8.
    /// </summary>
    [Fact]
    public void Data_BinaryContent_RoundTripsByteIdentical()
    {
        byte[] data = [0x44, 0x41, 0x54, 0x41, 0x01, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xD9, 0x54, 0xBE, 0xCE, 0xC5, 0x9D, 0x1C, 0x81];

        var resource = new TextResource(new ResourceKey(0x02D5DF13, 0, 0), data);

        resource.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void Data_AfterTextChange_IsReencoded()
    {
        byte[] data = [0xEF, 0xBB, 0xBF, (byte)'a', 0xFF];
        var resource = new TextResource(TestKey, data);

        resource.Text = "b";

        resource.Data.ToArray().Should().Equal(0xEF, 0xBB, 0xBF, (byte)'b');
    }
}
