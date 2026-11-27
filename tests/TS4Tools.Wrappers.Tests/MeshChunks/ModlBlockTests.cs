using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for ModlBlock. Layouts follow legacy MODL.cs: tag, version, int32 LOD count, bounding box; then
/// for 258 &lt;= version &lt; 0x300: int32 extra-bounds count + boxes, fade type, custom fade distance;
/// for version &gt;= 0x300: 20 unknown bytes; then the LOD entries (chunk ref, flags, id, minZ, maxZ).
/// </summary>
public class ModlBlockTests
{
    private static byte[] CreateModl(uint version, int lodCount, int extraBoxes = 0)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'M'); w.Write((byte)'O'); w.Write((byte)'D'); w.Write((byte)'L');
        w.Write(version);
        w.Write(lodCount);
        w.Write(-1f); w.Write(-2f); w.Write(-3f); w.Write(4f); w.Write(5f); w.Write(6f);

        if (version >= 258 && version < 0x300)
        {
            w.Write(extraBoxes);
            for (int i = 0; i < extraBoxes; i++)
            {
                w.Write((float)i); w.Write(0f); w.Write(0f); w.Write(i + 1f); w.Write(1f); w.Write(1f);
            }
            w.Write(2u);    // fade type
            w.Write(12.5f); // custom fade distance
        }
        else if (version >= 0x300)
        {
            for (int i = 0; i < 20; i++) w.Write((byte)(i + 1));
        }

        for (int i = 0; i < lodCount; i++)
        {
            w.Write((uint)(i + 1));              // chunk reference
            w.Write(i == 0 ? 0x1u : 0x0u);       // flags (Portal on first)
            w.Write(i == 0 ? 0u : 0x10000u + (uint)i); // id
            w.Write(i * 10f);                    // min z
            w.Write((i + 1) * 10f);              // max z
        }
        return ms.ToArray();
    }

    [Fact]
    public void Parse_Version256_HasNoExtraFields()
    {
        var block = new ModlBlock(CreateModl(256, 2));

        block.Tag.Should().Be("MODL");
        block.ResourceType.Should().Be(0x01661233u);
        block.Version.Should().Be(256u);
        block.Bounds.Should().Be(new BoundingBox(-1, -2, -3, 4, 5, 6));
        block.ExtraBounds.Should().BeEmpty();
        block.FadeType.Should().Be(0);
        block.LodEntryCount.Should().Be(2);
        block.Entries[0].ModelLodIndex.RawValue.Should().Be(1u);
        block.Entries[0].Flags.Should().Be(LodInfo.Portal);
        block.Entries[0].Id.Should().Be(LodId.HighDetail);
        block.Entries[1].Id.Should().Be(LodId.HighDetailShadow + 1);
        block.Entries[1].MinZValue.Should().Be(10f);
        block.Entries[1].MaxZValue.Should().Be(20f);
    }

    [Fact]
    public void Parse_Version258_ReadsExtraBoundsAndFade()
    {
        var block = new ModlBlock(CreateModl(258, 1, extraBoxes: 2));

        block.ExtraBounds.Should().HaveCount(2);
        block.ExtraBounds[1].Min.X.Should().Be(1f);
        block.FadeType.Should().Be(2u);
        block.CustomFadeDistance.Should().Be(12.5f);
        block.Entries.Should().ContainSingle();
    }

    [Fact]
    public void Parse_Version300_ReadsUnknownBlock()
    {
        var block = new ModlBlock(CreateModl(0x300, 1));

        block.Unknown.Should().Equal(Enumerable.Range(1, 20).Select(i => (byte)i));
        block.ExtraBounds.Should().BeEmpty();
        block.Entries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(256u, 0, 0)]
    [InlineData(256u, 3, 0)]
    [InlineData(257u, 1, 0)]
    [InlineData(258u, 2, 0)]
    [InlineData(258u, 2, 3)]
    [InlineData(0x2FFu, 1, 1)]
    [InlineData(0x300u, 2, 0)]
    [InlineData(0x301u, 0, 0)]
    public void Serialize_RoundTripsByteIdentical(uint version, int lods, int extra)
    {
        var original = CreateModl(version, lods, extra);

        new ModlBlock(original).Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Serialize_ExtraBoundsOnOldVersion_RaisesVersionTo258()
    {
        var block = new ModlBlock(CreateModl(256, 1));
        block.ExtraBounds.Add(new BoundingBox(0, 0, 0, 1, 1, 1));

        var reparsed = new ModlBlock(block.Serialize().Span);

        reparsed.Version.Should().Be(258u);
        reparsed.ExtraBounds.Should().ContainSingle();
        reparsed.Entries.Should().ContainSingle();
    }

    [Fact]
    public void SetUnknown_WrongLength_Throws()
    {
        var block = new ModlBlock();

        var action = () => block.SetUnknown(new byte[19]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_InvalidTag_Throws()
    {
        var data = CreateModl(256, 0);
        data[1] = (byte)'X';

        var action = () => new ModlBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid MODL tag*");
    }

    [Fact]
    public void Parse_LodCountLargerThanData_Throws()
    {
        var data = CreateModl(256, 1);
        BitConverter.GetBytes(500).CopyTo(data, 8);

        var action = () => new ModlBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Parse_NegativeExtraBoundsCount_Throws()
    {
        var data = CreateModl(258, 0, extraBoxes: 0);
        BitConverter.GetBytes(-5).CopyTo(data, 12 + 24);

        var action = () => new ModlBlock(data);

        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_Truncated_Throws()
    {
        foreach (uint version in new[] { 256u, 258u, 0x300u })
        {
            var data = CreateModl(version, 2, extraBoxes: version == 258 ? 1 : 0);
            for (int len = 0; len < data.Length; len++)
            {
                var prefix = data.AsSpan(0, len).ToArray();
                var action = () => new ModlBlock(prefix);
                action.Should().Throw<InvalidDataException>($"version {version} prefix of {len} bytes is truncated");
            }
        }
    }

    [Fact]
    public void Registry_CreatesModlBlock()
    {
        RcolBlockRegistry.IsRegistered(ModlBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.CreateBlock(ModlBlock.TypeId, CreateModl(256, 1)).Should().BeOfType<ModlBlock>();
    }

    [Fact]
    public void EmptyConstructor_UsesLegacyDefaults()
    {
        var block = new ModlBlock();

        block.Version.Should().Be(256u);
        var expected = CreateModl(256, 0);
        Array.Clear(expected, 12, 24); // default bounding box is all zeros
        block.Serialize().ToArray().Should().Equal(expected);
    }
}
