using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Sim Outfit resource (0x025ED6F4).
/// Contains sim outfit data with slider references, TGI lists, and customization data.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SimOutfitResource.cs lines 30-523
/// </summary>
public sealed class SimOutfitResource : TypedResource
{
    /// <summary>
    /// Type ID for Sim Outfit resources.
    /// </summary>
    public const uint TypeId = 0x025ED6F4;

    #region Properties

    /// <summary>
    /// Resource version.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Unknown float value 1.
    /// </summary>
    public float Unknown1 { get; set; }

    /// <summary>
    /// Unknown float value 2.
    /// </summary>
    public float Unknown2 { get; set; }

    /// <summary>
    /// Unknown float value 3.
    /// </summary>
    public float Unknown3 { get; set; }

    /// <summary>
    /// Unknown float value 4.
    /// </summary>
    public float Unknown4 { get; set; }

    /// <summary>
    /// Unknown float value 5.
    /// </summary>
    public float Unknown5 { get; set; }

    /// <summary>
    /// Unknown float value 6.
    /// </summary>
    public float Unknown6 { get; set; }

    /// <summary>
    /// Unknown float value 7.
    /// </summary>
    public float Unknown7 { get; set; }

    /// <summary>
    /// Unknown float value 8.
    /// </summary>
    public float Unknown8 { get; set; }

    /// <summary>
    /// Age flags.
    /// </summary>
    public AgeGenderFlags Age { get; set; }

    /// <summary>
    /// Gender flags.
    /// </summary>
    public AgeGenderFlags Gender { get; set; }

    /// <summary>
    /// Skin tone reference instance ID.
    /// </summary>
    public ulong SkinToneReference { get; set; }

    /// <summary>
    /// Unknown TGI index list.
    /// </summary>
    public List<byte> Unknown9 { get; set; } = [];

    /// <summary>
    /// Unknown byte list.
    /// </summary>
    public List<byte> UnknownByteList { get; set; } = [];

    /// <summary>
    /// Slider references list 1.
    /// </summary>
    public List<SliderReference> SliderReferences1 { get; set; } = [];

    /// <summary>
    /// Slider references list 2.
    /// </summary>
    public List<SliderReference> SliderReferences2 { get; set; } = [];

    /// <summary>
    /// Unknown data blob 10 (24 bytes).
    /// </summary>
    public byte[] Unknown10 { get; set; } = new byte[24];

    /// <summary>
    /// Unknown block list 11.
    /// </summary>
    public List<OutfitUnknownBlock> Unknown11 { get; set; } = [];

    /// <summary>
    /// Slider references list 3.
    /// </summary>
    public List<SliderReference> SliderReferences3 { get; set; } = [];

    /// <summary>
    /// Slider references list 4.
    /// </summary>
    public List<SliderReference> SliderReferences4 { get; set; } = [];

    /// <summary>
    /// Unknown data blob 12 (16 bytes).
    /// </summary>
    public byte[] Unknown12 { get; set; } = new byte[16];

    /// <summary>
    /// Slider references list 5.
    /// </summary>
    public List<SliderReference> SliderReferences5 { get; set; } = [];

    /// <summary>
    /// Unknown data blob 13 (9 bytes).
    /// </summary>
    public byte[] Unknown13 { get; set; } = new byte[9];

    /// <summary>
    /// CASP reference instance ID.
    /// </summary>
    public ulong CaspReference { get; set; }

    /// <summary>
    /// Data reference list (instance IDs).
    /// </summary>
    public List<ulong> DataReferenceList { get; set; } = [];

    /// <summary>
    /// TGI block list (stored at end of resource in IGT order).
    /// </summary>
    public List<TgiReference> TgiList { get; set; } = [];

    #endregion

