// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/ShaderData.cs

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Base class for shader data elements.
/// Source: ShaderData.cs lines 243-376
/// </summary>
public abstract class ShaderDataElement
{
    /// <summary>The field type identifier.</summary>
    public ShaderFieldType Field { get; set; }

    /// <summary>Gets the data type for this element.</summary>
    public abstract ShaderDataType DataType { get; }

    /// <summary>Gets the count value for this element.</summary>
    public abstract int Count { get; }

    /// <summary>Writes the element data to a binary writer.</summary>
    public abstract void WriteData(BinaryWriter writer);

    /// <summary>Creates a shader data element from a stream.</summary>
    public static ShaderDataElement Parse(ReadOnlySpan<byte> data, ref int offset, long startOffset)
    {
        // Read header
        var field = (ShaderFieldType)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        var dataType = (ShaderDataType)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        uint dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read data at the specified offset
        int dataPos = (int)(startOffset + dataOffset);

        return dataType switch
        {
            ShaderDataType.Float => count switch
            {
                1 => new ShaderFloat(field, data, dataPos),
                2 => new ShaderFloat2(field, data, dataPos),
                3 => new ShaderFloat3(field, data, dataPos),
                4 => new ShaderFloat4(field, data, dataPos),
                _ => throw new InvalidDataException($"Invalid count {count} for Float type")
            },
            ShaderDataType.Int => count switch
            {
                1 => new ShaderInt(field, data, dataPos),
                _ => throw new InvalidDataException($"Invalid count {count} for Int type")
            },
            ShaderDataType.Texture => count switch
            {
                4 => new ShaderTextureRef(field, data, dataPos),
                5 => new ShaderTextureKey(field, data, dataPos),
                _ => throw new InvalidDataException($"Invalid count {count} for Texture type")
            },
            ShaderDataType.ImageMap => count switch
            {
                4 => new ShaderImageMapKey(field, data, dataPos),
                _ => throw new InvalidDataException($"Invalid count {count} for ImageMap type")
            },
            _ => throw new InvalidDataException($"Unknown shader data type: 0x{(uint)dataType:X8}")
        };
    }

    /// <summary>Writes the element header to a binary writer.</summary>
    public void WriteHeader(BinaryWriter writer, uint dataOffset)
    {
        writer.Write((uint)Field);
        writer.Write((uint)DataType);
        writer.Write(Count);
        writer.Write(dataOffset);
    }
}

/// <summary>Single float shader data.</summary>
public sealed class ShaderFloat : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Float;
    /// <inheritdoc/>
    public override int Count => 1;

    /// <summary>The float value.</summary>
    public float Value { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="value">The float value.</param>
    public ShaderFloat(ShaderFieldType field, float value = 0f)
    {
        Field = field;
        Value = value;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderFloat(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        Value = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer) => writer.Write(Value);
}

/// <summary>Float2 (vec2) shader data.</summary>
public sealed class ShaderFloat2 : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Float;
    /// <inheritdoc/>
    public override int Count => 2;

    /// <summary>The X component.</summary>
    public float X { get; set; }
    /// <summary>The Y component.</summary>
    public float Y { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat2"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    public ShaderFloat2(ShaderFieldType field, float x = 0f, float y = 0f)
    {
        Field = field;
        X = x;
        Y = y;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat2"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderFloat2(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        X = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        Y = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }
}

/// <summary>Float3 (vec3) shader data.</summary>
public sealed class ShaderFloat3 : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Float;
    /// <inheritdoc/>
    public override int Count => 3;

    /// <summary>The X component.</summary>
    public float X { get; set; }
    /// <summary>The Y component.</summary>
    public float Y { get; set; }
    /// <summary>The Z component.</summary>
    public float Z { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat3"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public ShaderFloat3(ShaderFieldType field, float x = 0f, float y = 0f, float z = 0f)
    {
        Field = field;
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat3"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderFloat3(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        X = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        Y = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]);
        Z = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }
}

/// <summary>Float4 (vec4) shader data.</summary>
public sealed class ShaderFloat4 : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Float;
    /// <inheritdoc/>
    public override int Count => 4;

    /// <summary>The X component.</summary>
    public float X { get; set; }
    /// <summary>The Y component.</summary>
    public float Y { get; set; }
    /// <summary>The Z component.</summary>
    public float Z { get; set; }
    /// <summary>The W component.</summary>
    public float W { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat4"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    public ShaderFloat4(ShaderFieldType field, float x = 0f, float y = 0f, float z = 0f, float w = 0f)
    {
        Field = field;
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderFloat4"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderFloat4(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        X = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        Y = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]);
        Z = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]);
        W = BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 12)..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(W);
    }
}

