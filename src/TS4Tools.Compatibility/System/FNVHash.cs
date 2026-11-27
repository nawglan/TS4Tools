// Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs lines 24-273

namespace System.Security.Cryptography;

/// <summary>
/// Fowler-Noll-Vo (FNV-1) hash implementation, as used by s4pi.
/// </summary>
/// <remarks>
/// FNV-1: multiply then XOR (Source: FNVHash.cs lines 63-66). Strings are hashed as the ASCII bytes of
/// their lower-invariant form (line 50). As in legacy, <see cref="Initialize"/> does not reset the
/// running hash, so an instance should be used for a single computation.
/// </remarks>
public abstract class FNVHash : HashAlgorithm
{
    private readonly ulong prime;

    /// <summary>
    /// Hash value accumulator.
    /// </summary>
    protected ulong hash;

    /// <summary>
    /// Initialise the hash algorithm.
    /// </summary>
    /// <param name="prime">The FNV prime.</param>
    /// <param name="offset">The FNV offset basis.</param>
    protected FNVHash(ulong prime, ulong offset)
    {
        this.prime = prime;
        hash = offset;
    }

    /// <summary>
    /// Compute the hash of the lower-cased ASCII bytes of <paramref name="value"/>.
    /// </summary>
    /// <param name="value">String to hash.</param>
    /// <returns>The hash bytes.</returns>
    public byte[] ComputeHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ComputeHash(Encoding.ASCII.GetBytes(value.ToLowerInvariant()));
    }

    /// <summary>
    /// Does nothing (legacy behaviour; the accumulator is not reset).
    /// </summary>
    public override void Initialize() { }

    /// <inheritdoc />
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        for (int i = ibStart; i < ibStart + cbSize; i++)
        {
            hash *= prime;
            hash ^= array[i];
        }
    }

    /// <inheritdoc />
    protected override byte[] HashFinal()
    {
        HashValue = BitConverter.GetBytes(hash);
        return HashValue;
    }
}

/// <summary>
/// FNV32 hash routine.
/// </summary>
public class FNV32 : FNVHash
{
    /// <summary>Initialise the 32-bit hash algorithm.</summary>
    public FNV32() : base(0x01000193, 0x811C9DC5) { }

    /// <summary>Gets the value of the computed hash code.</summary>
    public override byte[] Hash => BitConverter.GetBytes((uint)hash);

    /// <summary>Gets the size, in bits, of the computed hash code.</summary>
    public override int HashSize => 32;

