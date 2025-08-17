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

using System.Text;

namespace TS4Tools.Core.System.Text;

/// <summary>
/// Read and write a seven-bit encoded length-prefixed string in a given <see cref="Encoding"/> from or to a <see cref="Stream"/>.
/// </summary>
public static class SevenBitString
{
    /// <summary>
    /// Read a string from <see cref="Stream"/> <paramref name="stream"/> using <see cref="Encoding"/> <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> from which to read string.</param>
    /// <param name="encoding"><see cref="Encoding"/> to use when reading.</param>
    /// <returns>A <see cref="string"/> value.</returns>
    public static string Read(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        using var reader = new BinaryReader(stream, encoding, leaveOpen: true);
        return reader.ReadString();
    }

    /// <summary>
    /// Write a string to <see cref="Stream"/> <paramref name="stream"/> using <see cref="Encoding"/> <paramref name="encoding"/>.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to which to write string.</param>
    /// <param name="encoding"><see cref="Encoding"/> to use when writing.</param>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public static void Write(Stream stream, Encoding encoding, string? value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        var bytes = encoding.GetBytes(value ?? string.Empty);
        using var writer = new BinaryWriter(stream, encoding, leaveOpen: true);

        // Write seven-bit encoded length
        Write7BitEncodedInt(writer, bytes.Length);

        // Write the string bytes
        writer.Write(bytes);
    }

    /// <summary>
    /// Write a string to <see cref="Stream"/> using <see cref="Encoding"/> with modern span-based approach.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to which to write string.</param>
    /// <param name="encoding"><see cref="Encoding"/> to use when writing.</param>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public static void WriteSpan(Stream stream, Encoding encoding, ReadOnlySpan<char> value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        var maxBytes = encoding.GetMaxByteCount(value.Length);
        Span<byte> buffer = maxBytes <= 256 ? stackalloc byte[maxBytes] : new byte[maxBytes];

        var actualBytes = encoding.GetBytes(value, buffer);
        var bytes = buffer[..actualBytes];

        using var writer = new BinaryWriter(stream, encoding, leaveOpen: true);

        // Write seven-bit encoded length
        Write7BitEncodedInt(writer, bytes.Length);

        // Write the string bytes
        writer.Write(bytes);
    }

    private static void Write7BitEncodedInt(BinaryWriter writer, int value)
    {
        // Write seven-bit encoded integer
        var v = (uint)value;
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }
        writer.Write((byte)v);
    }
}

/// <summary>
/// Read and write a seven-bit encoded length-prefixed string in <see cref="Encoding.BigEndianUnicode"/> from or to a <see cref="Stream"/>.
/// </summary>
public static class BigEndianUnicodeString
{
    /// <summary>
    /// Read a string from <see cref="Stream"/> <paramref name="stream"/> using <see cref="Encoding.BigEndianUnicode"/>.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> from which to read string.</param>
    /// <returns>A <see cref="string"/> value.</returns>
    public static string Read(Stream stream)
    {
        return SevenBitString.Read(stream, Encoding.BigEndianUnicode);
    }

    /// <summary>
    /// Write a string to <see cref="Stream"/> <paramref name="stream"/> using <see cref="Encoding.BigEndianUnicode"/>.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to which to write string.</param>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public static void Write(Stream stream, string? value)
    {
        SevenBitString.Write(stream, Encoding.BigEndianUnicode, value);
    }

    /// <summary>
    /// Write a string to <see cref="Stream"/> using <see cref="Encoding.BigEndianUnicode"/> with modern span-based approach.
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to which to write string.</param>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public static void WriteSpan(Stream stream, ReadOnlySpan<char> value)
    {
        SevenBitString.WriteSpan(stream, Encoding.BigEndianUnicode, value);
    }
}
