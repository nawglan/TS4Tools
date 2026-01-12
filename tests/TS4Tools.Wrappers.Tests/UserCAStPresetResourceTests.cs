using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="UserCAStPresetResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/UserCAStPresetResource/UserCAStPresetResource.cs
/// - Format:
///   - version: uint32 (default: 3)
///   - unknown1-3: uint32 each
///   - preset count: int32
///   - presets: List of Preset entries
/// - Type ID: 0x0591B1AF
/// </summary>
public class UserCAStPresetResourceTests
{
    // UserCAStPreset resource type ID - from legacy handler registration
    private static readonly ResourceKey TestKey = new(0x0591B1AF, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(3);
        resource.Unknown1.Should().Be(0);
        resource.Unknown2.Should().Be(0);
        resource.Unknown3.Should().Be(0);
        resource.Count.Should().Be(0);
        resource.Presets.Should().BeEmpty();
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var preset = new UserCAStPresetResource.Preset("<xml>test</xml>");

        resource.Add(preset);

        resource.Count.Should().Be(1);
        resource.Presets[0].Xml.Should().Be("<xml>test</xml>");
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Remove_ExistingPreset_ReturnsTrue()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var preset = new UserCAStPresetResource.Preset("<xml>test</xml>");
        resource.Add(preset);

        bool removed = resource.Remove(preset);

        removed.Should().BeTrue();
        resource.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveAt_ValidIndex_RemovesPreset()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(new UserCAStPresetResource.Preset("first"));
        resource.Add(new UserCAStPresetResource.Preset("second"));

        resource.RemoveAt(0);

        resource.Count.Should().Be(1);
        resource.Presets[0].Xml.Should().Be("second");
    }

    [Fact]
    public void Clear_RemovesAllPresets()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(new UserCAStPresetResource.Preset("a"));
        resource.Add(new UserCAStPresetResource.Preset("b"));

        resource.Clear();

        resource.Count.Should().Be(0);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Indexer_GetSet_Works()
    {
        var resource = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(new UserCAStPresetResource.Preset("original"));

        resource[0] = new UserCAStPresetResource.Preset("updated");

        resource[0].Xml.Should().Be("updated");
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new UserCAStPresetResource(TestKey, ReadOnlyMemory<byte>.Empty)
        {
            Version = 3,
            Unknown1 = 100,
            Unknown2 = 200,
            Unknown3 = 300
        };

        var preset1 = new UserCAStPresetResource.Preset("<preset1>content</preset1>")
        {
            Unknown1 = 1,
            Unknown2 = 2,
            Unknown3 = 12345,
            Unknown4 = 4,
            Unknown5 = 5,
            Unknown6 = 6
        };
        var preset2 = new UserCAStPresetResource.Preset("<preset2>日本語</preset2>")
        {
            Unknown1 = 10,
            Unknown2 = 20,
            Unknown3 = 67890,
            Unknown4 = 40,
            Unknown5 = 50,
            Unknown6 = 60
        };

        original.Add(preset1);
        original.Add(preset2);

        // Serialize
        var serialized = original.Data;

        // Parse
        var parsed = new UserCAStPresetResource(TestKey, serialized);

        parsed.Version.Should().Be(original.Version);
        parsed.Unknown1.Should().Be(original.Unknown1);
        parsed.Unknown2.Should().Be(original.Unknown2);
        parsed.Unknown3.Should().Be(original.Unknown3);
        parsed.Count.Should().Be(original.Count);

        for (int i = 0; i < original.Count; i++)
        {
            parsed.Presets[i].Xml.Should().Be(original.Presets[i].Xml);
            parsed.Presets[i].Unknown1.Should().Be(original.Presets[i].Unknown1);
            parsed.Presets[i].Unknown2.Should().Be(original.Presets[i].Unknown2);
            parsed.Presets[i].Unknown3.Should().Be(original.Presets[i].Unknown3);
            parsed.Presets[i].Unknown4.Should().Be(original.Presets[i].Unknown4);
            parsed.Presets[i].Unknown5.Should().Be(original.Presets[i].Unknown5);
            parsed.Presets[i].Unknown6.Should().Be(original.Presets[i].Unknown6);
        }
    }

    [Fact]
    public void Parse_ValidData_ReadsCorrectly()
    {
        // Manually construct minimal valid data
        // Header: version=3, unknown1=1, unknown2=2, unknown3=3
        // Presets: count=1, one preset with empty XML and all zeros
        var data = new List<byte>();

        // Header
        data.AddRange(BitConverter.GetBytes((uint)3));   // version
        data.AddRange(BitConverter.GetBytes((uint)1));   // unknown1
        data.AddRange(BitConverter.GetBytes((uint)2));   // unknown2
        data.AddRange(BitConverter.GetBytes((uint)3));   // unknown3
        data.AddRange(BitConverter.GetBytes(1));         // preset count

        // Preset 1
        data.AddRange(BitConverter.GetBytes(2));         // XML length (2 chars)
        data.AddRange(System.Text.Encoding.Unicode.GetBytes("AB")); // XML content
        data.Add(11);                                     // unknown1
        data.Add(22);                                     // unknown2
        data.AddRange(BitConverter.GetBytes((uint)333)); // unknown3
        data.Add(44);                                     // unknown4
        data.Add(55);                                     // unknown5
        data.Add(66);                                     // unknown6

        var resource = new UserCAStPresetResource(TestKey, data.ToArray());

        resource.Version.Should().Be(3);
        resource.Unknown1.Should().Be(1);
        resource.Unknown2.Should().Be(2);
        resource.Unknown3.Should().Be(3);
        resource.Count.Should().Be(1);
        resource.Presets[0].Xml.Should().Be("AB");
        resource.Presets[0].Unknown1.Should().Be(11);
        resource.Presets[0].Unknown2.Should().Be(22);
        resource.Presets[0].Unknown3.Should().Be(333);
        resource.Presets[0].Unknown4.Should().Be(44);
        resource.Presets[0].Unknown5.Should().Be(55);
        resource.Presets[0].Unknown6.Should().Be(66);
    }