/// <summary>Integer shader data.</summary>
public sealed class ShaderInt : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Int;
    /// <inheritdoc/>
    public override int Count => 1;

    /// <summary>The integer value.</summary>
    public int Value { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderInt"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="value">The integer value.</param>
    public ShaderInt(ShaderFieldType field, int value = 0)
    {
        Field = field;
        Value = value;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderInt"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderInt(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        Value = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer) => writer.Write(Value);
}

/// <summary>Texture reference shader data (TGI key in ITG order).</summary>
public sealed class ShaderTextureRef : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Texture;
    /// <inheritdoc/>
    public override int Count => 4;

    /// <summary>Texture resource key in ITG order.</summary>
    public ResourceKey TextureKey { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderTextureRef"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="key">The texture resource key.</param>
    public ShaderTextureRef(ShaderFieldType field, ResourceKey key = default)
    {
        Field = field;
        TextureKey = key;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderTextureRef"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderTextureRef(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        // ITG order: Instance (8), Type (4), Group (4)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 8)..]);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 12)..]);
        TextureKey = new ResourceKey(type, group, instance);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        // ITG order
        writer.Write(TextureKey.Instance);
        writer.Write(TextureKey.ResourceType);
        writer.Write(TextureKey.ResourceGroup);
    }
}

/// <summary>Texture key shader data (with additional format info).</summary>
public sealed class ShaderTextureKey : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.Texture;
    /// <inheritdoc/>
    public override int Count => 5;

    /// <summary>Texture resource key in ITG order.</summary>
    public ResourceKey TextureKey { get; set; }

    /// <summary>Additional format value.</summary>
    public uint Format { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderTextureKey"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="key">The texture resource key.</param>
    /// <param name="format">The format value.</param>
    public ShaderTextureKey(ShaderFieldType field, ResourceKey key = default, uint format = 0)
    {
        Field = field;
        TextureKey = key;
        Format = format;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderTextureKey"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderTextureKey(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        // ITG order: Instance (8), Type (4), Group (4), Format (4)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 8)..]);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 12)..]);
        TextureKey = new ResourceKey(type, group, instance);
        Format = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 16)..]);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        // ITG order
        writer.Write(TextureKey.Instance);
        writer.Write(TextureKey.ResourceType);
        writer.Write(TextureKey.ResourceGroup);
        writer.Write(Format);
    }
}

/// <summary>Image map key shader data.</summary>
public sealed class ShaderImageMapKey : ShaderDataElement
{
    /// <inheritdoc/>
    public override ShaderDataType DataType => ShaderDataType.ImageMap;
    /// <inheritdoc/>
    public override int Count => 4;

    /// <summary>Image map resource key in ITG order.</summary>
    public ResourceKey ImageMapKey { get; set; }

    /// <summary>Initializes a new instance of the <see cref="ShaderImageMapKey"/> class.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="key">The image map resource key.</param>
    public ShaderImageMapKey(ShaderFieldType field, ResourceKey key = default)
    {
        Field = field;
        ImageMapKey = key;
    }

    /// <summary>Initializes a new instance of the <see cref="ShaderImageMapKey"/> class from binary data.</summary>
    /// <param name="field">The shader field type.</param>
    /// <param name="data">The binary data to read from.</param>
    /// <param name="offset">The offset in the data to start reading.</param>
    internal ShaderImageMapKey(ShaderFieldType field, ReadOnlySpan<byte> data, int offset)
    {
        Field = field;
        // ITG order: Instance (8), Type (4), Group (4)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 8)..]);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 12)..]);
        ImageMapKey = new ResourceKey(type, group, instance);
    }

    /// <inheritdoc/>
    public override void WriteData(BinaryWriter writer)
    {
        // ITG order
        writer.Write(ImageMapKey.Instance);
        writer.Write(ImageMapKey.ResourceType);
        writer.Write(ImageMapKey.ResourceGroup);
    }
}
