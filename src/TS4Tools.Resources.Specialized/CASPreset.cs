namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Represents a Character Asset System (CAS) preset with validation and serialization capabilities.
/// Encapsulates user-created character customization data in XML format with additional metadata.
/// </summary>
public sealed class CASPreset : ICASPreset, IEquatable<CASPreset>
{
    private string _xml;

    /// <summary>
    /// Initializes a new instance of the CASPreset class.
    /// </summary>
    /// <param name="xml">The XML data for the preset.</param>
    /// <param name="unknown1">Unknown data field 1.</param>
    /// <param name="unknown2">Unknown data field 2.</param>
    /// <param name="unknown3">Unknown data field 3.</param>
    /// <param name="unknown4">Unknown data field 4.</param>
    /// <param name="unknown5">Unknown data field 5.</param>
    /// <param name="unknown6">Unknown data field 6.</param>
    public CASPreset(string xml = "", byte unknown1 = 0, byte unknown2 = 0, uint unknown3 = 0,
                     byte unknown4 = 0, byte unknown5 = 0, byte unknown6 = 0)
    {
        _xml = xml ?? string.Empty;
        Unknown1 = unknown1;
        Unknown2 = unknown2;
        Unknown3 = unknown3;
        Unknown4 = unknown4;
        Unknown5 = unknown5;
        Unknown6 = unknown6;
    }

    /// <summary>
    /// Creates a CASPreset from binary data.
    /// </summary>
    /// <param name="reader">The binary reader to read from.</param>
    /// <returns>A new CASPreset instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data is invalid.</exception>
    public static CASPreset FromBinaryReader(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        try
        {
            // Read XML string length and data (stored as Unicode)
            var xmlLength = reader.ReadInt32();
            if (xmlLength < 0 || xmlLength > 1_000_000) // Reasonable limit
            {
                throw new InvalidDataException($"Invalid XML length: {xmlLength}");
            }

            var xmlBytes = reader.ReadBytes(xmlLength * 2); // Unicode = 2 bytes per char
            var xml = Encoding.Unicode.GetString(xmlBytes);

            // Read metadata fields
            var unknown1 = reader.ReadByte();
            var unknown2 = reader.ReadByte();
            var unknown3 = reader.ReadUInt32();
            var unknown4 = reader.ReadByte();
            var unknown5 = reader.ReadByte();
            var unknown6 = reader.ReadByte();

            return new CASPreset(xml, unknown1, unknown2, unknown3, unknown4, unknown5, unknown6);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Unexpected end of stream while reading CAS preset data", ex);
        }
    }

    /// <summary>
    /// Writes the CASPreset to binary format.
    /// </summary>
    /// <param name="writer">The binary writer to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
    public void WriteToBinaryWriter(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        // Write XML string length and data (as Unicode)
        writer.Write(_xml.Length);
        writer.Write(Encoding.Unicode.GetBytes(_xml));

        // Write metadata fields
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(Unknown5);
        writer.Write(Unknown6);
    }

    #region ICASPreset Implementation

    /// <inheritdoc />
    public string Xml
    {
        get => _xml;
        set => _xml = value ?? string.Empty;
    }

    /// <inheritdoc />
    public byte Unknown1 { get; set; }

    /// <inheritdoc />
    public byte Unknown2 { get; set; }

    /// <inheritdoc />
    public uint Unknown3 { get; set; }

    /// <inheritdoc />
    public byte Unknown4 { get; set; }

    /// <inheritdoc />
    public byte Unknown5 { get; set; }

    /// <inheritdoc />
    public byte Unknown6 { get; set; }

    /// <inheritdoc />
    public bool IsValid()
    {
        // Basic validation checks
        if (string.IsNullOrWhiteSpace(_xml))
        {
            return false;
        }

        // Validate XML is well-formed
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(_xml);
            return doc.Root != null;
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }
    }

    #endregion

    #region IEquatable<CASPreset> Implementation

    /// <inheritdoc />
    public bool Equals(CASPreset? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(_xml, other._xml, StringComparison.Ordinal) &&
               Unknown1 == other.Unknown1 &&
               Unknown2 == other.Unknown2 &&
               Unknown3 == other.Unknown3 &&
               Unknown4 == other.Unknown4 &&
               Unknown5 == other.Unknown5 &&
               Unknown6 == other.Unknown6;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CASPreset other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_xml, Unknown1, Unknown2, Unknown3, Unknown4, Unknown5, Unknown6);
    }

    /// <summary>
    /// Determines whether two CASPreset instances are equal.
    /// </summary>
    /// <param name="left">The first preset to compare.</param>
    /// <param name="right">The second preset to compare.</param>
    /// <returns>True if the presets are equal; otherwise, false.</returns>
    public static bool operator ==(CASPreset? left, CASPreset? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two CASPreset instances are not equal.
    /// </summary>
    /// <param name="left">The first preset to compare.</param>
    /// <param name="right">The second preset to compare.</param>
    /// <returns>True if the presets are not equal; otherwise, false.</returns>
    public static bool operator !=(CASPreset? left, CASPreset? right)
    {
        return !Equals(left, right);
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this CAS preset.
    /// </summary>
    /// <returns>A summary of the CAS preset contents.</returns>
    public override string ToString()
    {
        var xmlPreview = _xml.Length > 160 ? $"{_xml[..157]}..." : _xml;
        return $"CASPreset (XML: {xmlPreview}, Unknown1: {Unknown1}, Unknown2: {Unknown2}, " +
               $"Unknown3: {Unknown3}, Unknown4: {Unknown4}, Unknown5: {Unknown5}, Unknown6: {Unknown6})";
    }
}