    /// <summary>
    /// Creates a new Sim Outfit resource.
    /// </summary>
    public SimOutfitResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            InitializeDefaults();
            return;
        }

        int offset = 0;

        // Source: SimOutfitResource.cs lines 70-71
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) + 8;
        offset += 4;

        // Parse TGI list at tgiOffset first (needed for index references)
        // Source: SimOutfitResource.cs lines 74-79
        int tgiListOffset = (int)tgiOffset;
        if (tgiListOffset < data.Length)
        {
            int tgiCount = data[tgiListOffset++];
            TgiList = new List<TgiReference>(tgiCount);
            for (int i = 0; i < tgiCount; i++)
            {
                // IGT order: Instance (8), Group (4), Type (4)
                ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[tgiListOffset..]);
                tgiListOffset += 8;
                uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiListOffset..]);
                tgiListOffset += 4;
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiListOffset..]);
                tgiListOffset += 4;
                TgiList.Add(new TgiReference(instance, type, group));
            }
        }

        // Source: SimOutfitResource.cs lines 81-88
        Unknown1 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown2 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown3 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown4 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown5 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown6 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown7 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;
        Unknown8 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]); offset += 4;

        // Source: SimOutfitResource.cs lines 90-92
        Age = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]); offset += 4;
        Gender = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]); offset += 4;
        SkinToneReference = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]); offset += 8;

        // Source: SimOutfitResource.cs lines 94-96
        int unknown9Count = data[offset++];
        Unknown9 = new List<byte>(unknown9Count);
        for (int i = 0; i < unknown9Count; i++)
            Unknown9.Add(data[offset++]);

        // Source: SimOutfitResource.cs lines 98-99
        SliderReferences1 = ParseSliderReferenceList(data, ref offset);
        SliderReferences2 = ParseSliderReferenceList(data, ref offset);

        // Source: SimOutfitResource.cs lines 101-102
        Unknown10 = data.Slice(offset, 24).ToArray();
        offset += 24;
        Unknown11 = ParseUnknownBlockList(data, ref offset);

        // Source: SimOutfitResource.cs lines 104-109
        int byteListCount = data[offset++];
        UnknownByteList = new List<byte>(byteListCount);
        for (int i = 0; i < byteListCount; i++)
            UnknownByteList.Add(data[offset++]);

        // Source: SimOutfitResource.cs lines 111-112
        SliderReferences3 = ParseSliderReferenceList(data, ref offset);
        SliderReferences4 = ParseSliderReferenceList(data, ref offset);

        // Source: SimOutfitResource.cs lines 114-116
        Unknown12 = data.Slice(offset, 16).ToArray();
        offset += 16;
        SliderReferences5 = ParseSliderReferenceList(data, ref offset);
        Unknown13 = data.Slice(offset, 9).ToArray();
        offset += 9;

        // Source: SimOutfitResource.cs lines 117-121
        CaspReference = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]); offset += 8;
        int dataRefCount = data[offset++];
        DataReferenceList = new List<ulong>(dataRefCount);
        for (int i = 0; i < dataRefCount; i++)
        {
            DataReferenceList.Add(BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]));
            offset += 8;
        }
    }

    private static List<SliderReference> ParseSliderReferenceList(ReadOnlySpan<byte> data, ref int offset)
    {
        int count = data[offset++];
        var list = new List<SliderReference>(count);
        for (int i = 0; i < count; i++)
        {
            byte index = data[offset++];
            float value = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            list.Add(new SliderReference(index, value));
        }
        return list;
    }

    private static List<OutfitUnknownBlock> ParseUnknownBlockList(ReadOnlySpan<byte> data, ref int offset)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        var list = new List<OutfitUnknownBlock>(count);
        for (int i = 0; i < count; i++)
        {
            var block = OutfitUnknownBlock.Parse(data, ref offset);
            list.Add(block);
        }
        return list;
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = GetSerializedSize();
        var buffer = new byte[size];
        int offset = 0;

        // Write version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        // Write tgiOffset placeholder (will be filled in later)
        int tgiOffsetPosition = offset;
        offset += 4;

        // Source: SimOutfitResource.cs lines 132-142
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown1); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown2); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown3); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown4); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown5); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown6); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown7); offset += 4;
        BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), Unknown8); offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)Age); offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)Gender); offset += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), SkinToneReference); offset += 8;

        // Unknown9
        buffer[offset++] = (byte)Unknown9.Count;
        foreach (var b in Unknown9)
            buffer[offset++] = b;

        // SliderReferences1, SliderReferences2
        WriteSliderReferenceList(buffer.AsSpan(), ref offset, SliderReferences1);
        WriteSliderReferenceList(buffer.AsSpan(), ref offset, SliderReferences2);

        // Unknown10
        Unknown10.CopyTo(buffer.AsSpan(offset));
        offset += 24;

        // Unknown11
        WriteUnknownBlockList(buffer.AsSpan(), ref offset, Unknown11);

        // UnknownByteList
        buffer[offset++] = (byte)UnknownByteList.Count;
        foreach (var b in UnknownByteList)
            buffer[offset++] = b;

        // SliderReferences3, SliderReferences4
        WriteSliderReferenceList(buffer.AsSpan(), ref offset, SliderReferences3);
        WriteSliderReferenceList(buffer.AsSpan(), ref offset, SliderReferences4);

        // Unknown12
        Unknown12.CopyTo(buffer.AsSpan(offset));
        offset += 16;

        // SliderReferences5
        WriteSliderReferenceList(buffer.AsSpan(), ref offset, SliderReferences5);

        // Unknown13
        Unknown13.CopyTo(buffer.AsSpan(offset));
        offset += 9;

        // CaspReference and DataReferenceList
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), CaspReference); offset += 8;
        buffer[offset++] = (byte)DataReferenceList.Count;
        foreach (var dataRef in DataReferenceList)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), dataRef);
            offset += 8;
        }

        // Write tgiOffset (offset - 8 as per legacy: line 166)
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(tgiOffsetPosition), (uint)(offset - 8));

        // Write TGI list at end (IGT order: Instance, Group, Type)
        // Source: SimOutfitResource.cs lines 168-174
        buffer[offset++] = (byte)TgiList.Count;
        foreach (var tgi in TgiList)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), tgi.Instance);
            offset += 8;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), tgi.Group);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), tgi.Type);
            offset += 4;
        }

        return buffer;
    }

    private static void WriteSliderReferenceList(Span<byte> buffer, ref int offset, List<SliderReference> list)
    {
        buffer[offset++] = (byte)list.Count;
        foreach (var slider in list)
        {
            buffer[offset++] = slider.Index;
            BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], slider.Value);
            offset += 4;
        }
    }

    private static void WriteUnknownBlockList(Span<byte> buffer, ref int offset, List<OutfitUnknownBlock> list)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], list.Count);
        offset += 4;
        foreach (var block in list)
        {
            block.WriteTo(buffer, ref offset);
        }
    }

    private int GetSerializedSize()
    {
        int size = 4 + 4; // version + tgiOffset
        size += 4 * 8; // Unknown1-8
        size += 4 + 4 + 8; // Age + Gender + SkinToneReference
        size += 1 + Unknown9.Count; // Unknown9
        size += GetSliderListSize(SliderReferences1);
        size += GetSliderListSize(SliderReferences2);
        size += 24; // Unknown10
        size += GetUnknownBlockListSize(Unknown11);
        size += 1 + UnknownByteList.Count;
        size += GetSliderListSize(SliderReferences3);
        size += GetSliderListSize(SliderReferences4);
        size += 16; // Unknown12
        size += GetSliderListSize(SliderReferences5);
        size += 9; // Unknown13
        size += 8 + 1 + (DataReferenceList.Count * 8); // CaspReference + count + refs
        size += 1 + (TgiList.Count * 16); // TGI list (count + entries)
        return size;
    }

    private static int GetSliderListSize(List<SliderReference> list)
    {
        return 1 + (list.Count * 5); // count byte + (index byte + float value per entry)
    }

    private static int GetUnknownBlockListSize(List<OutfitUnknownBlock> list)
    {
        int size = 4; // count
        foreach (var block in list)
        {
            size += block.GetSerializedSize();
        }
        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 1;
        Unknown10 = new byte[24];
        Unknown12 = new byte[16];
        Unknown13 = new byte[9];
    }
}

