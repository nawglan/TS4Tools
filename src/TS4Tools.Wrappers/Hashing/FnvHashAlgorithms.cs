// Source: legacy_references/Sims4Tools/CS System Classes/FNVHash.cs lines 28-273

namespace TS4Tools.Wrappers.Hashing;

/// <summary>
/// Base class for the legacy FNV-1 (multiply, then XOR) hash algorithms, implemented as a
/// <see cref="System.Security.Cryptography.HashAlgorithm"/>.
/// </summary>
/// <remarks>
/// Source: FNVHash.cs lines 28-65. NOTE: the legacy algorithm is FNV-1 (<c>hash *= prime; hash ^= byte</c>) and
/// the string overload lower-cases the text and encodes it as ASCII. This is NOT what the static
/// <see cref="FnvHash"/> helpers compute (they implement FNV-1a over UTF-8). Resource instance IDs and names in
/// Sims 4 packages use the legacy FNV-1 form, so use these classes when matching s4pi output.
/// One deviation: legacy <c>Initialize()</c> is a no-op so a reused instance keeps accumulating; here
/// <see cref="Initialize"/> resets the state so an instance can safely compute several hashes.
/// </remarks>
public abstract class FnvHashAlgorithm : System.Security.Cryptography.HashAlgorithm
{
    private readonly ulong _prime;
    private readonly ulong _offset;

    /// <summary>
    /// The running hash. Needs casting to the appropriate size by concrete classes.
    /// </summary>
    protected ulong HashState { get; set; }

    /// <summary>
    /// The state captured by the last <see cref="HashFinal"/>. .NET calls <see cref="Initialize"/> straight after a
    /// ComputeHash (the legacy .NET Framework did not), so <c>Hash</c> must not read the live state.
    /// </summary>
    protected ulong FinalState { get; private set; }

    /// <summary>True once a hash has been finalised.</summary>
    protected bool IsFinal { get; private set; }

    /// <summary>
    /// Initialises the algorithm with its prime and offset basis.
    /// </summary>
    /// <param name="prime">Algorithm-specific prime.</param>
    /// <param name="offset">Algorithm-specific offset basis.</param>
    protected FnvHashAlgorithm(ulong prime, ulong offset)
    {
        _prime = prime;
        _offset = offset;
        HashState = offset;
    }

    /// <summary>
    /// Hashes a string: the text is lower-cased (invariant) and encoded as ASCII, as legacy does.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The hash bytes (8 bytes, the state as a little-endian ulong, as legacy returns).</returns>
    public byte[] ComputeHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ComputeHash(Encoding.ASCII.GetBytes(value.ToLowerInvariant()));
    }

    /// <summary>Resets the hash state to the offset basis.</summary>
    public override void Initialize() => HashState = _offset;

    /// <inheritdoc/>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        for (int i = ibStart; i < ibStart + cbSize; i++)
        {
            HashState *= _prime;
            HashState ^= array[i];
        }
    }

    /// <inheritdoc/>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        foreach (byte b in source)
        {
            HashState *= _prime;
            HashState ^= b;
        }
    }

    /// <inheritdoc/>
    protected override byte[] HashFinal()
    {
        FinalState = HashState;
        IsFinal = true;
        HashValue = BitConverter.GetBytes(HashState);
        return HashValue;
    }
}

/// <summary>
/// FNV32 hash routine (FNV-1, prime 0x01000193, offset 0x811C9DC5).
/// </summary>
/// <remarks>Source: FNVHash.cs lines 67-90.</remarks>
public class FNV32 : FnvHashAlgorithm
{
    /// <summary>Initialises the hash algorithm.</summary>
    public FNV32() : base(0x01000193, 0x811C9DC5)
    {
    }

    /// <summary>Gets the value of the computed hash code (4 bytes).</summary>
    public override byte[]? Hash => IsFinal ? BitConverter.GetBytes((uint)FinalState) : base.Hash;

    /// <summary>Gets the size, in bits, of the computed hash code.</summary>
    public override int HashSize => 32;

