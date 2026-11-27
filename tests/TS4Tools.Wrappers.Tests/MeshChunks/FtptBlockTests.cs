using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for FtptBlock. Byte layouts follow legacy FTPT.cs: every list (height overrides, polygon points,
/// areas) has a ONE BYTE count, areas are name, priority, areaTypeFlags, points, intersectionObjectType,
/// allowIntersectionTypes, surfaceTypeFlags, surfaceAttributeFlags, deprecatedLevelOffset (1 byte) and a
/// bounding box stored as minX, minZ, maxX, maxZ, minY, maxY.
/// </summary>
public class FtptBlockTests
{
    private static BinaryWriter Header(MemoryStream ms, uint version, ulong instance, uint type, uint group)
    {
        var w = new BinaryWriter(ms);
        w.Write((byte)'F');
        w.Write((byte)'T');
        w.Write((byte)'P');
        w.Write((byte)'T');
        w.Write(version);
        w.Write(instance);
        w.Write(type);
        w.Write(group);
        return w;
    }

    private static void WriteArea(BinaryWriter w, uint name, byte priority, uint areaFlags,
        (float X, float Z)[] points, uint intersect, uint allow, uint surface, uint attribute, byte level,
        float minX, float minZ, float maxX, float maxZ, float minY, float maxY)
    {
        w.Write(name);
        w.Write(priority);
        w.Write(areaFlags);
        w.Write((byte)points.Length);
        foreach (var (x, z) in points)
        {
            w.Write(x);
            w.Write(z);
        }
        w.Write(intersect);
        w.Write(allow);
        w.Write(surface);
        w.Write(attribute);
        w.Write(level);
        w.Write(minX);
        w.Write(minZ);
        w.Write(maxX);
        w.Write(maxZ);
        w.Write(minY);
        w.Write(maxY);
    }

    private static byte[] CreateFtptWithHeightOverrides()
    {
        using var ms = new MemoryStream();
        using var w = Header(ms, 12, 0x123456789ABCDEF0UL, 1, 0x00AA55FF);

        w.Write((byte)2); // min overrides
        w.Write(0xDEADBEEFu);
        w.Write(1.5f);
        w.Write(0x01020304u);
        w.Write(-2f);

        w.Write((byte)1); // max overrides
        w.Write(0xCAFEBABEu);
        w.Write(9.25f);

        return ms.ToArray();
    }

    private static byte[] CreateFtptWithAreas(uint version = 12)
    {
        using var ms = new MemoryStream();
        using var w = Header(ms, version, 0x123456789ABCDEF0UL, 0, 0);

        w.Write((byte)2); // footprint areas
        WriteArea(w, 0x12345678, 3, 0x05, [(0f, 0f), (1f, 0f), (0.5f, 1f)],
            0x06, 0x04, 0x03, 0x02, 7, 0f, 0.25f, 1f, 1.75f, -0.5f, 2.5f);
        WriteArea(w, 0x9ABCDEF0, 255, 0x1FF, [], 0, 0, 0xFF, 0x0F, 0, 1, 2, 3, 4, 5, 6);

        w.Write((byte)1); // slot areas
        WriteArea(w, 0x0BADF00D, 1, 0x40, [(2f, 3f), (4f, 5f), (6f, 7f), (8f, 9f)],
            0x1000, 0x1000, 0x80, 0x01, 1, -1, -1, 1, 1, 0, 3);

        w.Write(12.5f); // maxHeight
        w.Write(-1.5f); // minHeight

        return ms.ToArray();
    }

    [Fact]
    public void Parse_WithHeightOverrides_ParsesCorrectly()
    {
        var block = new FtptBlock(CreateFtptWithHeightOverrides());

        block.Tag.Should().Be("FTPT");
        block.ResourceType.Should().Be(FtptBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(12);
        block.Instance.Should().Be(0x123456789ABCDEF0UL);
        block.Type.Should().Be(1);
        block.Group.Should().Be(0x00AA55FFu);
        block.HasHeightOverrides.Should().BeTrue();

        block.MinHeightOverrides.Should().HaveCount(2);
        block.MinHeightOverrides[0].NameHash.Should().Be(0xDEADBEEFu);
        block.MinHeightOverrides[0].Height.Should().Be(1.5f);
        block.MinHeightOverrides[1].Height.Should().Be(-2f);
        block.MaxHeightOverrides.Should().ContainSingle().Which.NameHash.Should().Be(0xCAFEBABEu);
        block.FootprintAreas.Should().BeEmpty();
        block.SlotAreas.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithAreas_FollowsLegacyAreaLayout()
    {
        var block = new FtptBlock(CreateFtptWithAreas());

        block.HasHeightOverrides.Should().BeFalse();
        block.MaxHeight.Should().Be(12.5f);
        block.MinHeight.Should().Be(-1.5f);
        block.FootprintAreas.Should().HaveCount(2);
        block.SlotAreas.Should().HaveCount(1);

        var a = block.FootprintAreas[0];
        a.Name.Should().Be(0x12345678u);
        a.Priority.Should().Be(3);
        a.AreaTypeFlags.Should().Be(FtptFootprintPolyFlags.Placement | FtptFootprintPolyFlags.Enabled);
        a.PolygonPoints.Should().HaveCount(3);
        a.PolygonPoints[2].Should().Be(new FtptPolygonPoint(0.5f, 1f));
        a.IntersectionObjectType.Should().Be(FtptIntersectionFlags.Walls | FtptIntersectionFlags.Objects);
        a.AllowIntersectionTypes.Should().Be(FtptIntersectionFlags.Objects);
        a.SurfaceTypeFlags.Should().Be(FtptSurfaceTypeFlags.Terrain | FtptSurfaceTypeFlags.Floor);
        a.SurfaceAttributeFlags.Should().Be(FtptSurfaceAttribute.Outside);
        a.DeprecatedLevelOffset.Should().Be(7);
        // On disk: minX, minZ, maxX, maxZ, minY, maxY
        a.BoundingBox3D.Min.X.Should().Be(0f);
        a.BoundingBox3D.Min.Z.Should().Be(0.25f);
        a.BoundingBox3D.Max.X.Should().Be(1f);
        a.BoundingBox3D.Max.Z.Should().Be(1.75f);
        a.BoundingBox3D.Min.Y.Should().Be(-0.5f);
        a.BoundingBox3D.Max.Y.Should().Be(2.5f);

        var b = block.FootprintAreas[1];
        b.Priority.Should().Be(255);
        b.PolygonPoints.Should().BeEmpty();
        b.AreaTypeFlags.Should().Be((FtptFootprintPolyFlags)0x1FF);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(6u)]
    [InlineData(7u)]
    [InlineData(12u)]
    public void Serialize_Areas_RoundTripsByteIdenticalForAllVersions(uint version)
    {
        var original = CreateFtptWithAreas(version);
        var block = new FtptBlock(original);

        block.Version.Should().Be(version);
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Serialize_HeightOverrides_RoundTripsByteIdentical()
    {
        var original = CreateFtptWithHeightOverrides();

        new FtptBlock(original).Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Serialize_AfterEdit_WritesNewValues()
    {
        var block = new FtptBlock(CreateFtptWithAreas());
        block.FootprintAreas[0].PolygonPoints.Add(new FtptPolygonPoint(9f, 9f));
        block.FootprintAreas[0].DeprecatedLevelOffset = 9;
        block.MaxHeight = 1f;

        var reparsed = new FtptBlock(block.Serialize().Span);

        reparsed.FootprintAreas[0].PolygonPoints.Should().HaveCount(4);
        reparsed.FootprintAreas[0].DeprecatedLevelOffset.Should().Be(9);
        reparsed.MaxHeight.Should().Be(1f);
    }

    [Fact]
    public void Serialize_MoreThan255Points_Throws()
    {
        var block = new FtptBlock(CreateFtptWithAreas());
        for (int i = 0; i < 256; i++)
            block.SlotAreas[0].PolygonPoints.Add(new FtptPolygonPoint(i, i));

        var act = () => block.Serialize();

        act.Should().Throw<InvalidOperationException>().WithMessage("*at most 255*");
    }

    [Fact]
    public void Parse_InvalidTag_ThrowsException()
    {
        var data = CreateFtptWithHeightOverrides();
        data[0] = (byte)'X';

        var action = () => new FtptBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid FTPT tag*");
    }

    [Fact]
    public void Parse_TruncatedAnywhere_ThrowsInvalidData()
    {
        var data = CreateFtptWithAreas();

        // Every strict prefix is invalid: the trailing heights are mandatory for Type == 0
        for (int len = 0; len < data.Length; len++)
        {
            var prefix = data.AsSpan(0, len).ToArray();
            var action = () => new FtptBlock(prefix);
            action.Should().Throw<InvalidDataException>($"prefix of {len} bytes is truncated");
        }
    }

    [Fact]
    public void Parse_AreaCountLargerThanData_ThrowsInvalidData()
    {
        using var ms = new MemoryStream();
        using var w = Header(ms, 12, 0, 0, 0);
        w.Write((byte)200); // claims 200 areas, none present

        var data = ms.ToArray();
        var action = () => new FtptBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*Invalid element count*");
    }

    [Fact]
    public void Registry_IsRegisteredAndCreatesFtptBlock()
    {
        RcolBlockRegistry.IsRegistered(FtptBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("FTPT").Should().BeTrue();

        var block = RcolBlockRegistry.CreateBlock(FtptBlock.TypeId, CreateFtptWithHeightOverrides());

        block.Should().BeOfType<FtptBlock>();
    }

    [Fact]
    public void EmptyBlock_SerializesToParseableAreaLayout()
    {
        var block = new FtptBlock();

        var reparsed = new FtptBlock(block.Serialize().Span);

        reparsed.Version.Should().Be(12);
        reparsed.FootprintAreas.Should().BeEmpty();
        reparsed.SlotAreas.Should().BeEmpty();
    }
}
