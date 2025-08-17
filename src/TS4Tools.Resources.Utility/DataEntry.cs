using Microsoft.Extensions.Logging;
using System.Text;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Represents a data entry within a DataResource
/// </summary>
public sealed class DataEntry
{
    private const uint NullOffset = 0x80000000;

    /// <summary>
    /// Name of the data entry
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the name (FNV-32)
    /// </summary>
    public uint NameHash { get; set; }

    /// <summary>
    /// Reference to the structure definition that describes this data
    /// </summary>
    public StructureDefinition? Structure { get; set; }

    /// <summary>
    /// Data type flags for this entry
    /// </summary>
    public FieldDataTypeFlags DataType { get; set; }

    /// <summary>
    /// Size of the field data in bytes
    /// </summary>
    public uint FieldSize { get; set; }

    /// <summary>
    /// Number of fields/elements
    /// </summary>
    public uint FieldCount { get; set; }

    /// <summary>
    /// Raw field data
    /// </summary>
    public byte[] FieldData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Indicates if this entry represents null data
    /// </summary>
    public bool IsNull { get; set; }

    /// <summary>
    /// Internal position within the binary stream (used for offset calculations)
    /// </summary>
    internal uint Position { get; set; }

    /// <summary>
    /// Internal name position within the binary stream
    /// </summary>
    internal uint NamePosition { get; set; }

    /// <summary>
    /// Internal structure position within the binary stream
    /// </summary>
    internal uint StructurePosition { get; set; }

    /// <summary>
    /// Internal field data position within the binary stream
    /// </summary>
    internal uint FieldPosition { get; set; }

    /// <summary>
    /// Parses a data entry from a binary reader
    /// </summary>
    public static DataEntry Parse(BinaryReader reader, IReadOnlyList<StructureDefinition> structures, ILogger logger)
    {
        var entry = new DataEntry
        {
            Position = (uint)reader.BaseStream.Position
        };

        try
        {
            // Read header (28 bytes total)
            var nameOffset = reader.ReadUInt32();
            entry.NameHash = reader.ReadUInt32();
            var structureOffset = reader.ReadUInt32();
            entry.DataType = (FieldDataTypeFlags)reader.ReadUInt32();
            entry.FieldSize = reader.ReadUInt32();
            var fieldOffset = reader.ReadUInt32();
            entry.FieldCount = reader.ReadUInt32();

            // Calculate absolute positions
            entry.NamePosition = nameOffset == NullOffset ? NullOffset : nameOffset + entry.Position;
            entry.StructurePosition = structureOffset == NullOffset ? NullOffset : structureOffset + entry.Position + 8;
            entry.FieldPosition = fieldOffset == NullOffset ? NullOffset : fieldOffset + entry.Position + 20;

            // Read name if present
            if (entry.NamePosition != NullOffset && entry.NamePosition < reader.BaseStream.Length)
            {
                var currentPos = reader.BaseStream.Position;
                reader.BaseStream.Position = entry.NamePosition;
                entry.Name = ReadNullTerminatedString(reader);
                reader.BaseStream.Position = currentPos;
            }

            // Find associated structure
            if (entry.StructurePosition != NullOffset)
            {
                entry.Structure = structures.FirstOrDefault(s => s.Position == entry.StructurePosition);
                if (entry.Structure == null)
                {
                    logger.LogWarning("Could not find structure at position 0x{Position:X8}", entry.StructurePosition);
                }
            }

            // Read field data if present
            if (entry.FieldPosition != NullOffset && entry.FieldSize > 0 && entry.FieldPosition < reader.BaseStream.Length)
            {
                var currentPos = reader.BaseStream.Position;
                reader.BaseStream.Position = entry.FieldPosition;

                var dataSize = Math.Min((long)entry.FieldSize, reader.BaseStream.Length - reader.BaseStream.Position);
                entry.FieldData = reader.ReadBytes((int)dataSize);

                reader.BaseStream.Position = currentPos;
            }

            entry.IsNull = entry.FieldPosition == NullOffset || entry.FieldSize == 0;

            return entry;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing data entry at position 0x{Position:X8}", entry.Position);
            throw;
        }
    }

    /// <summary>
    /// Writes the data entry to a binary writer
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        Position = (uint)writer.BaseStream.Position;

        // Calculate name hash if needed
        if (NameHash == 0 && !string.IsNullOrEmpty(Name))
        {
            NameHash = CalculateFnvHash(Name);
        }