    /// <summary>
    /// Gets the FNV32 hash for a string of text (lower-cased ASCII).
    /// </summary>
    /// <param name="text">The text to get the hash for.</param>
    /// <returns>The hash value.</returns>
    public static uint GetHash(string text)
    {
        using var algorithm = new FNV32();
        return BitConverter.ToUInt32(algorithm.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV24 hash routine: the FNV32 value folded to 24 bits as <c>(hash >> 24) ^ (hash &amp; 0xFFFFFF)</c>.
/// </summary>
/// <remarks>Source: FNVHash.cs lines 92-110. Note this differs from the plain mask used by <see cref="FnvHash.Fnv24(string)"/>.</remarks>
public class FNV24 : FNV32
{
    private const uint Fnv24Mask = 0xFFFFFF;

    /// <summary>Gets the value of the computed hash code (4 bytes, 24 significant bits).</summary>
    public override byte[]? Hash => IsFinal ? BitConverter.GetBytes(FoldTo24Bits((uint)FinalState)) : base.Hash;

    /// <summary>
    /// Gets the FNV24 hash for a string of text.
    /// </summary>
    /// <param name="text">The text to get the hash for.</param>
    /// <returns>The hash value.</returns>
    public static new uint GetHash(string text)
    {
        using var algorithm = new FNV24();
        return FoldTo24Bits(BitConverter.ToUInt32(algorithm.ComputeHash(text), 0));
    }

    private static uint FoldTo24Bits(uint hash) => (hash >> 24) ^ (hash & Fnv24Mask);
}

/// <summary>
/// FNV64 hash routine (FNV-1, prime 0x00000100000001B3, offset 0xCBF29CE484222325).
/// </summary>
/// <remarks>Source: FNVHash.cs lines 112-134.</remarks>
public class FNV64 : FnvHashAlgorithm
{
    /// <summary>Initialises the hash algorithm.</summary>
    public FNV64() : base(0x00000100000001B3, 0xCBF29CE484222325)
    {
    }

    /// <summary>Gets the value of the computed hash code (8 bytes).</summary>
    public override byte[]? Hash => IsFinal ? BitConverter.GetBytes(FinalState) : base.Hash;

    /// <summary>Gets the size, in bits, of the computed hash code.</summary>
    public override int HashSize => 64;

    /// <summary>
    /// Gets the FNV64 hash for a string of text (lower-cased ASCII).
    /// </summary>
    /// <param name="text">The text to get the hash for.</param>
    /// <returns>The hash value.</returns>
    public static ulong GetHash(string text)
    {
        using var algorithm = new FNV64();
        return BitConverter.ToUInt64(algorithm.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV64CLIP hash routine: the FNV64 hash used as the instance ID of a CLIP resource, with the actor
/// age/species prefix (for example <c>a2o_</c>, <c>ad2ad_</c>) folded into the top bits.
/// </summary>
/// <remarks>Source: FNVHash.cs lines 137-273.</remarks>
public class FNV64CLIP : FNV64
{
    private static readonly string[] AgeOrOther = ["a", "o"];

    /// <summary>Initialises the hash algorithm.</summary>
    public FNV64CLIP()
    {
    }

    /// <summary>
    /// Gets the FNV64 hash for use as the IID for a CLIP of a given name.
    /// </summary>
    /// <param name="text">The CLIP name to get the hash for.</param>
    /// <returns>The hash value.</returns>
    /// <remarks>Source: FNVHash.cs lines 153-196 (GetHash).</remarks>
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
                    if (!(AgeOrOther.Contains(x2y[0]) && AgeOrOther.Contains(x2y[1])))
                    {
                        byte yAge = Mask(x2y[1]);
                        value = string.Join("_", [(x2y[0][0] == 'o' ? "o" : "a") + "2" + (x2y[1][0] == 'o' ? "o" : "a"), split[1]]);
                        mask = (ulong)(0x8000 | (xAge << 8) | yAge);
                    }
                }
                else if (!AgeOrOther.Contains(x2y[0]))
                {
                    value = string.Join("_", ["a", split[1]]);
                    mask = (ulong)(0x8000 | (xAge << 8));
                }
            }
        }

        ulong hash = FNV64.GetHash(value);
        hash &= 0x7FFFFFFFFFFFFFFF;
        hash ^= mask << 48;

        return hash;
    }

    /// <summary>
    /// Gets the FNV64 hash for use as the IID for a CLIP but ignoring age and species.
    /// </summary>
    /// <param name="text">The CLIP name to get the generic hash for.</param>
    /// <returns>The generic hash value.</returns>
    /// <remarks>Source: FNVHash.cs lines 198-211 (GetHashGeneric).</remarks>
    public static ulong GetHashGeneric(string text)
    {
        string value = GetGenericValue(text);

        ulong hash = FNV64.GetHash(value);
        hash &= 0x7FFFFFFFFFFFFFFF;

        return hash;
    }

    /// <summary>
    /// Gets the "generic" CLIP name, removing age and species.
    /// </summary>
    /// <param name="text">The CLIP name from which to get the generic value.</param>
    /// <returns>The generic CLIP name.</returns>
    /// <remarks>Source: FNVHash.cs lines 213-239 (GetGenericValue).</remarks>
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
                    if (!(AgeOrOther.Contains(x2y[0]) && AgeOrOther.Contains(x2y[1])))
                    {
                        value = string.Join("_", [(x2y[0][0] == 'o' ? "o" : "a") + "2" + (x2y[1][0] == 'o' ? "o" : "a"), split[1]]);
                    }
                }
                else if (!AgeOrOther.Contains(x2y[0]))
                {
                    value = string.Join("_", ["a", split[1]]);
                }
            }
        }

        return value;
    }

    /// <summary>Source: FNVHash.cs lines 243-271 (Mask).</summary>
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
