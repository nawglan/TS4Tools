using Microsoft.Extensions.Logging;
using System.Text;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Represents a structure definition within a DataResource
/// </summary>
public sealed class StructureDefinition
{
    /// <summary>
    /// Name of the structure
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the name (FNV-32)
    /// </summary>
    public uint NameHash { get; set; }

    /// <summary>
    /// Unknown field (typically 0)
    /// </summary>
    public uint Unknown08 { get; set; }

    /// <summary>
    /// Size of the structure in bytes
    /// </summary>
    public uint Size { get; set; }

    /// <summary>
    /// Number of fields in the structure
    /// </summary>
    public uint FieldCount => (uint)Fields.Count;

    /// <summary>
    /// Collection of field definitions in this structure
    /// </summary>
    public List<FieldDefinition> Fields { get; } = new();

    /// <summary>
    /// Internal position within the binary stream
    /// </summary>
    internal uint Position { get; set; }

    /// <summary>
    /// Internal name position within the binary stream
    /// </summary>
    internal uint NamePosition { get; set; }

    /// <summary>
    /// Internal field table position within the binary stream
    /// </summary>
    internal uint FieldTablePosition { get; set; }

    /// <summary>
    /// Parses a structure definition from a binary reader
    /// </summary>
    public static StructureDefinition Parse(BinaryReader reader, ILogger logger)
    {
        var structure = new StructureDefinition
        {
            Position = (uint)reader.BaseStream.Position
        };

        try
        {
            // Read structure header (24 bytes)
            var nameOffset = reader.ReadUInt32();
            structure.NameHash = reader.ReadUInt32();
            structure.Unknown08 = reader.ReadUInt32();
            structure.Size = reader.ReadUInt32();
            var fieldTableOffset = reader.ReadUInt32();
            var fieldCount = reader.ReadUInt32();

            // Calculate absolute positions
            const uint NullOffset = 0x80000000;
            structure.NamePosition = nameOffset == NullOffset ? NullOffset : nameOffset + structure.Position;
            structure.FieldTablePosition = fieldTableOffset == NullOffset ? NullOffset : fieldTableOffset + structure.Position + 16;

            // Read name if present
            if (structure.NamePosition != NullOffset && structure.NamePosition < reader.BaseStream.Length)
            {
                var currentPos = reader.BaseStream.Position;
                reader.BaseStream.Position = structure.NamePosition;
                structure.Name = ReadNullTerminatedString(reader);
                reader.BaseStream.Position = currentPos;
            }

            // Parse field table
            if (structure.FieldTablePosition != NullOffset && fieldCount > 0)
            {
                structure.ParseFieldTable(reader, fieldCount, logger);
            }

            return structure;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing structure definition at position 0x{Position:X8}", structure.Position);
            throw;
        }
    }

    /// <summary>
    /// Parses the field table for this structure
    /// </summary>
    private void ParseFieldTable(BinaryReader reader, uint fieldCount, ILogger logger)
    {
        if (FieldTablePosition == 0x80000000) return;

        var currentPos = reader.BaseStream.Position;
        reader.BaseStream.Position = FieldTablePosition;

        for (uint i = 0; i < fieldCount; i++)
        {
            try
            {
                var field = FieldDefinition.Parse(reader, logger);
                Fields.Add(field);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing field {Index} in structure {Name}", i, Name);
                break;
            }
        }

        reader.BaseStream.Position = currentPos;
    }

    /// <summary>
    /// Writes the structure definition to a binary writer
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        Position = (uint)writer.BaseStream.Position;

        // Calculate name hash if needed
        if (NameHash == 0 && !string.IsNullOrEmpty(Name))
        {
            NameHash = CalculateFnvHash(Name);
        }

