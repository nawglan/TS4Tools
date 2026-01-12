using TS4Tools.Package;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// TMLT (Timeline Template) resource for world timeline/lighting parameters.
/// Resource Type: 0xB0118C15
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TMLTResource.cs
///
/// Format (99 bytes):
/// - version: uint32
/// - uint01: uint32
/// - uintlong02: uint64
/// - uint03: uint32
/// - selfReference: TGIBlock (ITG order: Instance, Type, Group - 16 bytes)
/// - uint04: uint32
/// - uint05: uint32
/// - magic: "TMLT" (4 bytes)
/// - float01: float
/// - float02: float
/// - float03: float
/// - colorRed: float
/// - colorGreen: float
/// - colorBlue: float
/// - float07: float
/// - float08: float
/// - float09: float
/// - float10: float
/// - float11: float
/// - float12: float
/// - byte01: byte
/// - byte02: byte
/// - byte03: byte
/// </summary>
public sealed class TmltResource : TypedResource
{
    private const uint Magic = 0x544C4D54; // "TMLT" in little-endian
    private const int ExpectedSize = 4 + 4 + 8 + 4 + 16 + 4 + 4 + 4 + (12 * 4) + 3; // 99 bytes

    private uint _version;
    private uint _uint01;
    private ulong _uintlong02;
    private uint _uint03;
    private ResourceKey _selfReference;
    private uint _uint04;
    private uint _uint05;
    private readonly float[] _floats = new float[12];
    private byte _byte01;
    private byte _byte02;
    private byte _byte03;

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
    /// Color red component (0.0 - 1.0).
    /// </summary>
    public float ColorRed
    {
        get => _floats[3];
        set { if (_floats[3] != value) { _floats[3] = value; OnChanged(); } }
    }

    /// <summary>
    /// Color green component (0.0 - 1.0).
    /// </summary>
    public float ColorGreen
    {
        get => _floats[4];
        set { if (_floats[4] != value) { _floats[4] = value; OnChanged(); } }
    }

    /// <summary>
    /// Color blue component (0.0 - 1.0).
    /// </summary>
    public float ColorBlue
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
    /// Float value 9.
    /// </summary>
    public float Float09
    {
        get => _floats[8];
        set { if (_floats[8] != value) { _floats[8] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 10.
    /// </summary>
    public float Float10
    {
        get => _floats[9];
        set { if (_floats[9] != value) { _floats[9] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 11.
    /// </summary>
    public float Float11
    {
        get => _floats[10];
        set { if (_floats[10] != value) { _floats[10] = value; OnChanged(); } }
    }

    /// <summary>
    /// Float value 12.
    /// </summary>
    public float Float12
    {
        get => _floats[11];
        set { if (_floats[11] != value) { _floats[11] = value; OnChanged(); } }
    }

    /// <summary>
    /// Byte value 1.
    /// </summary>
    public byte Byte01
    {
        get => _byte01;
        set { if (_byte01 != value) { _byte01 = value; OnChanged(); } }
    }

    /// <summary>
    /// Byte value 2.
    /// </summary>
    public byte Byte02
    {
        get => _byte02;
        set { if (_byte02 != value) { _byte02 = value; OnChanged(); } }
    }

    /// <summary>
    /// Byte value 3.
    /// </summary>
    public byte Byte03
    {
        get => _byte03;
        set { if (_byte03 != value) { _byte03 = value; OnChanged(); } }
    }

    /// <summary>
    /// Creates a new TMLT resource by parsing data.
    /// </summary>
    public TmltResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < ExpectedSize)
        {
            throw new InvalidDataException($"TMLT data too short: {data.Length} bytes, expected {ExpectedSize}");
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
        // Source: TMLTResource.cs lines 89-90
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
        // Source: TMLTResource.cs lines 93-101
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (magic != Magic)
        {
            throw new InvalidDataException($"Expected TMLT magic 0x{Magic:X8}, got 0x{magic:X8}");
        }
        offset += 4;

        // Read 12 floats
        // Source: TMLTResource.cs lines 102-113
        for (int i = 0; i < 12; i++)
        {
            _floats[i] = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
        }

        // Read 3 bytes
        // Source: TMLTResource.cs lines 114-116
        _byte01 = data[offset++];
        _byte02 = data[offset++];
        _byte03 = data[offset];
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

        // Write 12 floats
        for (int i = 0; i < 12; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), _floats[i]);
            offset += 4;
        }

        // Write 3 bytes
        buffer[offset++] = _byte01;
        buffer[offset++] = _byte02;
        buffer[offset] = _byte03;

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
        _byte01 = 0;
        _byte02 = 0;
        _byte03 = 0;
    }
}
