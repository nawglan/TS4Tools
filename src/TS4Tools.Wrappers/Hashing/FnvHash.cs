// Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs

namespace TS4Tools.Wrappers.Hashing;

/// <summary>
/// Fowler-Noll-Vo hash functions used by The Sims 4 for string hashing.
/// Implements FNV-1a variant for both 32-bit and 64-bit hashes.
/// </summary>
/// <remarks>
/// Source: CS System Classes/FNVHash.cs
/// Legacy implementation uses HashAlgorithm base class; this is a modern static implementation.
/// FNV32 parameters: prime=0x01000193, offset=0x811C9DC5
/// FNV64 parameters: prime=0x00000100000001B3, offset=0xCBF29CE484222325
/// </remarks>
public static class FnvHash
{
    // FNV-1a 32-bit parameters
    private const uint Fnv32Prime = 0x01000193;
    private const uint Fnv32Offset = 0x811C9DC5;

    // FNV-1a 64-bit parameters
    private const ulong Fnv64Prime = 0x00000100000001B3;
    private const ulong Fnv64Offset = 0xCBF29CE484222325;

    /// <summary>
    /// Computes FNV-1a 32-bit hash of a string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Fnv32(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Fnv32(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Computes FNV-1a 32-bit hash of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Fnv32(ReadOnlySpan<byte> data)
    {
        uint hash = Fnv32Offset;

        foreach (byte b in data)
        {
            hash ^= b;
            hash *= Fnv32Prime;
        }

        return hash;
    }

    /// <summary>
    /// Computes FNV-1a 64-bit hash of a string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Fnv64(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Fnv64(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Computes FNV-1a 64-bit hash of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Fnv64(ReadOnlySpan<byte> data)
    {
        ulong hash = Fnv64Offset;

        foreach (byte b in data)
        {
            hash ^= b;
            hash *= Fnv64Prime;
        }

        return hash;
    }

    /// <summary>
    /// Computes FNV-1a 32-bit hash of a lowercase string.
    /// Used for case-insensitive hashing.
    /// </summary>
    /// <param name="value">The string to hash (will be lowercased).</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Fnv32Lower(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Fnv32(value.ToLowerInvariant());
    }

    /// <summary>
    /// Computes FNV-1a 64-bit hash of a lowercase string.
    /// Used for case-insensitive hashing.
    /// </summary>
    /// <param name="value">The string to hash (will be lowercased).</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Fnv64Lower(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Fnv64(value.ToLowerInvariant());
    }

    /// <summary>
    /// Computes FNV-24 hash (used for CLIP hashes in TS4).
    /// This is FNV-32 masked to 24 bits.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 24-bit hash value.</returns>
    public static uint Fnv24(string value)
    {
        return Fnv32Lower(value) & 0x00FFFFFF;
    }
}
