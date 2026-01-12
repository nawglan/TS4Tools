// Source: legacy_references/Sims4Tools/s4pi Wrappers/ScriptResource/ScriptResource.cs lines 27-286

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Encrypted Signed Assembly resource containing mod DLL data.
/// Resource Type: 0x073FAA07
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/ScriptResource/ScriptResource.cs
///
/// Format:
/// - version (byte): Format version (1 or 2)
/// - gameVersion (string): Unicode string with length prefix (only if version > 1)
/// - unknown2 (uint32): Constant value 0x2BC4F79F
/// - md5sum (64 bytes): MD5 checksum data
/// - count (uint16): Number of 512-byte blocks
/// - md5table (count * 8 bytes): Encryption table
/// - md5data (count * 512 bytes): Encrypted assembly data
///
/// The encryption algorithm uses XOR with values from md5table, using a seed
/// derived from summing all md5table entries.
/// </remarks>
public sealed class ScriptResource : TypedResource
{
    /// <summary>
    /// Block size for encryption/decryption (512 bytes).
    /// </summary>
    private const int BlockSize = 512;

    /// <summary>
    /// Number of bytes per md5table entry.
    /// </summary>
    private const int Md5TableEntrySize = 8;

    /// <summary>
    /// Default value for Unknown2 field.
    /// </summary>
    public const uint DefaultUnknown2 = 0x2BC4F79F;

    private byte[] _md5Sum = new byte[64];
    private byte[] _md5Table = [];
    private byte[] _clearData = [];

    /// <summary>
    /// Format version (1 or 2).
    /// </summary>
    public byte Version { get; set; } = 1;

    /// <summary>
    /// Game version string (only present if Version > 1).
    /// </summary>
    public string GameVersion { get; set; } = string.Empty;

    /// <summary>
    /// Unknown constant value (typically 0x2BC4F79F).
    /// </summary>
    public uint Unknown2 { get; set; } = DefaultUnknown2;

    /// <summary>
    /// MD5 checksum data (64 bytes).
    /// </summary>
    public ReadOnlySpan<byte> Md5Sum => _md5Sum;

    /// <summary>
    /// The decrypted assembly data.
    /// </summary>
    public ReadOnlyMemory<byte> AssemblyData => _clearData;

