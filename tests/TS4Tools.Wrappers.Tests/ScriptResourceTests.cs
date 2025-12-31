using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="ScriptResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ScriptResource/ScriptResource.cs
/// - Type ID: 0x073FAA07 (Encrypted Signed Assembly)
/// - Uses XOR encryption with md5table-derived seed
/// </summary>
public class ScriptResourceTests
{
    private static readonly ResourceKey TestKey = new(0x073FAA07, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(1);
        resource.GameVersion.Should().BeEmpty();
        resource.Unknown2.Should().Be(ScriptResource.DefaultUnknown2);
        resource.Md5Sum.Length.Should().Be(64);
        resource.AssemblyData.Length.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.GameVersion.Should().Be(original.GameVersion);
        parsed.Unknown2.Should().Be(original.Unknown2);
        parsed.AssemblyData.Length.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_WithAssemblyData_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var assemblyData = new byte[1024];
        for (int i = 0; i < assemblyData.Length; i++)
        {
            assemblyData[i] = (byte)(i % 256);
        }
        original.Assembly = assemblyData;

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public void RoundTrip_LargeAssembly_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // Use 10KB of test data (multiple 512-byte blocks)
        var assemblyData = new byte[10240];
        var random = new Random(42); // Fixed seed for reproducibility
        random.NextBytes(assemblyData);
        original.Assembly = assemblyData;

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public void RoundTrip_Version2WithGameVersion_PreservesGameVersion()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 2;
        original.GameVersion = "1.100.147.1030";
        original.Assembly = [0x01, 0x02, 0x03, 0x04];

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.Version.Should().Be(2);
        parsed.GameVersion.Should().Be("1.100.147.1030");
    }

    [Fact]
    public void RoundTrip_NonBlockAlignedData_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // Use data size that doesn't align to 512-byte blocks
        var assemblyData = new byte[777];
        for (int i = 0; i < assemblyData.Length; i++)
        {
            assemblyData[i] = (byte)(i * 3 % 256);
        }
        original.Assembly = assemblyData;

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        // Parsed data may be padded to block boundary, but original data should be preserved
        parsed.AssemblyData.Span[..777].ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public void SetMd5Sum_With64Bytes_Succeeds()
    {
        var resource = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var md5 = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            md5[i] = (byte)i;
        }

        resource.SetMd5Sum(md5);

        resource.Md5Sum.ToArray().Should().BeEquivalentTo(md5);
    }

    [Fact]
    public void SetMd5Sum_WithWrongSize_ThrowsArgumentException()
    {
        var resource = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        var md5 = new byte[32]; // Wrong size

        var act = () => resource.SetMd5Sum(md5);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*64 bytes*");
    }

    [Fact]
    public void Assembly_Set_MarksResourceAsChanged()
    {
        var resource = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        bool changed = false;
        resource.Changed += (s, e) => changed = true;

        resource.Assembly = [0x01, 0x02, 0x03];

        changed.Should().BeTrue();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Parse_TooShortData_ThrowsResourceFormatException()
    {
        var act = () => new ScriptResource(TestKey, new byte[5]);

        act.Should().Throw<ResourceFormatException>();
    }

    [Fact]
    public void Parse_InvalidBlockCount_ThrowsResourceFormatException()
    {
        // Create data with an excessive block count
        var data = new byte[200];
        data[0] = 1; // version
        // unknown2 at offset 1-4
        // md5sum at offset 5-68
        // Put an excessive block count at offset 69-70
        data[69] = 0xFF;
        data[70] = 0xFF; // 65535 blocks - exceeds max but would require huge data

        var act = () => new ScriptResource(TestKey, data);

        // Should throw because data is too short for claimed block count
        act.Should().Throw<ResourceFormatException>();
    }

    [Fact]
    public void RoundTrip_ExactBlockSize_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // Exactly 512 bytes (1 block)
        var assemblyData = new byte[512];
        for (int i = 0; i < assemblyData.Length; i++)
        {
            assemblyData[i] = (byte)(i % 256);
        }
        original.Assembly = assemblyData;

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }

    [Fact]
    public void RoundTrip_TwoBlocks_PreservesData()
    {
        var original = new ScriptResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // Exactly 1024 bytes (2 blocks)
        var assemblyData = new byte[1024];
        for (int i = 0; i < assemblyData.Length; i++)
        {
            assemblyData[i] = (byte)((i * 7) % 256);
        }
        original.Assembly = assemblyData;

        var data = original.Data.ToArray();
        var parsed = new ScriptResource(TestKey, data);

        parsed.AssemblyData.ToArray().Should().BeEquivalentTo(assemblyData);
    }
}