    [Fact]
    public void Parse_TooShort_ThrowsException()
    {
        var data = new byte[] { 0x03, 0x00, 0x00, 0x00 }; // Only 4 bytes (incomplete header)

        var act = () => new UserCAStPresetResource(TestKey, data);

        act.Should().Throw<ResourceFormatException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_InvalidPresetCount_ThrowsException()
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes((uint)3));   // version
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown1
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown2
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown3
        data.AddRange(BitConverter.GetBytes(-1));        // preset count (negative)

        var act = () => new UserCAStPresetResource(TestKey, data.ToArray());

        act.Should().Throw<ResourceFormatException>().WithMessage("*Invalid preset count*");
    }

    [Fact]
    public void Parse_EmptyXml_Works()
    {
        var data = new List<byte>();

        // Header
        data.AddRange(BitConverter.GetBytes((uint)3));   // version
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown1
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown2
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown3
        data.AddRange(BitConverter.GetBytes(1));         // preset count

        // Preset with empty XML
        data.AddRange(BitConverter.GetBytes(0));         // XML length = 0
        data.Add(0);                                      // unknown1
        data.Add(0);                                      // unknown2
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown3
        data.Add(0);                                      // unknown4
        data.Add(0);                                      // unknown5
        data.Add(0);                                      // unknown6

        var resource = new UserCAStPresetResource(TestKey, data.ToArray());

        resource.Count.Should().Be(1);
        resource.Presets[0].Xml.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultiplePresets_Works()
    {
        var data = new List<byte>();

        // Header
        data.AddRange(BitConverter.GetBytes((uint)3));   // version
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown1
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown2
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown3
        data.AddRange(BitConverter.GetBytes(3));         // preset count = 3

        // Add 3 presets
        for (int i = 0; i < 3; i++)
        {
            var xmlContent = $"P{i}";
            data.AddRange(BitConverter.GetBytes(xmlContent.Length));
            data.AddRange(System.Text.Encoding.Unicode.GetBytes(xmlContent));
            data.Add((byte)i);                           // unknown1
            data.Add((byte)(i + 10));                    // unknown2
            data.AddRange(BitConverter.GetBytes((uint)(i * 100))); // unknown3
            data.Add((byte)(i + 20));                    // unknown4
            data.Add((byte)(i + 30));                    // unknown5
            data.Add((byte)(i + 40));                    // unknown6
        }

        var resource = new UserCAStPresetResource(TestKey, data.ToArray());

        resource.Count.Should().Be(3);
        for (int i = 0; i < 3; i++)
        {
            resource.Presets[i].Xml.Should().Be($"P{i}");
            resource.Presets[i].Unknown1.Should().Be((byte)i);
        }
    }

    [Fact]
    public void Preset_Equality_WorksCorrectly()
    {
        var preset1 = new UserCAStPresetResource.Preset("test")
        {
            Unknown1 = 1,
            Unknown2 = 2,
            Unknown3 = 3,
            Unknown4 = 4,
            Unknown5 = 5,
            Unknown6 = 6
        };

        var preset2 = new UserCAStPresetResource.Preset("test")
        {
            Unknown1 = 1,
            Unknown2 = 2,
            Unknown3 = 3,
            Unknown4 = 4,
            Unknown5 = 5,
            Unknown6 = 99 // Different unknown6 - excluded from equality per legacy
        };

        var preset3 = new UserCAStPresetResource.Preset("different")
        {
            Unknown1 = 1,
            Unknown2 = 2,
            Unknown3 = 3,
            Unknown4 = 4,
            Unknown5 = 5,
            Unknown6 = 6
        };

        preset1.Equals(preset2).Should().BeTrue("Unknown6 is excluded from equality");
        preset1.Equals(preset3).Should().BeFalse("Different XML content");
        preset1.GetHashCode().Should().Be(preset2.GetHashCode());
    }

    [Fact]
    public void Preset_ToString_ShowsPreview()
    {
        var shortPreset = new UserCAStPresetResource.Preset("<short>content</short>");
        var longXml = new string('x', 200);
        var longPreset = new UserCAStPresetResource.Preset(longXml);

        shortPreset.ToString().Should().Contain("<short>content</short>");
        longPreset.ToString().Should().Contain("...");
        longPreset.ToString().Length.Should().BeLessThan(200);
    }

    [Fact]
    public void Factory_Create_Works()
    {
        var factory = new UserCAStPresetResourceFactory();
        var data = BuildMinimalValidData();

        var resource = factory.Create(TestKey, data);

        resource.Should().BeOfType<UserCAStPresetResource>();
        ((UserCAStPresetResource)resource).Version.Should().Be(3);
    }

    [Fact]
    public void Factory_CreateEmpty_Works()
    {
        var factory = new UserCAStPresetResourceFactory();

        var resource = factory.CreateEmpty(TestKey);

        resource.Should().BeOfType<UserCAStPresetResource>();
        ((UserCAStPresetResource)resource).Count.Should().Be(0);
    }

    private static byte[] BuildMinimalValidData()
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes((uint)3));   // version
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown1
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown2
        data.AddRange(BitConverter.GetBytes((uint)0));   // unknown3
        data.AddRange(BitConverter.GetBytes(0));         // preset count = 0
        return data.ToArray();
    }
}
