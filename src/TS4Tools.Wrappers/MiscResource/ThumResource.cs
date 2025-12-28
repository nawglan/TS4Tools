using TS4Tools.Package;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// THUM (Theme/World Color) resource for world rendering parameters.
/// Resource Type: 0x16CA6BC4
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs
///
/// Format:
/// - version: uint32
/// - uint01: uint32
/// - uintlong02: uint64
/// - uint03: uint32
/// - selfReference: TGIBlock (ITG order: Instance, Type, Group - 16 bytes)
/// - uint04: uint32
/// - uint05: uint32
/// - magic: "THUM" (4 bytes)
/// - floats: 8 float values
/// </summary>
[ResourceHandler(0x16CA6BC4)]
public sealed class ThumWorldResource : TypedResource
{
    private const uint Magic = 0x4D554854; // "THUM" in little-endian
    private const int ExpectedSize = 4 + 4 + 8 + 4 + 16 + 4 + 4 + 4 + (8 * 4); // 80 bytes

    private uint _version;
    private uint _uint01;
    private ulong _uintlong02;
    private uint _uint03;
    private ResourceKey _selfReference;
    private uint _uint04;
    private uint _uint05;
    private readonly float[] _floats = new float[8];

    /// <summary>
    /// The format version.
    /// </summary>
    public uint Version
    {
        get => _version;
        set { if (_version != value) { _version = value; OnChanged(); } }
    }

    /// <summary>
    /// Unknown uint field 1.
    /// </summary>
    public uint Uint01
    {
        get => _uint01;
        set { if (_uint01 != value) { _uint01 = value; OnChanged(); } }
    }

    /// <summary>
    /// Unknown uint64 field 2.
    /// </summary>
    public ulong Uintlong02
    {
        get => _uintlong02;
        set { if (_uintlong02 != value) { _uintlong02 = value; OnChanged(); } }
    }

    /// <summary>
    /// Unknown uint field 3.
    /// </summary>
    public uint Uint03
    {
        get => _uint03;
        set { if (_uint03 != value) { _uint03 = value; OnChanged(); } }
    }

    /// <summary>
    /// Self-reference TGI block.
    /// </summary>
    public ResourceKey SelfReference
    {
        get => _selfReference;
        set { if (_selfReference != value) { _selfReference = value; OnChanged(); } }
    }

    /// <summary>
    /// Unknown uint field 4.
    /// </summary>
    public uint Uint04
    {
        get => _uint04;
        set { if (_uint04 != value) { _uint04 = value; OnChanged(); } }
    }

    /// <summary>
    /// Unknown uint field 5.
    /// </summary>
    public uint Uint05
    {
        get => _uint05;
        set { if (_uint05 != value) { _uint05 = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 1.
    /// </summary>
    public float Float01
    {
        get => _floats[0];
        set { if (_floats[0] != value) { _floats[0] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 2.
    /// </summary>
    public float Float02
    {
        get => _floats[1];
        set { if (_floats[1] != value) { _floats[1] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 3.
    /// </summary>
    public float Float03
    {
        get => _floats[2];
        set { if (_floats[2] != value) { _floats[2] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 4.
    /// </summary>
    public float Float04
    {
        get => _floats[3];
        set { if (_floats[3] != value) { _floats[3] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 5.
    /// </summary>
    public float Float05
    {
        get => _floats[4];
        set { if (_floats[4] != value) { _floats[4] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 6.
    /// </summary>
    public float Float06
    {
        get => _floats[5];
        set { if (_floats[5] != value) { _floats[5] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 7.
    /// </summary>
    public float Float07
    {
        get => _floats[6];
        set { if (_floats[6] != value) { _floats[6] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 8.
    /// </summary>
    public float Float08
    {
        get => _floats[7];
        set { if (_floats[7] != value) { _floats[7] = value; OnChanged(); } }
    }

    /// <summary>
    /// Creates a new THUM resource by parsing data.
    /// </summary>
    public ThumWorldResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < ExpectedSize)
        {
            throw new InvalidDataException($"THUM data too short: {data.Length} bytes, expected {ExpectedSize}");
        }

        int offset = 0;

        // Read header fields
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _uint01 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _uintlong02 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        _uint03 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read TGI block in ITG order (Instance, Type, Group)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        _selfReference = new ResourceKey(type, group, instance);

        _uint04 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _uint05 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read and validate magic
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (magic != Magic)
        {
            throw new InvalidDataException($"Expected THUM magic 0x{Magic:X8}, got 0x{magic:X8}");
        }
        offset += 4;

        // Read 8 floats
        for (int i = 0; i < 8; i++)
        {
            _floats[i] = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        var buffer = new byte[ExpectedSize];
        int offset = 0;

        // Write header fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _version);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _uint01);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), _uintlong02);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _uint03);
        offset += 4;

        // Write TGI block in ITG order
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), _selfReference.Instance);
        offset += 8;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _selfReference.ResourceType);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _selfReference.ResourceGroup);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _uint04);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _uint05);
        offset += 4;

        // Write magic
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Magic);
        offset += 4;

        // Write 8 floats
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), _floats[i]);
            offset += 4;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _version = 4;
        _uint01 = 0;
        _uintlong02 = 0;
        _uint03 = 0;
        _selfReference = default;
        _uint04 = 0;
        _uint05 = 0;
        Array.Clear(_floats);
    }
}
