// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/StyleLookResource.cs

using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;

namespace TS4Tools.Wrappers;

/// <summary>
/// Style Look resource containing outfit style definitions.
/// Resource Type: 0x71BDB8A2
/// </summary>
/// <remarks>
/// Source: StyleLookResource.cs lines 33-162
/// Contains references to sim outfits, textures, animations, colors, and flags
/// that define a complete look style in CAS.
/// </remarks>
[ResourceHandler(TypeId)]
public sealed class StyleLookResource : TypedResource
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = 0x71BDB8A2;

    private const int MinimumSize = 87; // Minimum header without variable-length strings
    private const int UnknownBlobSize = 14;

    private byte[] _rawData = [];
    private byte[] _unknownBlob = new byte[UnknownBlobSize];

    /// <summary>Resource version.</summary>
    public uint Version { get; set; }

    /// <summary>Age and gender flags for this style.</summary>
    public AgeGenderFlags AgeGender { get; set; }

    /// <summary>Grouping identifier.</summary>
    public ulong GroupingId { get; set; }

    /// <summary>Unknown byte value.</summary>
    public byte Unknown1 { get; set; }

    /// <summary>Reference to sim outfit resource (instance ID).</summary>
    public ulong SimOutfitReference { get; set; }

    /// <summary>Reference to texture resource (instance ID).</summary>
    public ulong TextureReference { get; set; }

    /// <summary>Reference to sim data resource (instance ID).</summary>
    public ulong SimDataReference { get; set; }

    /// <summary>Name string hash.</summary>
    public uint NameHash { get; set; }

    /// <summary>Description string hash.</summary>
    public uint DescHash { get; set; }

    /// <summary>14-byte unknown data blob.</summary>
    public ReadOnlySpan<byte> UnknownBlob => _unknownBlob;

    /// <summary>Unknown uint32 value.</summary>
    public uint Unknown3 { get; set; }

    /// <summary>First animation reference (instance ID).</summary>
    public ulong AnimationReference1 { get; set; }

    /// <summary>First animation state name.</summary>
    public string AnimationStateName1 { get; set; } = string.Empty;

    /// <summary>Second animation reference (instance ID).</summary>
    public ulong AnimationReference2 { get; set; }

    /// <summary>Second animation state name.</summary>
    public string AnimationStateName2 { get; set; } = string.Empty;

    /// <summary>List of swatch colors.</summary>
    public SwatchColorList ColorList { get; private set; } = [];

    /// <summary>List of CAS part flags.</summary>
    public CaspFlagList FlagList { get; private set; } = [];

    /// <summary>Whether this resource was successfully parsed.</summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Creates a new StyleLookResource by parsing data.
    /// </summary>
    public StyleLookResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        IsValid = false;

        if (data.Length < MinimumSize)
            return;

        try
        {
            int offset = 0;

            // Read header fields
            // Source: StyleLookResource.cs lines 65-75
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            AgeGender = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            GroupingId = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            Unknown1 = data[offset++];

            SimOutfitReference = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            TextureReference = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            SimDataReference = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            DescHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Read 14-byte unknown blob
            // Source: StyleLookResource.cs line 74
            if (offset + UnknownBlobSize > data.Length)
                return;
            data.Slice(offset, UnknownBlobSize).CopyTo(_unknownBlob);
            offset += UnknownBlobSize;

            Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Read first animation reference and state name
            // Source: StyleLookResource.cs lines 76-77
            AnimationReference1 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            AnimationStateName1 = ReadLengthPrefixedString(data, ref offset);

            // Read second animation reference and state name
            // Source: StyleLookResource.cs lines 78-79
            AnimationReference2 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            AnimationStateName2 = ReadLengthPrefixedString(data, ref offset);

            // Read color list
            // Source: StyleLookResource.cs line 80
            ColorList = SwatchColorList.Parse(data[offset..], out int colorBytesRead);
            offset += colorBytesRead;

            // Read flag list
            // Source: StyleLookResource.cs line 81
            FlagList = CaspFlagList.Parse(data[offset..], out int flagBytesRead);
            offset += flagBytesRead;

            IsValid = true;
        }
        catch
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Reads a length-prefixed ASCII string.
    /// Source: StyleLookResource.cs line 77
    /// </summary>
    private static string ReadLengthPrefixedString(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset + 4 > data.Length)
            throw new InvalidDataException("Truncated string length");

        int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (length < 0 || length > 10000)
            throw new InvalidDataException($"Invalid string length: {length}");

        if (length == 0)
            return string.Empty;

        if (offset + length > data.Length)
            throw new InvalidDataException("Truncated string data");

        var result = Encoding.ASCII.GetString(data.Slice(offset, length));
        offset += length;
        return result;
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate total size
        int totalSize = CalculateSerializedSize();
        byte[] buffer = new byte[totalSize];
        Span<byte> span = buffer;
        int offset = 0;

        // Write header fields
        // Source: StyleLookResource.cs lines 88-96
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Version);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)AgeGender);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], GroupingId);
        offset += 8;

        span[offset++] = Unknown1;

        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], SimOutfitReference);
        offset += 8;

        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], TextureReference);
        offset += 8;

        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], SimDataReference);
        offset += 8;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], NameHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], DescHash);
        offset += 4;

        // Write unknown blob
        // Source: StyleLookResource.cs line 97
        _unknownBlob.CopyTo(span[offset..]);
        offset += UnknownBlobSize;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Unknown3);
        offset += 4;

        // Write first animation reference and state name
        // Source: StyleLookResource.cs lines 99-101
        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], AnimationReference1);
        offset += 8;

        WriteLengthPrefixedString(span, ref offset, AnimationStateName1);

        // Write second animation reference and state name
        // Source: StyleLookResource.cs lines 102-104
        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], AnimationReference2);
        offset += 8;

        WriteLengthPrefixedString(span, ref offset, AnimationStateName2);

        // Write color list
        // Source: StyleLookResource.cs lines 105-106
        offset += ColorList.WriteTo(span[offset..]);

        // Write flag list
        // Source: StyleLookResource.cs lines 107-108
        offset += FlagList.WriteTo(span[offset..]);

        return buffer;
    }

    /// <summary>
    /// Writes a length-prefixed ASCII string.
    /// Source: StyleLookResource.cs line 100-101
    /// </summary>
    private static void WriteLengthPrefixedString(Span<byte> span, ref int offset, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], bytes.Length);
        offset += 4;

        if (bytes.Length > 0)
        {
            bytes.CopyTo(span[offset..]);
            offset += bytes.Length;
        }
    }

    /// <summary>
    /// Calculates the total serialized size.
    /// </summary>
    private int CalculateSerializedSize()
    {
        return 4 // Version
            + 4 // AgeGender
            + 8 // GroupingId
            + 1 // Unknown1
            + 8 // SimOutfitReference
            + 8 // TextureReference
            + 8 // SimDataReference
            + 4 // NameHash
            + 4 // DescHash
            + UnknownBlobSize // UnknownBlob
            + 4 // Unknown3
            + 8 // AnimationReference1
            + 4 + Encoding.ASCII.GetByteCount(AnimationStateName1 ?? string.Empty) // AnimationStateName1
            + 8 // AnimationReference2
            + 4 + Encoding.ASCII.GetByteCount(AnimationStateName2 ?? string.Empty) // AnimationStateName2
            + ColorList.GetSerializedSize()
            + FlagList.GetSerializedSize();
    }

    /// <summary>
    /// Sets the unknown blob data.
    /// </summary>
    public void SetUnknownBlob(ReadOnlySpan<byte> data)
    {
        if (data.Length != UnknownBlobSize)
            throw new ArgumentException($"Unknown blob must be exactly {UnknownBlobSize} bytes", nameof(data));
        data.CopyTo(_unknownBlob);
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        _unknownBlob = new byte[UnknownBlobSize];
        Version = 0;
        AgeGender = AgeGenderFlags.None;
        GroupingId = 0;
        Unknown1 = 0;
        SimOutfitReference = 0;
        TextureReference = 0;
        SimDataReference = 0;
        NameHash = 0;
        DescHash = 0;
        Unknown3 = 0;
        AnimationReference1 = 0;
        AnimationStateName1 = string.Empty;
        AnimationReference2 = 0;
        AnimationStateName2 = string.Empty;
        ColorList = [];
        FlagList = [];
        IsValid = false;
    }
}
