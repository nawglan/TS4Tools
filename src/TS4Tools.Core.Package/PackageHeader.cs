/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Core.Package;

/// <summary>
/// Represents a DBPF package header with modern .NET features
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PackageHeader
{
    /// <summary>
    /// The size of the header in bytes
    /// </summary>
    public const int HeaderSize = 96;

    /// <summary>
    /// Expected magic bytes for DBPF packages
    /// </summary>
    public static ReadOnlySpan<byte> ExpectedMagic => "DBPF"u8;

    /// <summary>
    /// Package magic bytes ("DBPF")
    /// </summary>
    public ReadOnlySpan<byte> Magic => new ReadOnlySpan<byte>(_magicBytes);
    private readonly byte[] _magicBytes;

    /// <summary>
    /// Major version (typically 2 for TS4)
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Minor version (typically 0 for TS4)
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// User version major
    /// </summary>
    public int UserVersionMajor { get; }

    /// <summary>
    /// User version minor
    /// </summary>
    public int UserVersionMinor { get; }

    /// <summary>
    /// Unused field 1
    /// </summary>
    public int Unused1 { get; }

    /// <summary>
    /// Creation date as Unix timestamp
    /// </summary>
    public uint CreatedDateRaw { get; }

    /// <summary>
    /// Modified date as Unix timestamp
    /// </summary>
    public uint ModifiedDateRaw { get; }

    /// <summary>
    /// Index major version
    /// </summary>
    public int IndexMajor { get; }

    /// <summary>
    /// Number of resources in the package
    /// </summary>
    public int ResourceCount { get; }

    /// <summary>
    /// Position of the resource index in the file
    /// </summary>
    public int IndexPosition { get; }

    /// <summary>
    /// Size of the resource index in bytes
    /// </summary>
    public int IndexSize { get; }

    /// <summary>
    /// Unused field 2
    /// </summary>
    public int Unused2 { get; }

    /// <summary>
    /// Unused field 3
    /// </summary>
    public int Unused3 { get; }

    /// <summary>
    /// Index minor version
    /// </summary>
    public int IndexMinor { get; }

    /// <summary>
    /// Position of the hole index in the file
    /// </summary>
    public int HoleIndexPosition { get; }

    /// <summary>
    /// Size of the hole index in bytes
    /// </summary>
    public int HoleIndexSize { get; }

    /// <summary>
    /// Number of holes in the package
    /// </summary>
    public int HoleCount { get; }

    /// <summary>
    /// Unused field 4
    /// </summary>
    public int Unused4 { get; }

    /// <summary>
    /// Unused field 5
    /// </summary>
    public int Unused5 { get; }

    /// <summary>
    /// Unused field 6
    /// </summary>
    public int Unused6 { get; }

    /// <summary>
    /// Creation date as DateTime
    /// </summary>
    public DateTime CreatedDate => DateTimeOffset.FromUnixTimeSeconds(CreatedDateRaw).DateTime;

    /// <summary>
    /// Modified date as DateTime
    /// </summary>
    public DateTime ModifiedDate => DateTimeOffset.FromUnixTimeSeconds(ModifiedDateRaw).DateTime;

    /// <summary>
    /// Check if the header has valid magic bytes
    /// </summary>
    public bool IsValid => Magic.SequenceEqual(ExpectedMagic);

    /// <summary>
    /// Creates a new package header
    /// </summary>
    public PackageHeader(
        ReadOnlySpan<byte> magic,
        int major = 2,
        int minor = 0,
        int userVersionMajor = 0,
        int userVersionMinor = 0,
        int unused1 = 0,
        uint createdDate = 0,
        uint modifiedDate = 0,
        int indexMajor = 7,
        int resourceCount = 0,
        int indexPosition = HeaderSize,
        int indexSize = 0,
        int unused2 = 0,
        int unused3 = 0,
        int indexMinor = 1,
        int holeIndexPosition = 0,
        int holeIndexSize = 0,
        int holeCount = 0,
        int unused4 = 0,
        int unused5 = 0,
        int unused6 = 0)
    {
        _magicBytes = magic.ToArray();
        Major = major;
        Minor = minor;
        UserVersionMajor = userVersionMajor;
        UserVersionMinor = userVersionMinor;
        Unused1 = unused1;
        CreatedDateRaw = createdDate;
        ModifiedDateRaw = modifiedDate;
        IndexMajor = indexMajor;
        ResourceCount = resourceCount;
        IndexPosition = indexPosition;
        IndexSize = indexSize;
        Unused2 = unused2;
        Unused3 = unused3;
        IndexMinor = indexMinor;
        HoleIndexPosition = holeIndexPosition;
        HoleIndexSize = holeIndexSize;
        HoleCount = holeCount;
        Unused4 = unused4;
        Unused5 = unused5;
        Unused6 = unused6;
    }

    /// <summary>
    /// Creates a default TS4 package header
    /// </summary>
    /// <returns>Default header for TS4 packages</returns>
    public static PackageHeader CreateDefault()
    {
        var now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new PackageHeader(
            ExpectedMagic,
            createdDate: now,
            modifiedDate: now);
    }

    /// <summary>
    /// Read header from a binary reader
    /// </summary>
    /// <param name="reader">Binary reader</param>
    /// <returns>Package header</returns>
    public static PackageHeader Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var magic = reader.ReadBytes(4);
        var major = reader.ReadInt32();
        var minor = reader.ReadInt32();
        var userVersionMajor = reader.ReadInt32();
        var userVersionMinor = reader.ReadInt32();
        var unused1 = reader.ReadInt32();
        var createdDate = reader.ReadUInt32();
        var modifiedDate = reader.ReadUInt32();
        var indexMajor = reader.ReadInt32();
        var resourceCount = reader.ReadInt32();
        var indexPosition = reader.ReadInt32();
        var indexSize = reader.ReadInt32();
        var unused2 = reader.ReadInt32();
        var unused3 = reader.ReadInt32();
        var indexMinor = reader.ReadInt32();
        var holeIndexPosition = reader.ReadInt32();
        var holeIndexSize = reader.ReadInt32();
        var holeCount = reader.ReadInt32();
        var unused4 = reader.ReadInt32();
        var unused5 = reader.ReadInt32();
        var unused6 = reader.ReadInt32();

        // Skip remaining bytes to reach HeaderSize
        var remainingBytes = HeaderSize - (int)reader.BaseStream.Position;
        if (remainingBytes > 0)
        {
            reader.ReadBytes(remainingBytes);
        }

        return new PackageHeader(
            magic,
            major,
            minor,
            userVersionMajor,
            userVersionMinor,
            unused1,
            createdDate,
            modifiedDate,
            indexMajor,
            resourceCount,
            indexPosition,
            indexSize,
            unused2,
            unused3,
            indexMinor,
            holeIndexPosition,
            holeIndexSize,
            holeCount,
            unused4,
            unused5,
            unused6);
    }

    /// <summary>
    /// Write header to a binary writer
    /// </summary>
    /// <param name="writer">Binary writer</param>
    public void Write(BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(_magicBytes);
        writer.Write(Major);
        writer.Write(Minor);
        writer.Write(UserVersionMajor);
        writer.Write(UserVersionMinor);
        writer.Write(Unused1);
        writer.Write(CreatedDateRaw);
        writer.Write(ModifiedDateRaw);
        writer.Write(IndexMajor);
        writer.Write(ResourceCount);
        writer.Write(IndexPosition);
        writer.Write(IndexSize);
        writer.Write(Unused2);
        writer.Write(Unused3);
        writer.Write(IndexMinor);
        writer.Write(HoleIndexPosition);
        writer.Write(HoleIndexSize);
        writer.Write(HoleCount);
        writer.Write(Unused4);
        writer.Write(Unused5);
        writer.Write(Unused6);

        // Pad to HeaderSize
        var currentPos = writer.BaseStream.Position;
        var paddingNeeded = HeaderSize - currentPos;
        if (paddingNeeded > 0)
        {
            writer.Write(new byte[paddingNeeded]);
        }
    }
}