    /// <summary>
    /// Gets or sets the assembly data. Setting this marks the resource as changed.
    /// </summary>
    public byte[] Assembly
    {
        get => _clearData;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _clearData = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Creates a new ScriptResource by parsing data.
    /// </summary>
    public ScriptResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: ScriptResource.cs lines 58-72
        int offset = 0;

        // Read version
        if (data.Length < 1)
            throw new ResourceFormatException("ScriptResource data too short for version byte.");
        Version = data[offset++];

        // Read game version if version > 1
        if (Version > 1)
        {
            if (offset + 4 > data.Length)
                throw new ResourceFormatException("ScriptResource data too short for game version length.");

            int gameVersionLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            if (gameVersionLength < 0 || gameVersionLength > 1024)
                throw new ResourceFormatException($"Invalid game version length: {gameVersionLength}");

            int gameVersionBytes = gameVersionLength * 2; // Unicode = 2 bytes per char
            if (offset + gameVersionBytes > data.Length)
                throw new ResourceFormatException("ScriptResource data too short for game version string.");

            var chars = new char[gameVersionLength];
            for (int i = 0; i < gameVersionLength; i++)
            {
                chars[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + i * 2)..]);
            }
            GameVersion = new string(chars);
            offset += gameVersionBytes;
        }
        else
        {
            GameVersion = string.Empty;
        }

        // Read unknown2
        if (offset + 4 > data.Length)
            throw new ResourceFormatException("ScriptResource data too short for unknown2.");
        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read md5sum (64 bytes)
        if (offset + 64 > data.Length)
            throw new ResourceFormatException("ScriptResource data too short for md5sum.");
        _md5Sum = data.Slice(offset, 64).ToArray();
        offset += 64;

        // Read block count
        if (offset + 2 > data.Length)
            throw new ResourceFormatException("ScriptResource data too short for block count.");
        ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        // Note: ushort max (65535) * 512 = ~32MB, which is acceptable for assemblies

        // Read md5table
        int md5TableSize = count * Md5TableEntrySize;
        if (offset + md5TableSize > data.Length)
            throw new ResourceFormatException("ScriptResource data too short for md5table.");
        _md5Table = data.Slice(offset, md5TableSize).ToArray();
        offset += md5TableSize;

        // Read md5data (encrypted)
        int md5DataSize = count * BlockSize;
        if (offset + md5DataSize > data.Length)
            throw new ResourceFormatException("ScriptResource data too short for md5data.");
        var md5Data = data.Slice(offset, md5DataSize);

        // Decrypt the data
        _clearData = Decrypt(_md5Table, md5Data);
    }

    /// <summary>
    /// Decrypts encrypted assembly data using the md5table.
    /// Source: ScriptResource.cs lines 74-103
    /// </summary>
    private static byte[] Decrypt(ReadOnlySpan<byte> md5Table, ReadOnlySpan<byte> md5Data)
    {
        if (md5Table.Length == 0)
            return [];

        // Calculate seed by summing all 8-byte entries in md5table
        ulong seed = 0;
        for (int i = 0; i < md5Table.Length; i += Md5TableEntrySize)
        {
            seed += BinaryPrimitives.ReadUInt64LittleEndian(md5Table[i..]);
        }
        seed = (ulong)(md5Table.Length - 1) & seed;

        var result = new MemoryStream();
        int dataOffset = 0;

        for (int i = 0; i < md5Table.Length; i += Md5TableEntrySize)
        {
            var buffer = new byte[BlockSize];

            // Check if this block is encrypted (first byte of table entry is even)
            if ((md5Table[i] & 1) == 0)
            {
                // Copy encrypted block
                md5Data.Slice(dataOffset, BlockSize).CopyTo(buffer);
                dataOffset += BlockSize;

                // Decrypt using XOR
                for (int j = 0; j < BlockSize; j++)
                {
                    byte value = buffer[j];
                    buffer[j] ^= md5Table[(int)seed];
                    seed = (ulong)((seed + value) % (ulong)md5Table.Length);
                }
            }
            else
            {
                // Block is all zeros (skipped during encryption)
                dataOffset += BlockSize;
            }

            result.Write(buffer, 0, buffer.Length);
        }

        return result.ToArray();
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: ScriptResource.cs lines 105-123
        using var ms = new MemoryStream();

        // Write version
        ms.WriteByte(Version);

        // Write game version if version > 1
        if (Version > 1)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, GameVersion.Length);
            ms.Write(buffer);

            foreach (char c in GameVersion)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(0, 2), c);
                ms.Write(buffer, 0, 2);
            }
        }

        // Write unknown2
        var unknown2Buffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(unknown2Buffer, Unknown2);
        ms.Write(unknown2Buffer);

        // Calculate md5table size based on cleardata length
        // Each block is 512 bytes, each table entry is 8 bytes
        int blockCount = ((_clearData.Length + BlockSize - 1) / BlockSize);
        if (blockCount == 0 && _clearData.Length == 0)
            blockCount = 0;

        var newMd5Table = new byte[blockCount * Md5TableEntrySize];

        // Encrypt the data
        byte[] md5Data = Encrypt(newMd5Table, _clearData);

        // Write md5sum
        ms.Write(_md5Sum);

        // Write block count
        var countBuffer = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(countBuffer, (ushort)blockCount);
        ms.Write(countBuffer);

        // Write md5table
        ms.Write(newMd5Table);

        // Write encrypted data
        ms.Write(md5Data);

        return ms.ToArray();
    }

    /// <summary>
    /// Encrypts assembly data using the md5table.
    /// Source: ScriptResource.cs lines 125-149
    /// </summary>
    private static byte[] Encrypt(Span<byte> md5Table, ReadOnlySpan<byte> clearData)
    {
        if (md5Table.Length == 0)
            return [];

        // Calculate seed by summing all 8-byte entries in md5table
        ulong seed = 0;
        for (int i = 0; i < md5Table.Length; i += Md5TableEntrySize)
        {
            seed += BinaryPrimitives.ReadUInt64LittleEndian(md5Table[i..]);
        }
        seed = (ulong)(md5Table.Length - 1) & seed;

        var result = new MemoryStream();
        int dataOffset = 0;

        for (int i = 0; i < md5Table.Length; i += Md5TableEntrySize)
        {
            var buffer = new byte[BlockSize];

            // Read up to BlockSize bytes from cleardata
            int bytesToCopy = Math.Min(BlockSize, clearData.Length - dataOffset);
            if (bytesToCopy > 0)
            {
                clearData.Slice(dataOffset, bytesToCopy).CopyTo(buffer);
            }
            dataOffset += BlockSize;

            // Encrypt using XOR
            for (int j = 0; j < BlockSize; j++)
            {
                buffer[j] ^= md5Table[(int)seed];
                seed = (ulong)((seed + buffer[j]) % (ulong)md5Table.Length);
            }

            result.Write(buffer, 0, buffer.Length);
        }

        return result.ToArray();
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 1;
        GameVersion = string.Empty;
        Unknown2 = DefaultUnknown2;
        _md5Sum = new byte[64];
        _md5Table = [];
        _clearData = [];
    }

    /// <summary>
    /// Sets the MD5 sum value.
    /// </summary>
    /// <param name="md5">The 64-byte MD5 sum data.</param>
    public void SetMd5Sum(ReadOnlySpan<byte> md5)
    {
        if (md5.Length != 64)
            throw new ArgumentException("MD5 sum must be exactly 64 bytes.", nameof(md5));

        md5.CopyTo(_md5Sum);
        OnChanged();
    }
}
