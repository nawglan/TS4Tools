using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Skin Tone Resource for sim skin tone definitions.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SkinToneResource.cs
/// Type ID: 0x0354796A
/// </summary>
public sealed class SkinToneResource : TypedResource
{
    /// <summary>Type ID for Skin Tone resources.</summary>
    public const uint TypeId = 0x0354796A;

    private const int MaxOverlays = 1000;

    /// <summary>Resource version.</summary>
    public uint Version { get; set; } = 7;

    /// <summary>Reference to the RLE texture instance.</summary>
    public ulong TextureInstance { get; set; }

    /// <summary>List of overlay references for different age/gender combinations.</summary>
    public List<OverlayReference> OverlayList { get; set; } = [];

    /// <summary>Colorize saturation value.</summary>
    public ushort ColorizeSaturation { get; set; }

    /// <summary>Colorize hue value.</summary>
    public ushort ColorizeHue { get; set; }

    /// <summary>Colorize opacity value.</summary>
    public uint ColorizeOpacity { get; set; }

    /// <summary>Flag list containing CASP-style flags.</summary>
    public CaspFlagList FlagList { get; set; } = new();

    /// <summary>Makeup opacity value.</summary>
    public float MakeupOpacity { get; set; }

    /// <summary>List of swatch colors.</summary>
    public SwatchColorList SwatchList { get; set; } = new();

    /// <summary>Sort order for UI display.</summary>
    public float SortOrder { get; set; }

    /// <summary>Secondary makeup opacity value.</summary>
    public float MakeupOpacity2 { get; set; }

    /// <inheritdoc/>
    public SkinToneResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        if (data.Length < 20) // Minimum size
            throw new InvalidDataException($"SkinToneResource data too short: {data.Length} bytes");

        int offset = 0;
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        TextureInstance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Parse overlay list
        int overlayCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (overlayCount < 0 || overlayCount > MaxOverlays)
            throw new InvalidDataException($"Invalid overlay count: {overlayCount}");

        OverlayList.Clear();
        for (int i = 0; i < overlayCount; i++)
        {
            if (offset + 12 > data.Length)
                throw new InvalidDataException("Unexpected end of data while parsing overlays");

            var ageGender = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            var textureRef = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            OverlayList.Add(new OverlayReference(ageGender, textureRef));
        }

        if (offset + 8 > data.Length)
            throw new InvalidDataException("Unexpected end of data while parsing colorize data");

        ColorizeSaturation = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;
        ColorizeHue = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;
        ColorizeOpacity = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Parse flag list - version > 6 uses 32-bit flags, otherwise 16-bit
        FlagList = CaspFlagList.Parse(data, ref offset, Version > 6);

        if (offset + 4 > data.Length)
            throw new InvalidDataException("Unexpected end of data while parsing makeup opacity");

        MakeupOpacity = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        // Parse swatch list
        SwatchList = SwatchColorList.ParseAt(data, ref offset);

        if (offset + 8 > data.Length)
            throw new InvalidDataException("Unexpected end of data while parsing sort order");

        SortOrder = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
        MakeupOpacity2 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = GetSerializedSize();
        var buffer = new byte[size];
        var span = buffer.AsSpan();

        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Version);
        offset += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], TextureInstance);
        offset += 8;

        // Write overlay list
        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], OverlayList.Count);
        offset += 4;

        foreach (var overlay in OverlayList)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)overlay.AgeGender);
            offset += 4;
            BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], overlay.TextureReference);
            offset += 8;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], ColorizeSaturation);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], ColorizeHue);
        offset += 2;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], ColorizeOpacity);
        offset += 4;

        // Write flag list - version > 6 uses 32-bit flags
        FlagList.Serialize(span, ref offset, Version > 6);

        BinaryPrimitives.WriteSingleLittleEndian(span[offset..], MakeupOpacity);
        offset += 4;

        SwatchList.Serialize(span, ref offset);

        BinaryPrimitives.WriteSingleLittleEndian(span[offset..], SortOrder);
        offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(span[offset..], MakeupOpacity2);
        offset += 4;

        return buffer;
    }

    private int GetSerializedSize()
    {
        // version(4) + textureInstance(8) + overlayCount(4) + overlays(12 each)
        // + colorizeSaturation(2) + colorizeHue(2) + colorizeOpacity(4)
        // + flagList + makeupOpacity(4) + swatchList + sortOrder(4) + makeupOpacity2(4)
        int size = 4 + 8 + 4 + (OverlayList.Count * 12) + 2 + 2 + 4;
        size += FlagList.GetSerializedSize(Version > 6);
        size += 4; // makeupOpacity
        size += SwatchList.GetSerializedSize();
        size += 4 + 4; // sortOrder + makeupOpacity2
        return size;
    }
}

/// <summary>
/// Reference to an overlay texture for a specific age/gender combination.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SkinToneResource.cs lines 138-212
/// </summary>
/// <param name="AgeGender">Age and gender flags for this overlay.</param>
/// <param name="TextureReference">Reference to the texture instance.</param>
public readonly record struct OverlayReference(AgeGenderFlags AgeGender, ulong TextureReference);