        // Write header with placeholder offsets (will be fixed up later)
        writer.Write(0u); // Name offset
        writer.Write(NameHash);
        writer.Write(0u); // Structure offset
        writer.Write((uint)DataType);
        writer.Write(FieldSize);
        writer.Write(0u); // Field offset
        writer.Write(FieldCount);
    }

    /// <summary>
    /// Updates offsets after all data has been written
    /// </summary>
    public void UpdateOffsets(BinaryWriter writer)
    {
        writer.BaseStream.Position = Position;

        // Write name offset
        writer.Write(NamePosition == NullOffset ? NullOffset : NamePosition - Position);
        writer.BaseStream.Position += 4; // Skip name hash

        // Write structure offset
        writer.Write(StructurePosition == NullOffset ? NullOffset : StructurePosition - Position - 8);
        writer.BaseStream.Position += 8; // Skip data type and field size

        // Write field offset
        writer.Write(FieldPosition == NullOffset ? NullOffset : FieldPosition - Position - 20);
    }

    /// <summary>
    /// Validates the data entry
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrEmpty(Name))
        {
            errors.Add("Data entry name is null or empty");
        }

        if (FieldSize > 0 && FieldData.Length == 0 && !IsNull)
        {
            errors.Add($"Field size is {FieldSize} but no field data is present");
        }

        if (FieldData.Length > 0 && FieldSize == 0)
        {
            errors.Add($"Field data present ({FieldData.Length} bytes) but field size is 0");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Gets the field data as a specific type
    /// </summary>
    public T? GetFieldDataAs<T>() where T : struct
    {
        if (FieldData.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(FieldData);
            using var reader = new BinaryReader(stream);

            return typeof(T).Name switch
            {
                nameof(UInt32) => (T)(object)reader.ReadUInt32(),
                nameof(Int32) => (T)(object)reader.ReadInt32(),
                nameof(UInt64) => (T)(object)reader.ReadUInt64(),
                nameof(Int64) => (T)(object)reader.ReadInt64(),
                nameof(Single) => (T)(object)reader.ReadSingle(),
                nameof(Double) => (T)(object)reader.ReadDouble(),
                nameof(Boolean) => (T)(object)reader.ReadBoolean(),
                nameof(Byte) => (T)(object)reader.ReadByte(),
                nameof(UInt16) => (T)(object)reader.ReadUInt16(),
                nameof(Int16) => (T)(object)reader.ReadInt16(),
                _ => default(T?)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the field data as a string
    /// </summary>
    public string? GetFieldDataAsString()
    {
        if (FieldData.Length == 0)
            return null;

        try
        {
            // Try UTF-8 first
            var result = Encoding.UTF8.GetString(FieldData);
            if (!result.Contains('\uFFFD')) // No replacement characters
                return result.TrimEnd('\0');

            // Fall back to ASCII
            return Encoding.ASCII.GetString(FieldData).TrimEnd('\0');
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets field data from a value
    /// </summary>
    public void SetFieldData<T>(T value) where T : struct
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        switch (value)
        {
            case uint uintVal:
                writer.Write(uintVal);
                DataType = FieldDataTypeFlags.UInt32;
                break;
            case int intVal:
                writer.Write(intVal);
                DataType = FieldDataTypeFlags.Int32;
                break;
            case ulong ulongVal:
                writer.Write(ulongVal);
                DataType = FieldDataTypeFlags.UInt64;
                break;
            case long longVal:
                writer.Write(longVal);
                DataType = FieldDataTypeFlags.Int64;
                break;
            case float floatVal:
                writer.Write(floatVal);
                DataType = FieldDataTypeFlags.Single;
                break;
            case double doubleVal:
                writer.Write(doubleVal);
                DataType = FieldDataTypeFlags.Double;
                break;
            case bool boolVal:
                writer.Write(boolVal);
                DataType = FieldDataTypeFlags.Boolean;
                break;
            case byte byteVal:
                writer.Write(byteVal);
                DataType = FieldDataTypeFlags.UInt8;
                break;
            default:
                throw new ArgumentException($"Unsupported type: {typeof(T).Name}");
        }

        FieldData = stream.ToArray();
        FieldSize = (uint)FieldData.Length;
        IsNull = false;
    }

    /// <summary>
    /// Sets field data from a string
    /// </summary>
    public void SetFieldData(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            FieldData = Array.Empty<byte>();
            FieldSize = 0;
            IsNull = true;
            DataType = FieldDataTypeFlags.String;
            return;
        }

        FieldData = Encoding.UTF8.GetBytes(value + '\0'); // Null-terminated
        FieldSize = (uint)FieldData.Length;
        DataType = FieldDataTypeFlags.String;
        IsNull = false;
    }

    /// <summary>
    /// Reads a null-terminated string from a binary reader
    /// </summary>
    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Calculates FNV-32 hash for a string
    /// </summary>
    private static uint CalculateFnvHash(string input)
    {
        const uint fnvPrime = 0x01000193;
        const uint fnvOffsetBasis = 0x811C9DC5;

        uint hash = fnvOffsetBasis;
        var bytes = Encoding.UTF8.GetBytes(input);

        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= fnvPrime;
        }

        return hash;
    }

    public override string ToString()
    {
        return $"DataEntry: {Name} (Type: {DataType}, Size: {FieldSize}, Count: {FieldCount})";
    }
}

/// <summary>
/// Field data type flags used in DataResource
/// </summary>
[Flags]
public enum FieldDataTypeFlags : uint
{
    Unknown = 0,
    UInt8 = 0x01,
    Int8 = 0x02,
    UInt16 = 0x03,
    Int16 = 0x04,
    UInt32 = 0x05,
    Int32 = 0x06,
    UInt64 = 0x07,
    Int64 = 0x08,
    Boolean = 0x09,
    Single = 0x0A,
    Double = 0x0B,
    String = 0x0C,
    Object = 0x0D,
    Vector2 = 0x0E,
    Vector3 = 0x0F,
    Vector4 = 0x10,
    LocalizationKey = 0x11,
    TableSetReference = 0x12,
    ResourceKey = 0x13,
    Transform = 0x14,
    Color = 0x15,
    Variant = 0x16
}