        // Write structure header with placeholder offsets
        writer.Write(0u); // Name offset
        writer.Write(NameHash);
        writer.Write(Unknown08);
        writer.Write(Size);
        writer.Write(0u); // Field table offset
        writer.Write((uint)Fields.Count);
    }

    /// <summary>
    /// Writes the field table for this structure
    /// </summary>
    public void WriteFieldTable(BinaryWriter writer)
    {
        FieldTablePosition = (uint)writer.BaseStream.Position;

        foreach (var field in Fields)
        {
            field.WriteTo(writer);
        }
    }

    /// <summary>
    /// Updates offsets after all data has been written
    /// </summary>
    public void UpdateOffsets(BinaryWriter writer)
    {
        writer.BaseStream.Position = Position;

        // Write name offset
        const uint NullOffset = 0x80000000;
        writer.Write(NamePosition == NullOffset ? NullOffset : NamePosition - Position);
        writer.BaseStream.Position += 12; // Skip name hash, unknown08, size

        // Write field table offset
        writer.Write(FieldTablePosition == NullOffset ? NullOffset : FieldTablePosition - Position - 16);

        // Update field offsets
        foreach (var field in Fields)
        {
            field.UpdateOffsets(writer);
        }
    }

    /// <summary>
    /// Validates the structure definition
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrEmpty(Name))
        {
            errors.Add("Structure name is null or empty");
        }

        foreach (var field in Fields)
        {
            if (!field.Validate(out var fieldErrors))
            {
                errors.AddRange(fieldErrors.Select(e => $"Field '{field.Name}': {e}"));
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Finds a field by name
    /// </summary>
    public FieldDefinition? FindField(string name)
    {
        return Fields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a field to the structure
    /// </summary>
    public void AddField(FieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Fields.Add(field);
    }

    /// <summary>
    /// Removes a field from the structure
    /// </summary>
    public bool RemoveField(FieldDefinition field)
    {
        return Fields.Remove(field);
    }

    /// <summary>
    /// Reads a null-terminated string from a binary reader
    /// </summary>
    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        byte b;
        while (reader.BaseStream.Position < reader.BaseStream.Length && (b = reader.ReadByte()) != 0)
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
        return $"StructureDefinition: {Name} (Size: {Size}, Fields: {Fields.Count})";
    }
}

/// <summary>
/// Represents a field definition within a structure
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>
    /// Name of the field
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the name (FNV-32)
    /// </summary>
    public uint NameHash { get; set; }

    /// <summary>
    /// Data type of the field
    /// </summary>
    public FieldDataTypeFlags DataType { get; set; }

    /// <summary>
    /// Offset of the data within the structure
    /// </summary>
    public uint DataOffset { get; set; }

    /// <summary>
    /// Position offset (typically 0)
    /// </summary>
    public uint PositionOffset { get; set; }

    /// <summary>
    /// Internal position within the binary stream
    /// </summary>
    internal uint Position { get; set; }

    /// <summary>
    /// Internal name position within the binary stream
    /// </summary>
    internal uint NamePosition { get; set; }

    /// <summary>
    /// Parses a field definition from a binary reader
    /// </summary>
    public static FieldDefinition Parse(BinaryReader reader, ILogger logger)
    {
        var field = new FieldDefinition
        {
            Position = (uint)reader.BaseStream.Position
        };

        try
        {
            // Read field header (20 bytes)
            var nameOffset = reader.ReadUInt32();
            field.NameHash = reader.ReadUInt32();
            field.DataType = (FieldDataTypeFlags)reader.ReadUInt32();
            field.DataOffset = reader.ReadUInt32();
            field.PositionOffset = reader.ReadUInt32();

            // Calculate absolute name position
            const uint NullOffset = 0x80000000;
            field.NamePosition = nameOffset == NullOffset ? NullOffset : nameOffset + field.Position;

            // Read name if present
            if (field.NamePosition != NullOffset && field.NamePosition < reader.BaseStream.Length)
            {
                var currentPos = reader.BaseStream.Position;
                reader.BaseStream.Position = field.NamePosition;
                field.Name = ReadNullTerminatedString(reader);
                reader.BaseStream.Position = currentPos;
            }

            return field;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing field definition at position 0x{Position:X8}", field.Position);
            throw;
        }
    }

    /// <summary>
    /// Writes the field definition to a binary writer
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        Position = (uint)writer.BaseStream.Position;

        // Calculate name hash if needed
        if (NameHash == 0 && !string.IsNullOrEmpty(Name))
        {
            NameHash = CalculateFnvHash(Name);
        }

        // Write field header with placeholder name offset
        writer.Write(0u); // Name offset
        writer.Write(NameHash);
        writer.Write((uint)DataType);
        writer.Write(DataOffset);
        writer.Write(PositionOffset);
    }

    /// <summary>
    /// Updates offsets after all data has been written
    /// </summary>
    public void UpdateOffsets(BinaryWriter writer)
    {
        writer.BaseStream.Position = Position;

        // Write name offset
        const uint NullOffset = 0x80000000;
        writer.Write(NamePosition == NullOffset ? NullOffset : NamePosition - Position);
    }

    /// <summary>
    /// Validates the field definition
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrEmpty(Name))
        {
            errors.Add("Field name is null or empty");
        }

        if (!Enum.IsDefined(typeof(FieldDataTypeFlags), DataType))
        {
            errors.Add($"Invalid data type: {DataType}");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Reads a null-terminated string from a binary reader
    /// </summary>
    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        byte b;
        while (reader.BaseStream.Position < reader.BaseStream.Length && (b = reader.ReadByte()) != 0)
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
        return $"FieldDefinition: {Name} (Type: {DataType}, Offset: 0x{DataOffset:X})";
    }
}
