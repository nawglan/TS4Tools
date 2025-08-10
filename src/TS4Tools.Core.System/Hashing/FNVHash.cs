/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System.Security.Cryptography;
using System.Text;

namespace TS4Tools.Core.System.Hashing;

/// <summary>
/// Base class implementing <see cref="HashAlgorithm"/> for FNV hash algorithms.
/// For full documentation, refer to http://www.sims2wiki.info/wiki.php?title=FNV
/// </summary>
public abstract class FnvHash : HashAlgorithm
{
    private readonly ulong prime;
    private readonly ulong offset;

    /// <summary>
    /// Algorithm result, needs casting to appropriate size by concrete classes
    /// </summary>
    private ulong hash;

    /// <summary>
    /// Gets the current hash value for derived classes
    /// </summary>
    protected new ulong HashValue => hash;

    /// <summary>
    /// Initialise the hash algorithm
    /// </summary>
    /// <param name="prime">algorithm-specific value</param>
    /// <param name="offset">algorithm-specific value</param>
    protected FnvHash(ulong prime, ulong offset)
    {
        this.prime = prime;
        this.offset = offset;
        hash = offset;
    }

    /// <summary>
    /// Method for hashing a string
    /// </summary>
    /// <param name="value">string</param>
    /// <returns>FNV hash of string</returns>
    public byte[] ComputeHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ComputeHash(Encoding.ASCII.GetBytes(value.ToLowerInvariant()));
    }

    /// <summary>
    /// Nothing to initialize
    /// </summary>
    public override void Initialize()
    {
        hash = offset;
    }

    /// <summary>
    /// Implements the algorithm
    /// </summary>
    /// <param name="array">The input to compute the hash code for.</param>
    /// <param name="ibStart">The offset into the byte array from which to begin using data.</param>
    /// <param name="cbSize">The number of bytes in the byte array to use as data.</param>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ArgumentNullException.ThrowIfNull(array);

        for (int i = ibStart; i < ibStart + cbSize; i++)
        {
            hash *= prime;
            hash ^= array[i];
        }
    }

    /// <summary>
    /// Implements the algorithm for spans (modern approach)
    /// </summary>
    /// <param name="source">The data to process</param>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        foreach (var b in source)
        {
            hash *= prime;
            hash ^= b;
        }
    }

    /// <summary>
    /// Returns the computed hash code.
    /// </summary>
    /// <returns>The computed hash code.</returns>
    protected override byte[] HashFinal()
    {
        var hashBytes = BitConverter.GetBytes(hash);
        return hashBytes;
    }
}

/// <summary>
/// FNV32 hash routine
/// </summary>
public class Fnv32 : FnvHash
{
    /// <summary>
    /// Initialise the hash algorithm
    /// </summary>
    public Fnv32() : base(0x01000193u, 0x811C9DC5u) { }

    /// <summary>
    /// Gets the value of the computed hash code.
    /// </summary>
    public override byte[] Hash => BitConverter.GetBytes((uint)HashValue);

    /// <summary>
    /// Gets the size, in bits, of the computed hash code.
    /// </summary>
    public override int HashSize => 32;

