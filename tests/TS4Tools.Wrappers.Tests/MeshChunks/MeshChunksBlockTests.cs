using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MeshChunks block types (VRTF, IBUF, VBUF, SKIN).
/// </summary>
public class MeshChunksBlockTests
{
    #region VRTF Block Tests

    /// <summary>
    /// Creates a minimal valid VRTF block with specified layouts.
    /// </summary>
    private static byte[] CreateVrtfBlock(
        uint version = 2,
        int stride = 12,
        bool extended = false,
        params (ElementUsage Usage, byte UsageIndex, ElementFormat Format, byte Offset)[] layouts)
    {
        // Tag(4) + Version(4) + Stride(4) + LayoutCount(4) + Extended(4) + Layouts(4 each)
        int size = 20 + (layouts.Length * ElementLayout.Size);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag "VRTF"
        buffer[pos++] = (byte)'V';
        buffer[pos++] = (byte)'R';
        buffer[pos++] = (byte)'T';
        buffer[pos++] = (byte)'F';

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), version);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), stride);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), layouts.Length);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), extended ? 1u : 0u);
        pos += 4;

        // Write layouts
        foreach (var (usage, usageIndex, format, offset) in layouts)
        {
            buffer[pos++] = (byte)usage;
            buffer[pos++] = usageIndex;
            buffer[pos++] = (byte)format;
            buffer[pos++] = offset;
        }

        return buffer;
    }

    [Fact]
    public void VrtfBlock_Parse_MinimalBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVrtfBlock(
            version: 2,
            stride: 12,
            extended: false,
            (ElementUsage.Position, 0, ElementFormat.Float3, 0));

        // Act
        var block = new VrtfBlock(data);

        // Assert
        block.Tag.Should().Be("VRTF");
        block.Version.Should().Be(2);
        block.Stride.Should().Be(12);
        block.ExtendedFormat.Should().BeFalse();
        block.Layouts.Should().HaveCount(1);
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.Float3);
        block.IsKnownType.Should().BeTrue();
    }

    [Fact]
    public void VrtfBlock_Parse_MultipleLayouts_ParsesAll()
    {
        // Arrange
        var data = CreateVrtfBlock(
            version: 2,
            stride: 28,
            extended: true,
            (ElementUsage.Position, 0, ElementFormat.Float3, 0),
            (ElementUsage.Normal, 0, ElementFormat.Float3, 12),
            (ElementUsage.UV, 0, ElementFormat.Float2, 24));

        // Act
        var block = new VrtfBlock(data);

        // Assert
        block.Layouts.Should().HaveCount(3);
        block.ExtendedFormat.Should().BeTrue();
        block.Layouts[1].Usage.Should().Be(ElementUsage.Normal);
        block.Layouts[1].Offset.Should().Be(12);
        block.Layouts[2].Usage.Should().Be(ElementUsage.UV);
    }

    [Fact]
    public void VrtfBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateVrtfBlock(
            version: 2,
            stride: 20,
            extended: false,
            (ElementUsage.Position, 0, ElementFormat.Float3, 0),
            (ElementUsage.UV, 0, ElementFormat.Float2, 12));

        var block = new VrtfBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().Equal(data);
    }

    [Fact]
    public void VrtfBlock_CreateDefaultForSunShadow_HasCorrectLayout()
    {
        // Act
        var block = VrtfBlock.CreateDefaultForSunShadow();

        // Assert
        block.Layouts.Should().HaveCount(1);
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.Short4);
        block.Stride.Should().Be(8); // 4 shorts = 8 bytes
    }

    [Fact]
    public void VrtfBlock_CreateDefaultForDropShadow_HasCorrectLayout()
    {
        // Act
        var block = VrtfBlock.CreateDefaultForDropShadow();

        // Assert
        block.Layouts.Should().HaveCount(2);
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.UShort4N);
        block.Layouts[1].Usage.Should().Be(ElementUsage.UV);
    }

    [Fact]
    public void VrtfBlock_FindLayout_FindsCorrectLayout()
    {
        // Arrange
        var data = CreateVrtfBlock(
            version: 2,
            stride: 20,
            extended: false,
            (ElementUsage.Position, 0, ElementFormat.Float3, 0),
            (ElementUsage.Normal, 0, ElementFormat.Float3, 12),
            (ElementUsage.UV, 0, ElementFormat.Float2, 24));

        var block = new VrtfBlock(data);

        // Act
        var normalLayout = block.FindLayout(ElementUsage.Normal);

        // Assert
        normalLayout.Should().NotBeNull();
        normalLayout!.Value.Offset.Should().Be(12);
    }

    [Fact]
    public void VrtfBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = "XXXX\x02\x00\x00\x00\x0C\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00"u8.ToArray();

        // Act & Assert
        var action = () => new VrtfBlock(data);
        action.Should().Throw<InvalidDataException>();
    }

    #endregion

    #region IBUF Block Tests

    /// <summary>
    /// Creates a minimal valid IBUF block.
    /// </summary>
    private static byte[] CreateIbufBlock(
        uint version = 0,
        IbufFormat flags = IbufFormat.None,
        uint displayListUsage = 0,
        int[] indices = null!)
    {
        indices ??= [0, 1, 2];
        bool is32Bit = (flags & IbufFormat.Uses32BitIndices) != 0;
        int indexSize = is32Bit ? 4 : 2;
        int size = 16 + (indices.Length * indexSize);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag "IBUF"
        buffer[pos++] = (byte)'I';
        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'U';
        buffer[pos++] = (byte)'F';

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), version);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)flags);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), displayListUsage);
        pos += 4;

        // Write indices
        bool isDifferenced = (flags & IbufFormat.DifferencedIndices) != 0;
        int lastValue = 0;

        foreach (int index in indices)
        {
            int toWrite = isDifferenced ? index - lastValue : index;
            lastValue = index;

            if (is32Bit)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), toWrite);
                pos += 4;
            }
            else
            {
                BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(pos), (short)toWrite);
                pos += 2;
            }
        }

        return buffer;
    }

    [Fact]
    public void IbufBlock_Parse_16BitIndices_ParsesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlock(
            version: 0,
            flags: IbufFormat.None,
            indices: [0, 1, 2, 0, 2, 3]);

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Tag.Should().Be("IBUF");
        block.Version.Should().Be(0);
        block.Buffer.Should().Equal([0, 1, 2, 0, 2, 3]);
        block.IsKnownType.Should().BeTrue();
    }

    [Fact]
    public void IbufBlock_Parse_32BitIndices_ParsesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlock(
            version: 0,
            flags: IbufFormat.Uses32BitIndices,
            indices: [100000, 100001, 100002]);

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Buffer.Should().Equal([100000, 100001, 100002]);
    }

    [Fact]
    public void IbufBlock_Parse_DifferencedIndices_DecodesCorrectly()
    {
        // Arrange - with differenced encoding, values are stored as deltas
        var data = CreateIbufBlock(
            version: 0,
            flags: IbufFormat.DifferencedIndices,
            indices: [10, 11, 12, 10, 12, 13]);

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Buffer.Should().Equal([10, 11, 12, 10, 12, 13]);
    }

    [Fact]
    public void IbufBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateIbufBlock(
            version: 1,
            flags: IbufFormat.None,
            indices: [0, 1, 2, 3, 4, 5]);

        var block = new IbufBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().Equal(data);
    }

    [Fact]
    public void IbufBlock_GetIndices_ReturnsCorrectSubset()
    {
        // Arrange
        var data = CreateIbufBlock(
            version: 0,
            flags: IbufFormat.None,
            indices: [0, 1, 2, 3, 4, 5, 6, 7, 8]);

        var block = new IbufBlock(data);

        // Act - Get indices for 2 triangles starting at index 3
        var subset = block.GetIndices(ModelPrimitiveType.TriangleList, 3, 2);

        // Assert
        subset.Should().Equal([3, 4, 5, 6, 7, 8]);
    }

    [Fact]
    public void IbufBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = "XXXX\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00"u8.ToArray();

        // Act & Assert
        var action = () => new IbufBlock(data);
        action.Should().Throw<InvalidDataException>();
    }

    #endregion

    #region ElementLayout Tests

    [Fact]
    public void ElementLayout_ByteSize_ReturnsCorrectSize()
    {
        ElementFormat.Float1.ByteSize().Should().Be(4);
        ElementFormat.Float2.ByteSize().Should().Be(8);
        ElementFormat.Float3.ByteSize().Should().Be(12);
        ElementFormat.Float4.ByteSize().Should().Be(16);
        ElementFormat.Short2.ByteSize().Should().Be(4);
        ElementFormat.Short4.ByteSize().Should().Be(8);
        ElementFormat.UByte4.ByteSize().Should().Be(4);
    }

    [Fact]
    public void ElementLayout_FloatCount_ReturnsCorrectCount()
    {
        ElementFormat.Float1.FloatCount().Should().Be(1);
        ElementFormat.Float2.FloatCount().Should().Be(2);
        ElementFormat.Float3.FloatCount().Should().Be(3);
        ElementFormat.Float4.FloatCount().Should().Be(4);
        // Short4 is typically used for 3D position with padding, so returns 3
        ElementFormat.Short4.FloatCount().Should().Be(3);
    }

    [Fact]
    public void ElementLayout_Equality_WorksCorrectly()
    {
        var layout1 = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Float3, 0);
        var layout2 = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Float3, 0);
        var layout3 = new ElementLayout(ElementUsage.Normal, 0, ElementFormat.Float3, 0);

        (layout1 == layout2).Should().BeTrue();
        (layout1 != layout3).Should().BeTrue();
        layout1.Equals(layout2).Should().BeTrue();
    }

    #endregion

    #region VBUF Block Tests

    /// <summary>
    /// Creates a minimal valid VBUF block.
    /// </summary>
    private static byte[] CreateVbufBlock(
        uint version = 0x00000101,
        VbufFormat flags = VbufFormat.None,
        uint swizzleInfoIndex = 0,
        byte[]? vertexData = null)
    {
        vertexData ??= new byte[24]; // 2 vertices x 12 bytes each
        int size = 16 + vertexData.Length;
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag "VBUF"
        buffer[pos++] = (byte)'V';
        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'U';
        buffer[pos++] = (byte)'F';

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), version);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)flags);
        pos += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), swizzleInfoIndex);
        pos += 4;

        vertexData.CopyTo(buffer.AsSpan(pos));

        return buffer;
    }

    [Fact]
    public void VbufBlock_Parse_BasicBlock_ParsesCorrectly()
    {
        // Arrange
        var vertexData = new byte[24];
        var data = CreateVbufBlock(
            version: 0x00000101,
            flags: VbufFormat.None,
            swizzleInfoIndex: 0,
            vertexData: vertexData);

        // Act
        var block = new VbufBlock(data);

        // Assert
        block.Tag.Should().Be("VBUF");
        block.Version.Should().Be(0x00000101);
        block.Flags.Should().Be(VbufFormat.None);
        block.SwizzleInfoIndex.Should().Be(0);
        block.Buffer.Should().HaveCount(24);
        block.IsKnownType.Should().BeTrue();
    }

    [Fact]
    public void VbufBlock_GetVertexCount_CalculatesCorrectly()
    {
        // Arrange - 24 bytes of vertex data with stride 12 = 2 vertices
        var data = CreateVbufBlock(vertexData: new byte[24]);
        var block = new VbufBlock(data);

        // Act & Assert
        block.GetVertexCount(12).Should().Be(2);
        block.GetVertexCount(8).Should().Be(3);
        block.GetVertexCount(24).Should().Be(1);
    }

    [Fact]
    public void VbufBlock_ReadVector3_ReadsCorrectly()
    {
        // Arrange - Create vertex data with known float values
        var vertexData = new byte[12];
        BinaryPrimitives.WriteSingleLittleEndian(vertexData.AsSpan(0), 1.0f);
        BinaryPrimitives.WriteSingleLittleEndian(vertexData.AsSpan(4), 2.0f);
        BinaryPrimitives.WriteSingleLittleEndian(vertexData.AsSpan(8), 3.0f);

        var data = CreateVbufBlock(vertexData: vertexData);
        var block = new VbufBlock(data);

        // Act
        var position = block.ReadVector3(0, 12, 0);

        // Assert
        position.X.Should().Be(1.0f);
        position.Y.Should().Be(2.0f);
        position.Z.Should().Be(3.0f);
    }

    [Fact]
    public void VbufBlock_Serialize_RoundTrips()
    {
        // Arrange
        var vertexData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var data = CreateVbufBlock(
            version: 0x00000101,
            flags: VbufFormat.Collapsed,
            swizzleInfoIndex: 5,
            vertexData: vertexData);

        var block = new VbufBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().Equal(data);
    }

    [Fact]
    public void VbufBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = "XXXX\x01\x01\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00"u8.ToArray();

        // Act & Assert
        var action = () => new VbufBlock(data);
        action.Should().Throw<InvalidDataException>();
    }

    #endregion

    #region SKIN Block Tests

    /// <summary>
    /// Creates a minimal valid SKIN block.
    /// </summary>
    private static byte[] CreateSkinBlock(uint version = 1, params (uint NameHash, Matrix43 Matrix)[] bones)
    {
        // Tag(4) + Version(4) + BoneCount(4) + Hashes(count*4) + Matrices(count*48)
        int size = 12 + (bones.Length * 4) + (bones.Length * Matrix43.Size);
        byte[] buffer = new byte[size];
        int pos = 0;

        // Write tag "SKIN"
        buffer[pos++] = (byte)'S';
        buffer[pos++] = (byte)'K';
        buffer[pos++] = (byte)'I';
        buffer[pos++] = (byte)'N';

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), version);
        pos += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), bones.Length);
        pos += 4;

        // Write bone hashes
        foreach (var (hash, _) in bones)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), hash);
            pos += 4;
        }

        // Write matrices
        foreach (var (_, matrix) in bones)
        {
            matrix.Write(buffer, ref pos);
        }

        return buffer;
    }

    [Fact]
    public void SkinBlock_Parse_BasicBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateSkinBlock(1,
            (0x12345678, Matrix43.Identity),
            (0xABCDEF00, Matrix43.Identity));

        // Act
        var block = new SkinBlock(data);

        // Assert
        block.Tag.Should().Be("SKIN");
        block.Version.Should().Be(1);
        block.Bones.Should().HaveCount(2);
        block.Bones[0].NameHash.Should().Be(0x12345678);
        block.Bones[1].NameHash.Should().Be(0xABCDEF00);
        block.IsKnownType.Should().BeTrue();
    }

    [Fact]
    public void SkinBlock_Parse_EmptyBoneList_ParsesCorrectly()
    {
        // Arrange
        var data = CreateSkinBlock(1);

        // Act
        var block = new SkinBlock(data);

        // Assert
        block.Bones.Should().BeEmpty();
    }

    [Fact]
    public void SkinBlock_FindBone_FindsCorrectBone()
    {
        // Arrange
        var data = CreateSkinBlock(1,
            (0x11111111, Matrix43.Identity),
            (0x22222222, Matrix43.Identity),
            (0x33333333, Matrix43.Identity));

        var block = new SkinBlock(data);

        // Act
        var bone = block.FindBone(0x22222222);

        // Assert
        bone.Should().NotBeNull();
        bone!.NameHash.Should().Be(0x22222222);
    }

    [Fact]
    public void SkinBlock_FindBone_ReturnsNullForMissing()
    {
        // Arrange
        var data = CreateSkinBlock(1, (0x12345678, Matrix43.Identity));
        var block = new SkinBlock(data);

        // Act
        var bone = block.FindBone(0xFFFFFFFF);

        // Assert
        bone.Should().BeNull();
    }

    [Fact]
    public void SkinBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateSkinBlock(1,
            (0x12345678, Matrix43.Identity),
            (0xABCDEF00, Matrix43.Identity));

        var block = new SkinBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().Equal(data);
    }

    [Fact]
    public void SkinBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = "XXXX\x01\x00\x00\x00\x00\x00\x00\x00"u8.ToArray();

        // Act & Assert
        var action = () => new SkinBlock(data);
        action.Should().Throw<InvalidDataException>();
    }

    #endregion

    #region MeshVector Tests

    [Fact]
    public void MeshVector2_Read_ReadsCorrectly()
    {
        // Arrange
        var data = new byte[8];
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(0), 1.5f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4), 2.5f);
        int pos = 0;

        // Act
        var vector = MeshVector2.Read(data, ref pos);

        // Assert
        vector.X.Should().Be(1.5f);
        vector.Y.Should().Be(2.5f);
        pos.Should().Be(8);
    }

    [Fact]
    public void MeshVector3_Read_ReadsCorrectly()
    {
        // Arrange
        var data = new byte[12];
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(0), 1.0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4), 2.0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(8), 3.0f);
        int pos = 0;

        // Act
        var vector = MeshVector3.Read(data, ref pos);

        // Assert
        vector.X.Should().Be(1.0f);
        vector.Y.Should().Be(2.0f);
        vector.Z.Should().Be(3.0f);
    }

    [Fact]
    public void MeshVector3_Equality_WorksCorrectly()
    {
        var v1 = new MeshVector3(1, 2, 3);
        var v2 = new MeshVector3(1, 2, 3);
        var v3 = new MeshVector3(4, 5, 6);

        (v1 == v2).Should().BeTrue();
        (v1 != v3).Should().BeTrue();
    }

    [Fact]
    public void MeshVector4_Read_ReadsCorrectly()
    {
        // Arrange
        var data = new byte[16];
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(0), 1.0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4), 2.0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(8), 3.0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(12), 4.0f);
        int pos = 0;

        // Act
        var vector = MeshVector4.Read(data, ref pos);

        // Assert
        vector.X.Should().Be(1.0f);
        vector.Y.Should().Be(2.0f);
        vector.Z.Should().Be(3.0f);
        vector.W.Should().Be(4.0f);
    }

    #endregion

    #region Matrix43 Tests

    [Fact]
    public void Matrix43_Identity_HasCorrectValues()
    {
        // Act
        var identity = Matrix43.Identity;

        // Assert - Identity matrix: unit vectors for Right/Up/Back, zero for Translate
        identity.Right.Should().Be(MeshVector3.UnitX);
        identity.Up.Should().Be(MeshVector3.UnitY);
        identity.Back.Should().Be(MeshVector3.UnitZ);
        identity.Translate.Should().Be(MeshVector3.Zero);
    }

    [Fact]
    public void Matrix43_Serialize_RoundTrips()
    {
        // Arrange
        var identity = Matrix43.Identity;
        var buffer = new byte[Matrix43.Size];
        int writePos = 0;
        identity.Write(buffer, ref writePos);

        // Act
        int readPos = 0;
        var read = Matrix43.Read(buffer, ref readPos);

        // Assert
        read.Should().Be(identity);
    }

    [Fact]
    public void Matrix43_Size_Is48Bytes()
    {
        // Matrix43 is 4x3 = 12 floats x 4 bytes = 48 bytes
        Matrix43.Size.Should().Be(48);
    }

    #endregion

    #region UByte4 Tests

    [Fact]
    public void UByte4_Read_ReadsCorrectly()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4 };
        int pos = 0;

        // Act
        var ub = UByte4.Read(data, ref pos);

        // Assert
        ub.A.Should().Be(1);
        ub.B.Should().Be(2);
        ub.C.Should().Be(3);
        ub.D.Should().Be(4);
    }

    [Fact]
    public void UByte4_Equality_WorksCorrectly()
    {
        var ub1 = new UByte4(1, 2, 3, 4);
        var ub2 = new UByte4(1, 2, 3, 4);
        var ub3 = new UByte4(5, 6, 7, 8);

        (ub1 == ub2).Should().BeTrue();
        (ub1 != ub3).Should().BeTrue();
    }

    #endregion
}