    /// <summary>Get the FNV32 hash for a string of text.</summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>The hash value.</returns>
    public static uint GetHash(string text)
    {
        using var h = new FNV32();
        return BitConverter.ToUInt32(h.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV24 hash routine (FNV32 folded to 24 bits).
/// </summary>
public class FNV24 : FNV32
{
    private const uint FNV24Mask = 0xFFFFFF;

    /// <summary>Gets the value of the computed hash code.</summary>
    public override byte[] Hash => BitConverter.GetBytes(ConvertTo24BitFromUInt32((uint)hash));

    /// <summary>Get the FNV24 hash for a string of text.</summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>The hash value.</returns>
    /// <remarks>Source: FNVHash.cs line 115</remarks>
    public static new uint GetHash(string text)
    {
        using var h = new FNV24();
        var value = BitConverter.ToUInt32(h.ComputeHash(text), 0);
        return ConvertTo24BitFromUInt32(value);
    }

    // Source: FNVHash.cs line 117
    private static uint ConvertTo24BitFromUInt32(uint value) => (value >> 24) ^ (value & FNV24Mask);
}

/// <summary>
/// FNV64 hash routine.
/// </summary>
public class FNV64 : FNVHash
{
    /// <summary>Initialise the 64-bit hash algorithm.</summary>
    public FNV64() : base(0x00000100000001B3, 0xCBF29CE484222325) { }

    /// <summary>Gets the value of the computed hash code.</summary>
    public override byte[] Hash => BitConverter.GetBytes(hash);

    /// <summary>Gets the size, in bits, of the computed hash code.</summary>
    public override int HashSize => 64;

    /// <summary>Get the FNV64 hash for a string of text.</summary>
    /// <param name="text">The text to hash.</param>
    /// <returns>The hash value.</returns>
    public static ulong GetHash(string text)
    {
        using var h = new FNV64();
        return BitConverter.ToUInt64(h.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV64 hash routine for CLIP resource names (age/species prefix masking).
/// </summary>
/// <remarks>Source: FNVHash.cs lines 149-272</remarks>
public class FNV64CLIP : FNV64
{
    private static readonly string[] ao = ["a", "o"];

    /// <summary>Initialise the hash algorithm.</summary>
    public FNV64CLIP() { }

    /// <summary>Get the FNV64 hash of a CLIP name, with the age/species mask in the high bits.</summary>
    /// <param name="text">The CLIP name.</param>
    /// <returns>The hash value.</returns>
    public static new ulong GetHash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string value = text;
        ulong mask = 0;

        string[] split = text.Split('_', 2);
        if (split.Length > 1 && split[0].Length <= 5)
        {
            string[] x2y = split[0].Split('2', 2);
            if (x2y[0].Length > 0 && x2y[0].Length <= 2)
            {
                byte xAge = Mask(x2y[0]);

                if (x2y.Length > 1 && x2y[1].Length > 0 && x2y[1].Length <= 2)
                {
                    if (!(ao.Contains(x2y[0]) && ao.Contains(x2y[1])))
                    {
                        byte yAge = Mask(x2y[1]);
                        value = string.Join("_", (x2y[0][0] == 'o' ? "o" : "a") + "2" + (x2y[1][0] == 'o' ? "o" : "a"), split[1]);
                        mask = (ulong)(0x8000 | xAge << 8 | yAge);
                    }
                }
                else if (!ao.Contains(x2y[0]))
                {
                    value = string.Join("_", "a", split[1]);
                    mask = (ulong)(0x8000 | xAge << 8);
                }
            }
        }

        ulong h = FNV64.GetHash(value);
        h &= 0x7FFFFFFFFFFFFFFF;
        h ^= mask << 48;
        return h;
    }

    /// <summary>Get the FNV64 hash of the generic (age-neutral) form of a CLIP name.</summary>
    /// <param name="text">The CLIP name.</param>
    /// <returns>The hash value.</returns>
    public static ulong GetHashGeneric(string text)
    {
        ulong h = FNV64.GetHash(GetGenericValue(text));
        h &= 0x7FFFFFFFFFFFFFFF;
        return h;
    }

    /// <summary>Get the generic (age-neutral) form of a CLIP name.</summary>
    /// <param name="text">The CLIP name.</param>
    /// <returns>The generic name.</returns>
    public static string GetGenericValue(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string value = text;

        string[] split = text.Split('_', 2);
        if (split.Length > 1 && split[0].Length <= 5)
        {
            string[] x2y = split[0].Split('2', 2);
            if (x2y[0].Length > 0 && x2y[0].Length <= 2)
            {
                if (x2y.Length > 1 && x2y[1].Length > 0 && x2y[1].Length <= 2)
                {
                    if (!(ao.Contains(x2y[0]) && ao.Contains(x2y[1])))
                        value = string.Join("_", (x2y[0][0] == 'o' ? "o" : "a") + "2" + (x2y[1][0] == 'o' ? "o" : "a"), split[1]);
                }
                else if (!ao.Contains(x2y[0]))
                {
                    value = string.Join("_", "a", split[1]);
                }
            }
        }

        return value;
    }

    // Source: FNVHash.cs lines 250-271
    private static byte Mask(string actor) => actor switch
    {
        "b" => 0x01,
        "p" => 0x02,
        "c" => 0x03,
        "t" => 0x04,
        "h" => 0x05,
        "e" => 0x06,
        "ad" => 0x08,
        "cd" => 0x09,
        "al" => 0x0A,
        "ac" => 0x0D,
        "cc" => 0x0E,
        "ah" => 0x10,
        "ch" => 0x11,
        "ab" => 0x12,
        "ar" => 0x13,
        _ => 0x00,
    };
}