/// <summary>
/// A slider reference with TGI index and value.
/// Source: SimOutfitResource.cs lines 182-219
/// </summary>
public readonly record struct SliderReference(byte Index, float Value)
{
    /// <summary>
    /// Serialized size: 1 (index) + 4 (value) = 5 bytes.
    /// </summary>
    public const int SerializedSize = 5;
}

/// <summary>
/// Unknown reference with index and value.
/// Source: SimOutfitResource.cs lines 283-320
/// </summary>
public readonly record struct OutfitUnknownReference2(byte TgiIndex, uint ReferenceValue)
{
    /// <summary>
    /// Serialized size: 1 (index) + 4 (value) = 5 bytes.
    /// </summary>
    public const int SerializedSize = 5;
}

/// <summary>
/// Unknown reference block containing a data blob and list of unknown reference 2.
/// Source: SimOutfitResource.cs lines 247-281
/// </summary>
public sealed class OutfitUnknownReference
{
    /// <summary>
    /// Unknown data blob (17 bytes).
    /// </summary>
    public byte[] UnknownBlob { get; set; } = new byte[17];

    /// <summary>
    /// List of unknown reference 2.
    /// </summary>
    public List<OutfitUnknownReference2> UnknownReference2List { get; set; } = [];

    /// <summary>
    /// Parses from binary data.
    /// </summary>
    public static OutfitUnknownReference Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var result = new OutfitUnknownReference();
        result.UnknownBlob = data.Slice(offset, 17).ToArray();
        offset += 17;

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        result.UnknownReference2List = new List<OutfitUnknownReference2>(count);
        for (int i = 0; i < count; i++)
        {
            byte index = data[offset++];
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            result.UnknownReference2List.Add(new OutfitUnknownReference2(index, value));
        }

