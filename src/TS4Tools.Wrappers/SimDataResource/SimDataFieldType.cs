namespace TS4Tools.Wrappers;

/// <summary>
/// Field data types used in SimData resources.
/// Source: DataFlags.cs lines 10-24
/// </summary>
public enum SimDataFieldType : uint
{
    /// <summary>Boolean value (4 bytes)</summary>
    Boolean = 0x00000000,

    /// <summary>16-bit integer (4 bytes, padded)</summary>
    Integer16 = 0x00000006,

    /// <summary>Unknown type 1 (4 bytes)</summary>
    Unknown1 = 0x00000007,

    /// <summary>Unknown type 2 (8 bytes)</summary>
    Unknown2 = 0x00000008,

    /// <summary>Floating point value (4 bytes)</summary>
    FloatValue = 0x0000000A,

    /// <summary>VFX reference (8 bytes)</summary>
    VFX = 0x0000000B,

    /// <summary>Unknown type 3 (8 bytes)</summary>
    Unknown3 = 0x0000000E,

    /// <summary>RGB color (12 bytes - 3 x int32)</summary>
    RGBColor = 0x00000010,

    /// <summary>ARGB color (16 bytes - 4 x int32)</summary>
    ARGBColor = 0x00000011,

    /// <summary>Data instance reference (8 bytes)</summary>
    DataInstance = 0x00000012,

    /// <summary>Image/texture instance reference (16 bytes - ResourceKey)</summary>
    ImageInstance = 0x00000013,

    /// <summary>String instance reference (4 bytes - offset)</summary>
    StringInstance = 0x00000014
}

/// <summary>
/// Provides size information for SimData field types.
/// Source: DataFlags.cs lines 26-47
/// </summary>
public static class SimDataFieldTypeExtensions
{
    /// <summary>
    /// Gets the byte size for a field type.
    /// </summary>
    public static int GetSize(this SimDataFieldType type) => type switch
    {
        SimDataFieldType.Boolean => 4,
        SimDataFieldType.Integer16 => 4,
        SimDataFieldType.Unknown1 => 4,
        SimDataFieldType.Unknown2 => 8,
        SimDataFieldType.FloatValue => 4,
        SimDataFieldType.VFX => 8,
        SimDataFieldType.Unknown3 => 8,
        SimDataFieldType.RGBColor => 12,
        SimDataFieldType.ARGBColor => 16,
        SimDataFieldType.DataInstance => 8,
        SimDataFieldType.ImageInstance => 16,
        SimDataFieldType.StringInstance => 4,
        _ => 4 // Default to 4 bytes for unknown types
    };

    /// <summary>
    /// Checks if a type value is a known field type.
    /// </summary>
    public static bool IsKnownType(uint typeValue) => typeValue switch
    {
        0x00 or 0x06 or 0x07 or 0x08 or 0x0A or 0x0B or 0x0E
            or 0x10 or 0x11 or 0x12 or 0x13 or 0x14 => true,
        _ => false
    };
}
