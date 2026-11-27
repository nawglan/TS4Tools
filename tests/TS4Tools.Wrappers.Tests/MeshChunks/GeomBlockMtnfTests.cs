using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for the structured MTNF handling in GeomBlock (legacy GEOM.cs lines 135-145, 191-198, 1295-1310).
/// </summary>
public class GeomBlockMtnfTests
{
    /// <summary>Minimal v12 GEOM with one position-only vertex and the given shader/MTNF bytes.</summary>
    private static byte[] CreateGeom(uint shader, byte[]? mtnf, uint declaredMtnfSize = uint.MaxValue,
        RcolTgiBlock[]? tgis = null)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'G'); w.Write((byte)'E'); w.Write((byte)'O'); w.Write((byte)'M');
        w.Write(0x0Cu);
        long tgiOffsetPos = ms.Position;
        w.Write(0u);
        w.Write(0u);

        w.Write(shader);
        if (shader != 0)
        {
            w.Write(declaredMtnfSize == uint.MaxValue ? (uint)mtnf!.Length : declaredMtnfSize);
            w.Write(mtnf!);
        }
        w.Write(1u); // merge group
        w.Write(2u); // sort order
        w.Write(1);  // vertex count
        w.Write(1);  // format count
        w.Write(1u); w.Write(1u); w.Write((byte)12);
        w.Write(0f); w.Write(0f); w.Write(0f);
        w.Write(1);
        w.Write((byte)2);
        w.Write(0);  // faces
        w.Write(0);  // unknown things
        w.Write(0);  // unknown things 2
        w.Write(0);  // bone hashes

        long tgiStart = ms.Position;
        tgis ??= [];
        w.Write(tgis.Length); // TGI count (legacy TGIBlockList.UnParse always writes it)
        var tgiBuf = new byte[RcolTgiBlock.Size];
        foreach (var t in tgis)
        {
            t.WriteTgiOrder(tgiBuf);
            w.Write(tgiBuf);
        }
        ms.Position = tgiOffsetPos;
        w.Write((uint)(tgiStart - (tgiOffsetPos + 4)));
        w.Write((uint)(4 + tgis.Length * RcolTgiBlock.Size));
        return ms.ToArray();
    }

    private static byte[] StructuredMtnf(float value)
    {
        var mtnf = new MtnfData();
        mtnf.ShaderData.Add(new ShaderFloat(ShaderFieldType.AlignAcrossDirection, value));
        return mtnf.Serialize();
    }

    [Fact]
    public void Parse_IndexedTextureRef_ResolvesThroughTgiBlocks()
    {
        var model = new MtnfData();
        model.ShaderData.Add(ShaderTextureRef.FromIndex(ShaderFieldType.DiffuseMap, 1));
        var mtnfBytes = model.Serialize();
        RcolTgiBlock[] tgis = [new(1, 0x00B2D882, 0), new(0xAABBCCDD11223344, 0x00B2D882, 7)];
        var original = CreateGeom(1, mtnfBytes, tgis: tgis);

        var block = new GeomBlock(original);

        block.Mtnf.Should().NotBeNull();
        block.Mtnf!.GetParameter(ShaderFieldType.DiffuseMap).Should().BeOfType<ShaderTextureRef>()
            .Which.Index.Should().Be(1);
        block.Mtnf.GetTexture(ShaderFieldType.DiffuseMap).Should().BeNull();
        block.Mtnf.GetTexture(ShaderFieldType.DiffuseMap, block.TgiBlocks)
            .Should().Be(new ResourceKey(0x00B2D882, 7, 0xAABBCCDD11223344));
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_IndexedTextureRef_OutOfRangeIndex_ResolvesToNull()
    {
        var model = new MtnfData();
        model.ShaderData.Add(ShaderTextureRef.FromIndex(ShaderFieldType.DiffuseMap, 5));

        new GeomBlock(CreateGeom(1, model.Serialize())).Mtnf!
            .GetTexture(ShaderFieldType.DiffuseMap, []).Should().BeNull();
    }

    [Fact]
    public void Parse_IndexedTextureRef_NonZeroPadding_FallsBackToRawBytes()
    {
        var model = new MtnfData();
        model.ShaderData.Add(ShaderTextureRef.FromIndex(ShaderFieldType.DiffuseMap, 0));
        var bytes = model.Serialize();
        bytes[^1] = 0x7F; // padding must be zero in the GEOM layout
        var original = CreateGeom(1, bytes);

        var block = new GeomBlock(original);

        block.Mtnf.Should().BeNull();
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_StructuredMtnf_IsExposedAndRoundTrips()
    {
        var mtnfBytes = StructuredMtnf(2.5f);
        var original = CreateGeom(1, mtnfBytes);

        var block = new GeomBlock(original);

        block.MtnfData.Should().Equal(mtnfBytes);
        block.Mtnf.Should().NotBeNull();
        block.Mtnf!.GetFloat(ShaderFieldType.AlignAcrossDirection).Should().Be(2.5f);
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Serialize_AfterEditingStructuredMtnf_WritesEdit()
    {
        var block = new GeomBlock(CreateGeom(1, StructuredMtnf(2.5f)));

        ((ShaderFloat)block.Mtnf!.GetParameter(ShaderFieldType.AlignAcrossDirection)!).Value = 9f;
        var reparsed = new GeomBlock(block.Serialize().Span);

        reparsed.Mtnf!.GetFloat(ShaderFieldType.AlignAcrossDirection).Should().Be(9f);
    }

    [Fact]
    public void Serialize_AssigningNewMtnf_ReplacesBytes()
    {
        var block = new GeomBlock(CreateGeom(1, StructuredMtnf(2.5f)));
        var replacement = new MtnfData();
        replacement.ShaderData.Add(new ShaderFloat(ShaderFieldType.AlignAcrossDirection, -1f));

        block.Mtnf = replacement;

        new GeomBlock(block.Serialize().Span).Mtnf!.GetFloat(ShaderFieldType.AlignAcrossDirection).Should().Be(-1f);
    }

    [Fact]
    public void Parse_UnrecognisedMtnf_KeepsRawBytesByteIdentical()
    {
        byte[] junk = [1, 2, 3, 4, 5, 6, 7, 8];
        var original = CreateGeom(1, junk);

        var block = new GeomBlock(original);

        block.Mtnf?.IsStructuredParsed.Should().NotBe(true);
        block.MtnfData.Should().Equal(junk);
        block.Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_MtnfWithTagButNoStructure_RoundTripsRaw()
    {
        // "MTNF" tag, unknown1, length 9 then 5 raw bytes that do not form shader entries
        byte[] raw = [(byte)'M', (byte)'T', (byte)'N', (byte)'F', 0x78, 0x56, 0x34, 0x12, 5, 0, 0, 0, 0, 0xAA, 0xBB, 0xCC, 0xDD];
        var original = CreateGeom(2, raw);

        new GeomBlock(original).Serialize().ToArray().Should().Equal(original);
    }

    [Fact]
    public void Parse_NoShader_HasNoMtnf()
    {
        var block = new GeomBlock(CreateGeom(0, null));

        block.Mtnf.Should().BeNull();
        block.MtnfData.Should().BeNull();
    }

    [Fact]
    public void Parse_MtnfSizeBeyondData_Throws()
    {
        var data = CreateGeom(1, [1, 2, 3, 4], declaredMtnfSize: 0x7FFFFFFF);

        var action = () => new GeomBlock(data);

        action.Should().Throw<InvalidDataException>().WithMessage("*MTNF chunk size invalid*");
    }
}
