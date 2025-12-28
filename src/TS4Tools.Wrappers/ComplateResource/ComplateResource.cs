using System.Buffers.Binary;
using System.Text;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Complate resource containing a Unicode text string.
/// Resource Type: 0x044AE110
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ComplateResource/ComplateResource.cs
/// </summary>
[ResourceHandler(0x044AE110)]
public sealed class ComplateResource : TypedResource
{
    /// <summary>
    /// Default value for Unknown1 field.
    /// </summary>
    public const uint DefaultUnknown1 = 0x00000002;

    /// <summary>
    /// Default value for Unknown2 field.
    /// </summary>
    public const uint DefaultUnknown2 = 0x00000000;

    private string _text = string.Empty;

    /// <summary>
    /// Unknown field at the start of the resource.
    /// </summary>
    public uint Unknown1 { get; set; } = DefaultUnknown1;

    /// <summary>
    /// The Unicode text content.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Unknown field at the end of the resource.
    /// </summary>
    public uint Unknown2 { get; set; } = DefaultUnknown2;

    /// <summary>
    /// Creates a new ComplateResource by parsing data.
    /// </summary>
    public ComplateResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Minimum size: 4 (unknown1) + 4 (charCount) + 4 (unknown2) = 12 bytes
        if (data.Length < 12)
            throw new ResourceFormatException("Complate data too short for header.");

        int offset = 0;

        // Read unknown1
        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read character count (number of UTF-16 characters)
        int charCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (charCount < 0)
            throw new ResourceFormatException($"Invalid Complate character count: {charCount}");

        // Validate we have enough data for the string and unknown2
        int bytesForString = charCount * 2;
        int requiredLength = 8 + bytesForString + 4;
        if (data.Length < requiredLength)
            throw new ResourceFormatException($"Complate data too short. Expected {requiredLength} bytes, got {data.Length}.");

        // Read the UTF-16 LE string
        if (charCount > 0)
        {
            var chars = new char[charCount];
            for (int i = 0; i < charCount; i++)
            {
                chars[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + i * 2)..]);
            }
            _text = new string(chars);
            offset += bytesForString;
        }
        else
        {
            _text = string.Empty;
        }

        // Read unknown2
        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Size: 4 (unknown1) + 4 (charCount) + charCount*2 (text) + 4 (unknown2)
        int totalSize = 12 + (_text.Length * 2);
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write unknown1
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown1);
        offset += 4;

        // Write character count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _text.Length);
        offset += 4;

        // Write the UTF-16 LE string
        foreach (char c in _text)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), c);
            offset += 2;
        }

        // Write unknown2
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unknown2);

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Unknown1 = DefaultUnknown1;
        _text = string.Empty;
        Unknown2 = DefaultUnknown2;
    }
}
