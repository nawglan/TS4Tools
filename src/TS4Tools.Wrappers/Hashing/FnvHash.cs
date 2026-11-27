// Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs

namespace TS4Tools.Wrappers.Hashing;

/// <summary>
/// Fowler-Noll-Vo hash functions used by The Sims 4 for string hashing.
/// Implements the legacy s4pi FNV-1 variant (multiply, then XOR) for 32/64/24-bit hashes.
/// </summary>
/// <remarks>
/// Source: CS System Classes/FNVHash.cs
/// - HashCore (line 65): <c>hash *= prime; hash ^= b;</c> — FNV-1, NOT FNV-1.
/// - ComputeHash(string) (line 50): ASCII bytes of <c>value.ToLowerInvariant()</c>, so every
///   string overload here is case-insensitive, exactly like legacy.
/// - FNV24: 32-bit hash folded <c>(h &gt;&gt; 24) ^ (h &amp; 0xFFFFFF)</c>, see <see cref="FNV24"/>.
/// This static API delegates to the faithful <see cref="FNV32"/>/<see cref="FNV64"/>/<see cref="FNV24"/> classes.
/// FNV32 parameters: prime=0x01000193, offset=0x811C9DC5
/// FNV64 parameters: prime=0x00000100000001B3, offset=0xCBF29CE484222325
/// </remarks>
public static class FnvHash
{
    // FNV 32-bit parameters
    private const uint Fnv32Prime = 0x01000193;
    private const uint Fnv32Offset = 0x811C9DC5;

    // FNV 64-bit parameters
    private const ulong Fnv64Prime = 0x00000100000001B3;
    private const ulong Fnv64Offset = 0xCBF29CE484222325;

    /// <summary>
    /// Computes FNV-1 32-bit hash of a string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Fnv32(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FNV32.GetHash(value);
    }

    /// <summary>
    /// Computes FNV-1 32-bit hash of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Fnv32(ReadOnlySpan<byte> data)
    {
        uint hash = Fnv32Offset;

        foreach (byte b in data)
        {
            hash *= Fnv32Prime;
            hash ^= b;
        }

        return hash;
    }

    /// <summary>
    /// Computes FNV-1 64-bit hash of a string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Fnv64(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FNV64.GetHash(value);
    }

    /// <summary>
    /// Computes FNV-1 64-bit hash of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Fnv64(ReadOnlySpan<byte> data)
    {
        ulong hash = Fnv64Offset;

        foreach (byte b in data)
        {
            hash *= Fnv64Prime;
            hash ^= b;
        }

        return hash;
    }

    /// <summary>
    /// Computes FNV-1 32-bit hash of a lowercase string.
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
    /// Computes FNV-1 64-bit hash of a lowercase string.
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
    /// Computes the legacy FNV-24 hash (FNV-32 with the high byte folded into the low 24 bits).
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The 24-bit hash value.</returns>
    public static uint Fnv24(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FNV24.GetHash(value);
    }
}
