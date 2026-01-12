// Source: legacy_references/Sims4Tools/s4pi Wrappers/UserCAStPresetResource/UserCAStPresetResource.cs lines 27-234

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// User Create-A-Sim preset resource containing preset configuration data.
/// Resource Type: 0x0591B1AF
/// </summary>
/// <remarks>
/// Format:
/// - version: uint32 (default: 3)
/// - unknown1: uint32 (base preset indicator?)
/// - unknown2: uint32
/// - unknown3: uint32
/// - presets: List of Preset entries
/// </remarks>
public sealed class UserCAStPresetResource : TypedResource
{
    private const uint DefaultVersion = 3;
    private const int MaxPresets = 10000; // Reasonable limit for validation
    private const int MaxXmlLength = 10 * 1024 * 1024; // 10 MB max XML length

    private List<Preset> _presets = [];

    /// <summary>
    /// Format version (default: 3).
    /// </summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Unknown field 1 (possibly base preset indicator).
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// </summary>
    public uint Unknown3 { get; set; }

    /// <summary>
    /// List of presets in this resource.
    /// </summary>
    public IReadOnlyList<Preset> Presets => _presets;

    /// <summary>
    /// Number of presets.
    /// </summary>
    public int Count => _presets.Count;

    /// <summary>
    /// Creates a new UserCAStPresetResource by parsing data.
    /// </summary>
    public UserCAStPresetResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: UserCAStPresetResource.cs lines 45-53
        int offset = 0;

        // Read header
        if (data.Length < 16)
            throw new ResourceFormatException("UserCAStPresetResource data too short for header.");

        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read preset count
        if (offset + 4 > data.Length)
            throw new ResourceFormatException("UserCAStPresetResource data too short for preset count.");

        int presetCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (presetCount < 0 || presetCount > MaxPresets)
            throw new ResourceFormatException($"Invalid preset count: {presetCount}");

        // Read presets
        _presets = new List<Preset>(presetCount);
        for (int i = 0; i < presetCount; i++)
        {
            var (preset, bytesRead) = Preset.Parse(data[offset..]);
            _presets.Add(preset);
            offset += bytesRead;
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: UserCAStPresetResource.cs lines 55-66
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header
        writer.Write(Version);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);

        // Write preset count and presets
        writer.Write(_presets.Count);
        foreach (var preset in _presets)
        {
            preset.WriteTo(writer);
        }

        return ms.ToArray();
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        Unknown1 = 0;
        Unknown2 = 0;
        Unknown3 = 0;
        _presets = [];
    }

    /// <summary>
    /// Adds a preset to the resource.
    /// </summary>
    public void Add(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        _presets.Add(preset);
        OnChanged();
    }

    /// <summary>
    /// Removes a preset from the resource.
    /// </summary>
    public bool Remove(Preset preset)
    {
        bool removed = _presets.Remove(preset);
        if (removed) OnChanged();
        return removed;
    }

    /// <summary>
    /// Removes the preset at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        _presets.RemoveAt(index);
        OnChanged();
    }

    /// <summary>
    /// Clears all presets.
    /// </summary>
    public void Clear()
    {
        _presets.Clear();
        OnChanged();
    }

    /// <summary>
    /// Gets or sets the preset at the specified index.
    /// </summary>
    public Preset this[int index]
    {
        get => _presets[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _presets[index] = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Represents a single CAS preset entry.
    /// </summary>
    /// <remarks>
    /// Source: UserCAStPresetResource.cs lines 70-204
    ///
    /// Format:
    /// - xml: Unicode string (int32 length prefix, then length * 2 bytes of UTF-16LE)
    /// - unknown1: byte
    /// - unknown2: byte
    /// - unknown3: uint32
    /// - unknown4: byte
    /// - unknown5: byte
    /// - unknown6: byte
    /// </remarks>
    public sealed class Preset : IEquatable<Preset>
    {
        /// <summary>
        /// The XML content of this preset.
        /// </summary>
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// Unknown field 1.
        /// </summary>
        public byte Unknown1 { get; set; }

        /// <summary>
        /// Unknown field 2.
        /// </summary>
        public byte Unknown2 { get; set; }

        /// <summary>
        /// Unknown field 3.
        /// </summary>
        public uint Unknown3 { get; set; }

        /// <summary>
        /// Unknown field 4.
        /// </summary>
        public byte Unknown4 { get; set; }

        /// <summary>
        /// Unknown field 5.
        /// </summary>
        public byte Unknown5 { get; set; }

        /// <summary>
        /// Unknown field 6.
        /// </summary>
        public byte Unknown6 { get; set; }

        /// <summary>
        /// Creates a new empty preset.
        /// </summary>
        public Preset()
        {
        }

        /// <summary>
        /// Creates a new preset with the specified XML content.
        /// </summary>
        public Preset(string xml)
        {
            Xml = xml ?? string.Empty;
        }

        /// <summary>
        /// Parses a preset from binary data.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <returns>The parsed preset and the number of bytes consumed.</returns>
        internal static (Preset Preset, int BytesRead) Parse(ReadOnlySpan<byte> data)
        {
            // Source: UserCAStPresetResource.cs Preset.Parse lines 105-115
            int offset = 0;

            // Read XML string (length-prefixed Unicode)
            if (data.Length < 4)
                throw new ResourceFormatException("Preset data too short for XML length.");

            int xmlLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (xmlLength < 0 || xmlLength > MaxXmlLength)
                throw new ResourceFormatException($"Invalid XML length: {xmlLength}");

            int xmlBytes = xmlLength * 2; // UTF-16LE
            if (offset + xmlBytes > data.Length)
                throw new ResourceFormatException("Preset data too short for XML content.");

            var xml = System.Text.Encoding.Unicode.GetString(data.Slice(offset, xmlBytes));
            offset += xmlBytes;

            // Read unknown bytes
            if (offset + 9 > data.Length) // 1 + 1 + 4 + 1 + 1 + 1 = 9 bytes
                throw new ResourceFormatException("Preset data too short for unknown fields.");

            var preset = new Preset
            {
                Xml = xml,
                Unknown1 = data[offset++],
                Unknown2 = data[offset++],
                Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]),
            };
            offset += 4;

            preset.Unknown4 = data[offset++];
            preset.Unknown5 = data[offset++];
            preset.Unknown6 = data[offset++];

            return (preset, offset);
        }

        /// <summary>
        /// Writes the preset to a binary writer.
        /// </summary>
        internal void WriteTo(BinaryWriter writer)
        {
            // Source: UserCAStPresetResource.cs Preset.UnParse lines 117-128
            writer.Write(Xml.Length);
            writer.Write(System.Text.Encoding.Unicode.GetBytes(Xml));
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
            writer.Write(Unknown6);
        }

        /// <inheritdoc/>
        public bool Equals(Preset? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Source: UserCAStPresetResource.cs Preset.Equals lines 133-142
            // Note: Legacy excludes unknown6 from equality - preserving that behavior
            return Xml == other.Xml
                && Unknown1 == other.Unknown1
                && Unknown2 == other.Unknown2
                && Unknown3 == other.Unknown3
                && Unknown4 == other.Unknown4
                && Unknown5 == other.Unknown5;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Preset);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // Source: UserCAStPresetResource.cs Preset.GetHashCode lines 148-158
            return HashCode.Combine(Xml, Unknown1, Unknown2, Unknown3, Unknown4, Unknown5);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var xmlPreview = Xml.Length > 160 ? Xml[..157] + "..." : Xml;
            return $"Preset: Xml=\"{xmlPreview}\"";
        }
    }
}
