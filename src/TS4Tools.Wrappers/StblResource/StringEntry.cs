// Source: legacy_references/Sims4Tools/s4pi Wrappers/StblResource/StblResource.cs (StringEntry class, lines 162-290)

namespace TS4Tools.Wrappers;

/// <summary>
/// Represents a single entry in a string table (STBL) resource.
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/StblResource/StblResource.cs (StringEntry nested class)
/// Legacy uses AHandlerElement base class; this is a modern readonly record struct.
/// </remarks>
/// <param name="KeyHash">The FNV-32 hash of the string key.</param>
/// <param name="Flags">Metadata flags for this entry.</param>
/// <param name="Value">The string value (UTF-8 encoded in binary format).</param>
public readonly record struct StringEntry(uint KeyHash, byte Flags, string Value)
{
    /// <summary>
    /// Creates a new string entry with default flags.
    /// </summary>
    /// <param name="keyHash">The FNV-32 hash of the string key.</param>
    /// <param name="value">The string value.</param>
    public StringEntry(uint keyHash, string value) : this(keyHash, 0, value)
    {
    }
}
