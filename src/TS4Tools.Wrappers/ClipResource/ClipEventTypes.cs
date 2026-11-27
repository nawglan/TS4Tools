// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/ClipEvents.cs lines 65-503

namespace TS4Tools.Wrappers;

/// <summary>
/// Sound event - plays a sound by name.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 65-120
/// </remarks>
public sealed class ClipEventSound : ClipEvent
{
    /// <summary>Name of the sound to play.</summary>
    public string SoundName { get; set; } = string.Empty;

    /// <summary>
    /// True when the name is stored as a 32-bit length followed by ASCII chars (the compact layout in
    /// current TS4 clips) instead of legacy's fixed 0x80-byte field (ClipEvents.cs lines 95-104).
    /// </summary>
    public bool LengthPrefixedName { get; set; }

    /// <inheritdoc/>
    protected override int TypeDataSize => LengthPrefixedName ? 4 + Encoding.ASCII.GetByteCount(SoundName) : ClipEventConstants.FixedStringLength;

    /// <summary>Whether <paramref name="payload"/> is exactly an int32 length followed by that many bytes.</summary>
    internal static bool IsLengthPrefixed(ReadOnlySpan<byte> payload) =>
        payload.Length >= 4 && BinaryPrimitives.ReadInt32LittleEndian(payload) == payload.Length - 4;

    /// <summary>Creates a new sound event.</summary>
    public ClipEventSound() : base(ClipEventType.Sound) { }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        if (LengthPrefixedName)
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            SoundName = Encoding.ASCII.GetString(data.Slice(offset + 4, length));
            return;
        }

        SoundName = ReadStringFixedField(data, offset, 0);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        if (LengthPrefixedName)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(SoundName);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            return;
        }

        WriteStringFixedField(writer, SoundName, 0);
    }
}

/// <summary>
/// Script event - contains raw script/bytecode data.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 122-168
/// </remarks>
public sealed class ClipEventScript : ClipEvent
{
    private byte[] _data = [];

    /// <summary>Raw script data.</summary>
    public byte[] Data
    {
        get => _data;
        set => _data = value ?? [];
    }

    /// <inheritdoc/>
    protected override int TypeDataSize => _data.Length;

    /// <summary>Creates a new script event with the specified data size.</summary>
    public ClipEventScript(int dataSize = 0) : base(ClipEventType.Script)
    {
        _data = new byte[Math.Max(0, dataSize)];
    }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        data.Slice(offset, _data.Length).CopyTo(_data);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        writer.Write(_data);
    }
}

/// <summary>
/// Effect event - spawns a visual effect at a slot.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 170-269
/// </remarks>
public sealed class ClipEventEffect : ClipEvent
{
    /// <summary>Slot name where effect is attached.</summary>
    public string SlotName { get; set; } = string.Empty;

    /// <summary>Hash of the actor name.</summary>
    public uint ActorNameHash { get; set; }

    /// <summary>Hash of the slot name.</summary>
    public uint SlotNameHash { get; set; }

    /// <summary>Unknown bytes (16 bytes).</summary>
    public byte[] UnknownBytes { get; set; } = new byte[16];

    /// <summary>Name of the effect to play.</summary>
    public string EffectName { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override int TypeDataSize => LegacyTypeDataSize;

    /// <summary>Payload size of the legacy layout (ClipEvents.cs ClipEventEffect).</summary>
    internal const int LegacyTypeDataSize = (2 * ClipEventConstants.FixedStringLength) + 24;

    /// <summary>Creates a new effect event.</summary>
    public ClipEventEffect() : base(ClipEventType.Effect) { }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        SlotName = ReadStringFixedField(data, offset, 0);
        offset += ClipEventConstants.FixedStringLength;

        ActorNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        SlotNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        data.Slice(offset, 16).CopyTo(UnknownBytes);
        offset += 16;

        EffectName = ReadStringFixedField(data, offset, 1);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        WriteStringFixedField(writer, SlotName, 0);
        writer.Write(ActorNameHash);
        writer.Write(SlotNameHash);
        writer.Write(UnknownBytes);
        WriteStringFixedField(writer, EffectName, 1);
    }
}