    /// <summary>
    /// Get the FNV32 hash for a string of text
    /// </summary>
    /// <param name="text">the text to get the hash for</param>
    /// <returns>the hash value</returns>
    public static uint GetHash(string text)
    {
        using var fnv = new Fnv32();
        return BitConverter.ToUInt32(fnv.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV24 hash routine
/// </summary>
public sealed class Fnv24 : Fnv32
{
    private const uint FNV24Mask = 0xFFFFFF;

    /// <summary>
    /// Gets the value of the computed hash code.
    /// </summary>
    public override byte[] Hash => BitConverter.GetBytes(ConvertTo24BitFromUInt32((uint)HashValue));

    /// <summary>
    /// Get the FNV24 hash for a string of text
    /// </summary>
    /// <param name="text">the text to get the hash for</param>
    /// <returns>the hash value</returns>
    public static new uint GetHash(string text)
    {
        using var fnv = new Fnv24();
        var hash = BitConverter.ToUInt32(fnv.ComputeHash(text), 0);
        return ConvertTo24BitFromUInt32(hash);
    }

    private static uint ConvertTo24BitFromUInt32(uint hash) => (hash >> 24) ^ (hash & FNV24Mask);
}

/// <summary>
/// FNV64 hash routine
/// </summary>
public class Fnv64 : FnvHash
{
    /// <summary>
    /// Initialise the hash algorithm
    /// </summary>
    public Fnv64() : base(0x00000100000001B3ul, 0xCBF29CE484222325ul) { }

    /// <summary>
    /// Gets the value of the computed hash code.
    /// </summary>
    public override byte[] Hash => BitConverter.GetBytes(HashValue);

    /// <summary>
    /// Gets the size, in bits, of the computed hash code.
    /// </summary>
    public override int HashSize => 64;

    /// <summary>
    /// Get the FNV64 hash for a string of text
    /// </summary>
    /// <param name="text">the text to get the hash for</param>
    /// <returns>the hash value</returns>
    public static ulong GetHash(string text)
    {
        using var fnv = new Fnv64();
        return BitConverter.ToUInt64(fnv.ComputeHash(text), 0);
    }
}

/// <summary>
/// FNV64CLIP hash routine
/// </summary>
public sealed class Fnv64Clip : Fnv64
{
    private static readonly string[] AgeOnlyValues = ["a", "o"];

    /// <summary>
    /// Get the FNV64 hash for use as the IID for a CLIP of a given name.
    /// </summary>
    /// <param name="text">the CLIP name to get the hash for</param>
    /// <returns>the hash value</returns>
    public static new ulong GetHash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var value = text;
        ulong mask = 0;

        var split = text.Split('_', 2);
        if (split.Length > 1 && split[0].Length <= 5)
        {
            var x2y = split[0].Split('2', 2);
            if (x2y[0].Length is > 0 and <= 2)
            {
                var xAge = GetAgeMask(x2y[0]);

                if (x2y.Length > 1 && x2y[1].Length is > 0 and <= 2)
                {
                    if (!AgeOnlyValues.Contains(x2y[0]) || !AgeOnlyValues.Contains(x2y[1]))
                    {
                        var yAge = GetAgeMask(x2y[1]);
                        value = $"{(x2y[0][0] == 'o' ? "o" : "a")}2{(x2y[1][0] == 'o' ? "o" : "a")}_{split[1]}";
                        mask = (ulong)(0x8000 | xAge << 8 | yAge);
                    }
                }
                else if (!AgeOnlyValues.Contains(x2y[0]))
                {
                    value = $"a_{split[1]}";
                    mask = (ulong)(0x8000 | xAge << 8);
                }
            }
        }

        var hash = Fnv64.GetHash(value);
        hash &= 0x7FFFFFFFFFFFFFFF;
        hash ^= mask << 48;

        return hash;
    }

    /// <summary>
    /// Get the FNV64 hash for use as the IID for a CLIP but ignoring age and species.
    /// </summary>
    /// <param name="text">The CLIP name to get the generic hash for.</param>
    /// <returns>The generic hash value</returns>
    public static ulong GetHashGeneric(string text)
    {
        var value = GetGenericValue(text);
        var hash = Fnv64.GetHash(value);
        return hash & 0x7FFFFFFFFFFFFFFF;
    }

    /// <summary>
    /// Get the "generic" CLIP, removing age and species.
    /// </summary>
    /// <param name="text">The CLIP name from which to get the generic value.</param>
    /// <returns>The generic CLIP name.</returns>
    public static string GetGenericValue(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var value = text;
        var split = text.Split('_', 2);

        if (split.Length > 1 && split[0].Length <= 5)
        {
            var x2y = split[0].Split('2', 2);
            if (x2y[0].Length is > 0 and <= 2)
            {
                if (x2y.Length > 1 && x2y[1].Length is > 0 and <= 2)
                {
                    if (!AgeOnlyValues.Contains(x2y[0]) || !AgeOnlyValues.Contains(x2y[1]))
                    {
                        value = $"{(x2y[0][0] == 'o' ? "o" : "a")}2{(x2y[1][0] == 'o' ? "o" : "a")}_{split[1]}";
                    }
                }
                else if (!AgeOnlyValues.Contains(x2y[0]))
                {
                    value = $"a_{split[1]}";
                }
            }
        }

        return value;
    }

    private static byte GetAgeMask(string actor) => actor switch
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
        _ => 0x00
    };
}
