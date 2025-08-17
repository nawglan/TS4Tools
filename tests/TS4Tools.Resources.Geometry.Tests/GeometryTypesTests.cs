using TS4Tools.Resources.Geometry;

namespace TS4Tools.Resources.Geometry.Tests;

/// <summary>
/// Unit tests for GeometryTypes enums and record structs.
/// Tests all type definitions, validation methods, and edge cases.
/// </summary>
public class GeometryTypesTests
{
    [Theory]
    [InlineData(0, ShaderType.None)]
    [InlineData(unchecked((int)0x9DD6A9F9), ShaderType.VertexLit)]
    [InlineData(unchecked((int)0x57A47B7C), ShaderType.ShadowMap)]
    [InlineData(unchecked((int)0x8D23AC8E), ShaderType.DropShadow)]
    [InlineData(unchecked((int)0x4ECD93F0), ShaderType.Skin)]
    [InlineData(unchecked((int)0x1A3B0A87), ShaderType.Hair)]
    [InlineData(99, (ShaderType)99)]
    public void ShaderType_FromValue_ReturnsExpectedEnum(int value, ShaderType expected)
    {
        var result = (ShaderType)value;
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, UsageType.None)]
    [InlineData(0x01, UsageType.Position)]
    [InlineData(0x02, UsageType.Normal)]
    [InlineData(0x03, UsageType.UV)]
    [InlineData(0x04, UsageType.BoneAssignment)]
    [InlineData(0x05, UsageType.Weights)]
    [InlineData(0x06, UsageType.Tangent)]
    [InlineData(0x07, UsageType.Color)]
    [InlineData(0x08, UsageType.BlendIndices)]
    [InlineData(0x09, UsageType.BlendWeights)]
    [InlineData(99, (UsageType)99)]
    public void UsageType_FromValue_ReturnsExpectedEnum(int value, UsageType expected)
    {
        var result = (UsageType)value;
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, DataType.None)]
    [InlineData(0x01, DataType.Float)]
    [InlineData(0x02, DataType.Float2)]
    [InlineData(0x03, DataType.Float3)]
    [InlineData(0x04, DataType.Float4)]
    [InlineData(0x05, DataType.UByte)]
    [InlineData(0x06, DataType.UByte4)]
    [InlineData(0x07, DataType.Byte)]
    [InlineData(0x08, DataType.Byte4)]
    [InlineData(0x09, DataType.UShort)]
    [InlineData(0x0A, DataType.UShort4)]
    [InlineData(0x0B, DataType.Short)]
    [InlineData(0x0C, DataType.Short4)]
    [InlineData(99, (DataType)99)]
    public void DataType_FromValue_ReturnsExpectedEnum(int value, DataType expected)
    {
        var result = (DataType)value;
        result.Should().Be(expected);
    }

    [Fact]
    public void VertexFormat_Construction_SetsPropertiesCorrectly()
    {
        var format = new VertexFormat(
            usage: UsageType.Position,
            dataType: DataType.Float,
            subUsage: 3,
            reserved: 0
        );

        format.Usage.Should().Be(UsageType.Position);
        format.DataType.Should().Be(DataType.Float);
        format.SubUsage.Should().Be(3);
        format.Reserved.Should().Be(0);
    }

    [Theory]
    [InlineData(DataType.UByte, 1)]
    [InlineData(DataType.Byte, 1)]
    [InlineData(DataType.UShort, 2)]
    [InlineData(DataType.Short, 2)]
    [InlineData(DataType.Float, 4)]
    [InlineData(DataType.Float2, 8)]
    [InlineData(DataType.Float3, 12)]
    [InlineData(DataType.Float4, 16)]
    public void VertexFormat_GetElementSize_ReturnsCorrectSize(DataType dataType, int expectedSize)
    {
        var format = new VertexFormat(UsageType.Position, dataType, 0, 0);
        format.GetElementSize().Should().Be(expectedSize);
    }

    [Fact]
    public void Face_Construction_WithBasicValues_SetsPropertiesCorrectly()
    {
        var face = new Face(0, 1, 2);

        face.A.Should().Be(0);
        face.B.Should().Be(1);
        face.C.Should().Be(2);
    }

    [Theory]
    [InlineData((ushort)0, (ushort)1, (ushort)2)]
    [InlineData((ushort)100, (ushort)200, (ushort)300)]
    [InlineData((ushort)65535, (ushort)0, (ushort)1)]
    public void Face_Construction_SetsPropertiesCorrectly(ushort a, ushort b, ushort c)
    {
        var face = new Face(a, b, c);

        face.A.Should().Be(a);
        face.B.Should().Be(b);
        face.C.Should().Be(c);
    }

    [Fact]
    public void UVStitch_Construction_SetsPropertiesCorrectly()
    {
        var stitch = new UVStitch(5, 10);

        stitch.VertexA.Should().Be(5);
        stitch.VertexB.Should().Be(10);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(100, 0)]
    [InlineData(200, 300)]
    public void UVStitch_WithVariousValues_SetsPropertiesCorrectly(uint vertexA, uint vertexB)
    {
        var stitch = new UVStitch(vertexA, vertexB);
        stitch.VertexA.Should().Be(vertexA);
        stitch.VertexB.Should().Be(vertexB);
    }

    [Fact]
    public void SeamStitch_Construction_SetsPropertiesCorrectly()
    {
        var stitch = new SeamStitch(7, 14);

        stitch.VertexA.Should().Be(7);
        stitch.VertexB.Should().Be(14);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(50, 0)]
    [InlineData(100, 200)]
    public void SeamStitch_WithVariousValues_SetsPropertiesCorrectly(uint vertexA, uint vertexB)
    {
        var stitch = new SeamStitch(vertexA, vertexB);
        stitch.VertexA.Should().Be(vertexA);
        stitch.VertexB.Should().Be(vertexB);
    }

    [Fact]
    public void VertexFormat_Equality_WorksCorrectly()
    {
        var format1 = new VertexFormat(UsageType.Position, DataType.Float, 3, 0);
        var format2 = new VertexFormat(UsageType.Position, DataType.Float, 3, 0);
        var format3 = new VertexFormat(UsageType.Normal, DataType.Float, 3, 0);

        format1.Should().Be(format2);
        format1.Should().NotBe(format3);
        (format1 == format2).Should().BeTrue();
        (format1 != format3).Should().BeTrue();
    }

    [Fact]
    public void Face_Equality_WorksCorrectly()
    {
        var face1 = new Face(0, 1, 2);
        var face2 = new Face(0, 1, 2);
        var face3 = new Face(1, 2, 3);

        face1.Should().Be(face2);
        face1.Should().NotBe(face3);
        (face1 == face2).Should().BeTrue();
        (face1 != face3).Should().BeTrue();
    }

    [Fact]
    public void UVStitch_Equality_WorksCorrectly()
    {
        var stitch1 = new UVStitch(5, 10);
        var stitch2 = new UVStitch(5, 10);
        var stitch3 = new UVStitch(6, 10);

        stitch1.Should().Be(stitch2);
        stitch1.Should().NotBe(stitch3);
        (stitch1 == stitch2).Should().BeTrue();
        (stitch1 != stitch3).Should().BeTrue();
    }

    [Fact]
    public void SeamStitch_Equality_WorksCorrectly()
    {
        var stitch1 = new SeamStitch(7, 14);
        var stitch2 = new SeamStitch(7, 14);
        var stitch3 = new SeamStitch(8, 14);

        stitch1.Should().Be(stitch2);
        stitch1.Should().NotBe(stitch3);
        (stitch1 == stitch2).Should().BeTrue();
        (stitch1 != stitch3).Should().BeTrue();
    }

    [Fact]
    public void VertexFormat_ToString_ReturnsReadableString()
    {
        var format = new VertexFormat(UsageType.Position, DataType.Float, 3, 0);
        var result = format.ToString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Position");
        result.Should().Contain("Float");
        result.Should().Contain("3");
        result.Should().Contain("0");
    }

    [Fact]
    public void Face_ToString_ReturnsReadableString()
    {
        var face = new Face(10, 20, 30);
        var result = face.ToString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("10");
        result.Should().Contain("20");
        result.Should().Contain("30");
    }
}