        return result;
    }

    /// <summary>
    /// Writes to buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer, ref int offset)
    {
        UnknownBlob.CopyTo(buffer[offset..]);
        offset += 17;

        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], UnknownReference2List.Count);
        offset += 4;

        foreach (var ref2 in UnknownReference2List)
        {
            buffer[offset++] = ref2.TgiIndex;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], ref2.ReferenceValue);
            offset += 4;
        }
    }

    /// <summary>
    /// Gets serialized size.
    /// </summary>
    public int GetSerializedSize()
    {
        return 17 + 4 + (UnknownReference2List.Count * 5);
    }
}

/// <summary>
/// Unknown block containing block index, value, and reference list.
/// Source: SimOutfitResource.cs lines 322-370
/// </summary>
public sealed class OutfitUnknownBlock
{
    /// <summary>
    /// Block index (TGI index).
    /// </summary>
    public byte BlockIndex { get; set; }

    /// <summary>
    /// Unknown value.
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// List of unknown references.
    /// </summary>
    public List<OutfitUnknownReference> UnknownReferenceList { get; set; } = [];

    /// <summary>
    /// Parses from binary data.
    /// Source: SimOutfitResource.cs lines 333-339
    /// </summary>
    public static OutfitUnknownBlock Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var block = new OutfitUnknownBlock();
        block.BlockIndex = data[offset++];
        block.Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        block.UnknownReferenceList = new List<OutfitUnknownReference>(count);
        for (int i = 0; i < count; i++)
        {
            var reference = OutfitUnknownReference.Parse(data, ref offset);
            block.UnknownReferenceList.Add(reference);
        }

        return block;
    }

    /// <summary>
    /// Writes to buffer.
    /// Source: SimOutfitResource.cs lines 341-347
    /// </summary>
    public void WriteTo(Span<byte> buffer, ref int offset)
    {
        buffer[offset++] = BlockIndex;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown1);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], UnknownReferenceList.Count);
        offset += 4;

        foreach (var reference in UnknownReferenceList)
        {
            reference.WriteTo(buffer, ref offset);
        }
    }

    /// <summary>
    /// Gets serialized size.
    /// </summary>
    public int GetSerializedSize()
    {
        int size = 1 + 4 + 4; // BlockIndex + Unknown1 + count
        foreach (var reference in UnknownReferenceList)
        {
            size += reference.GetSerializedSize();
        }
        return size;
    }
}
