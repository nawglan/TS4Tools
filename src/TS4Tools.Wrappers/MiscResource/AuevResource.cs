using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// AUEV (Audio Event) resource for audio/event definitions.
/// Resource Type: 0xBDD82221
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/AUEVResource.cs
///
/// Format:
/// - Magic: "AUEV" (4 bytes)
/// - Version: uint32
/// - GroupCount: int32
/// - Content: string[GroupCount * 3] (each string is length-prefixed + null-terminated)
/// </summary>
public sealed class AuevResource : TypedResource
{
    private const uint Magic = 0x56455541; // "AUEV" in little-endian
    private const int MaxGroupCount = 10000; // Reasonable limit for validation

    private uint _version;
    private string[] _content = [];

    /// <summary>
    /// The format version.
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The number of groups (content.Length / 3).
    /// </summary>
    public int GroupCount => _content.Length / 3;

    /// <summary>
    /// The string content array. Each group consists of 3 strings.
    /// </summary>
    public string[] Content
    {
        get => _content;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length % 3 != 0)
            {
                throw new ArgumentException("Content length must be a multiple of 3", nameof(value));
            }
            _content = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Creates a new AUEV resource by parsing data.
    /// </summary>
    public AuevResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12) // Magic + Version + GroupCount
        {
            throw new InvalidDataException($"AUEV data too short: {data.Length} bytes");
        }

        int offset = 0;

        // Read and validate magic
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (magic != Magic)
        {
            throw new InvalidDataException($"Expected AUEV magic 0x{Magic:X8}, got 0x{magic:X8}");
        }
        offset += 4;

        // Read version
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read group count
        int groupCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate group count
        if (groupCount < 0 || groupCount > MaxGroupCount)
        {
            throw new InvalidDataException($"Invalid group count: {groupCount}");
        }

        // Read strings (3 per group)
        int stringCount = groupCount * 3;
        _content = new string[stringCount];

        for (int i = 0; i < stringCount; i++)
        {
            if (offset + 4 > data.Length)
            {
                throw new InvalidDataException($"Unexpected end of data reading string {i}");
            }

            // Read string length (includes null terminator in legacy format)
            int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (length < 1 || offset + length > data.Length)
            {
                throw new InvalidDataException($"Invalid string length {length} at offset {offset}");
            }

            // Read string bytes (length - 1 to exclude null terminator)
            _content[i] = Encoding.ASCII.GetString(data.Slice(offset, length - 1));
            offset += length - 1;

            // Skip null terminator
            if (data[offset] != 0)
            {
                throw new InvalidDataException($"Expected null terminator at offset {offset}");
            }
            offset += 1;
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate total size
        int size = 12; // Magic + Version + GroupCount
        foreach (var str in _content)
        {
            size += 4; // Length
            size += str.Length; // String bytes
            size += 1; // Null terminator
        }

        var buffer = new byte[size];
        int offset = 0;

        // Write magic
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Magic);
        offset += 4;

        // Write version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _version);
        offset += 4;

        // Write group count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), GroupCount);
        offset += 4;

        // Write strings
        foreach (var str in _content)
        {
            // Write length (including null terminator)
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), str.Length + 1);
            offset += 4;

            // Write string bytes
            Encoding.ASCII.GetBytes(str, buffer.AsSpan(offset));
            offset += str.Length;

            // Write null terminator
            buffer[offset] = 0;
            offset += 1;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _version = 1;
        _content = [];
    }

    /// <summary>
    /// Gets a group by index.
    /// </summary>
    /// <param name="index">The group index (0 to GroupCount-1).</param>
    /// <returns>The three strings for the group.</returns>
    public (string First, string Second, string Third) GetGroup(int index)
    {
        if (index < 0 || index >= GroupCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int baseIndex = index * 3;
        return (_content[baseIndex], _content[baseIndex + 1], _content[baseIndex + 2]);
    }

    /// <summary>
    /// Sets a group by index.
    /// </summary>
    /// <param name="index">The group index (0 to GroupCount-1).</param>
    /// <param name="first">First string.</param>
    /// <param name="second">Second string.</param>
    /// <param name="third">Third string.</param>
    public void SetGroup(int index, string first, string second, string third)
    {
        if (index < 0 || index >= GroupCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int baseIndex = index * 3;
        _content[baseIndex] = first;
        _content[baseIndex + 1] = second;
        _content[baseIndex + 2] = third;
        OnChanged();
    }
}
