namespace TS4Tools.Resources.Strings;

/// <summary>
/// Represents a single string entry in a String Table (STBL) resource.
/// Each entry consists of a 32-bit key and a localized string value.
/// </summary>
/// <param name="Key">The unique identifier for this string entry</param>
/// <param name="Value">The localized string value</param>
public readonly record struct StringEntry(uint Key, string Value)
{
    /// <summary>
    /// Gets the unique identifier for this string entry.
    /// </summary>
    public uint Key { get; } = Key;

    /// <summary>
    /// Gets the localized string value.
    /// </summary>
    public string Value { get; } = Value ?? string.Empty;

    /// <summary>
    /// Gets the length of the string value in bytes when encoded as UTF-8.
    /// </summary>
    public int ByteLength => Encoding.UTF8.GetByteCount(Value);

    /// <summary>
    /// Creates a string entry with the specified key and value.
    /// </summary>
    /// <param name="key">The unique identifier</param>
    /// <param name="value">The string value</param>
    /// <returns>A new StringEntry instance</returns>
    public static StringEntry Create(uint key, string value) => new(key, value);

    /// <summary>
    /// Returns a string representation of this entry.
    /// </summary>
    /// <returns>A string in the format "Key: Value"</returns>
    public override string ToString() => $"0x{Key:X8}: {Value}";

    /// <summary>
    /// Reads a StringEntry from a binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the entry data</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <returns>The parsed StringEntry</returns>
    /// <exception cref="ArgumentNullException">Thrown when reader is null</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends unexpectedly</exception>
    public static StringEntry ReadFrom(BinaryReader reader, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        encoding ??= Encoding.UTF8;

        var key = reader.ReadUInt32();
        var length = reader.ReadByte();

        if (length == 0)
        {
            return new StringEntry(key, string.Empty);
        }

        var stringBytes = reader.ReadBytes(length);
        if (stringBytes.Length != length)
        {
            throw new EndOfStreamException($"Expected {length} bytes for string data, but only {stringBytes.Length} were available");
        }

        var value = encoding.GetString(stringBytes);
        return new StringEntry(key, value);
    }

    /// <summary>
    /// Writes this StringEntry to a binary writer.
    /// </summary>
    /// <param name="writer">The binary writer to write to</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <exception cref="ArgumentNullException">Thrown when writer is null</exception>
    /// <exception cref="ArgumentException">Thrown when the encoded string exceeds 255 bytes</exception>
    public void WriteTo(BinaryWriter writer, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        encoding ??= Encoding.UTF8;

        writer.Write(Key);

        if (string.IsNullOrEmpty(Value))
        {
            writer.Write((byte)0);
            return;
        }

        var stringBytes = encoding.GetBytes(Value);
        if (stringBytes.Length > byte.MaxValue)
        {
            throw new ArgumentException($"String value too long: {stringBytes.Length} bytes (max {byte.MaxValue})", nameof(writer));
        }

        writer.Write((byte)stringBytes.Length);
        writer.Write(stringBytes);
    }
}