/// <summary>
/// Snap event - positioning/snapping data.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 271-317
/// </remarks>
public sealed class ClipEventSnap : ClipEvent
{
    private byte[] _data = [];

    /// <summary>Raw snap data.</summary>
    public byte[] Data
    {
        get => _data;
        set => _data = value ?? [];
    }

    /// <inheritdoc/>
    protected override int TypeDataSize => _data.Length;

    /// <summary>Creates a new snap event with the specified data size.</summary>
    public ClipEventSnap(int dataSize = 0) : base(ClipEventType.Snap)
    {
        _data = new byte[Math.Max(0, dataSize)];
    }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        data.Slice(offset, _data.Length).CopyTo(_data);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        writer.Write(_data);
    }
}

/// <summary>
/// Double modifier sound event.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 319-396
/// </remarks>
public sealed class ClipEventDoubleModifierSound : ClipEvent
{
    /// <summary>Unknown string.</summary>
    public string Unknown3 { get; set; } = string.Empty;

    /// <summary>Hash of the actor name.</summary>
    public uint ActorNameHash { get; set; }

    /// <summary>Hash of the slot name.</summary>
    public uint SlotNameHash { get; set; }

    /// <inheritdoc/>
    protected override int TypeDataSize => LegacyTypeDataSize;

    /// <summary>Payload size of the legacy layout (ClipEvents.cs ClipEventDoubleModifierSound).</summary>
    internal const int LegacyTypeDataSize = ClipEventConstants.FixedStringLength + 8;

    /// <summary>Creates a new double modifier sound event.</summary>
    public ClipEventDoubleModifierSound() : base(ClipEventType.DoubleModifierSound) { }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        Unknown3 = ReadStringFixedField(data, offset, 0);
        offset += ClipEventConstants.FixedStringLength;

        ActorNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        SlotNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        WriteStringFixedField(writer, Unknown3, 0);
        writer.Write(ActorNameHash);
        writer.Write(SlotNameHash);
    }
}

/// <summary>
/// Censor event - controls censorship overlay.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 398-454
/// </remarks>
public sealed class ClipEventCensor : ClipEvent
{
    /// <summary>Unknown float value.</summary>
    public float Unknown3 { get; set; }

    /// <inheritdoc/>
    protected override int TypeDataSize => LegacyTypeDataSize;

    /// <summary>Payload size of the legacy layout (ClipEvents.cs ClipEventCensor).</summary>
    internal const int LegacyTypeDataSize = 4;

    /// <summary>Creates a new censor event.</summary>
    public ClipEventCensor() : base(ClipEventType.Censor) { }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        Unknown3 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        writer.Write(Unknown3);
    }
}

/// <summary>
/// Unknown/unrecognized event type - preserves raw data.
/// </summary>
/// <remarks>
/// Source: ClipEvents.cs lines 456-502
/// </remarks>
public sealed class ClipEventUnknown : ClipEvent
{
    private byte[] _data = [];

    /// <summary>Raw event data.</summary>
    public byte[] Data
    {
        get => _data;
        set => _data = value ?? [];
    }

    /// <inheritdoc/>
    protected override int TypeDataSize => _data.Length;

    /// <summary>Creates a new unknown event with the specified type and data size.</summary>
    public ClipEventUnknown(ClipEventType typeId, int dataSize = 0) : base(typeId)
    {
        _data = new byte[Math.Max(0, dataSize)];
    }

    /// <inheritdoc/>
    protected override void ReadTypeData(ReadOnlySpan<byte> data, int offset)
    {
        data.Slice(offset, _data.Length).CopyTo(_data);
    }

    /// <inheritdoc/>
    protected override void WriteTypeData(BinaryWriter writer)
    {
        writer.Write(_data);
    }
}
