namespace TS4Tools.Core.Tests.TestFixtures;

/// <summary>
/// Centralized test data and fixtures for TS4Tools tests.
///
/// This provides commonly used test values, binary formats, and resource keys
/// to reduce duplication across test files and ensure consistency.
///
/// LEGACY ANALYSIS:
/// All binary format structures are based on analysis of:
/// - legacy_references/Sims4Tools/s4pi/Package/
/// - legacy_references/Sims4Tools/s4pi Wrappers/
/// </summary>
public static class TestData
{
    #region Resource Type IDs

    /// <summary>
    /// STBL (String Table) resource type ID.
    /// Source: s4pi Wrappers/StblResource
    /// </summary>
    public const uint StblTypeId = 0x220557DA;

    /// <summary>
    /// NameMap resource type ID.
    /// Source: s4pi Wrappers/NameMapResource
    /// </summary>
    public const uint NameMapTypeId = 0x0166038C;

    /// <summary>
    /// SimData resource type ID.
    /// Source: s4pi Wrappers/DataResource
    /// </summary>
    public const uint SimDataTypeId = 0x545AC67A;

    /// <summary>
    /// DDS/DST image resource type ID.
    /// </summary>
    public const uint DdsImageTypeId = 0x00B2D882;

    /// <summary>
    /// PNG image resource type ID.
    /// </summary>
    public const uint PngImageTypeId = 0x00B00000;

    /// <summary>
    /// Tuning XML resource type ID.
    /// </summary>
    public const uint TuningXmlTypeId = 0x03B33DDF;

    #endregion

    #region Test Resource Keys

    /// <summary>
    /// Standard STBL test key.
    /// </summary>
    public static readonly ResourceKey StblTestKey = new(StblTypeId, 0, 0);

    /// <summary>
    /// Standard NameMap test key.
    /// </summary>
    public static readonly ResourceKey NameMapTestKey = new(NameMapTypeId, 0, 0);

    /// <summary>
    /// Standard SimData test key.
    /// </summary>
    public static readonly ResourceKey SimDataTestKey = new(SimDataTypeId, 0, 0);

    #endregion

    #region FNV Hash Constants

    /// <summary>
    /// FNV-1a 32-bit offset basis.
    /// Source: http://www.isthe.com/chongo/tech/comp/fnv/#FNV-param
    /// </summary>
    public const uint Fnv32OffsetBasis = 0x811C9DC5;

    /// <summary>
    /// FNV-1a 32-bit prime.
    /// </summary>
    public const uint Fnv32Prime = 0x01000193;

    /// <summary>
    /// FNV-1a 64-bit offset basis.
    /// </summary>
    public const ulong Fnv64OffsetBasis = 0xCBF29CE484222325;

    /// <summary>
    /// FNV-1a 64-bit prime.
    /// </summary>
    public const ulong Fnv64Prime = 0x00000100000001B3;

    /// <summary>
    /// Known FNV-1a 32-bit hash of "hello".
    /// Verified against: https://www.tools4noobs.com/online_tools/hash/
    /// </summary>
    public const uint Fnv32Hello = 0x4F9F2CAB;

    /// <summary>
    /// Known FNV-1a 64-bit hash of "hello".
    /// </summary>
    public const ulong Fnv64Hello = 0xA430D84680AABD0B;

    #endregion

    #region Compression Constants

    /// <summary>
    /// ZLIB compression type marker ("ZB" = 0x5A42).
    /// Used in DBPF resource index entries.
    /// </summary>
    public const ushort ZlibCompressionType = 0x5A42;

    /// <summary>
    /// No compression marker.
    /// </summary>
    public const ushort NoCompression = 0x0000;

    /// <summary>
    /// ZLIB header byte (compression method = deflate, window size = 32K).
    /// </summary>
    public const byte ZlibHeader = 0x78;

    #endregion

    #region Binary Format Samples

    /// <summary>
    /// Minimal valid DBPF header (96 bytes).
    /// Major version 2, Minor version 1.
    /// </summary>
    public static byte[] CreateMinimalDbpfHeader()
    {
        var header = new byte[96];
        // Magic "DBPF"
        header[0] = 0x44; header[1] = 0x42; header[2] = 0x50; header[3] = 0x46;
        // Major version = 2
        header[4] = 0x02; header[5] = 0x00; header[6] = 0x00; header[7] = 0x00;
        // Minor version = 1
        header[8] = 0x01; header[9] = 0x00; header[10] = 0x00; header[11] = 0x00;
        return header;
    }

    /// <summary>
    /// Creates a minimal valid STBL binary with no entries.
    /// </summary>
    public static byte[] CreateEmptyStblBinary()
    {
        return
        [
            // Magic "STBL"
            0x53, 0x54, 0x42, 0x4C,
            // Version (5)
            0x05, 0x00,
            // IsCompressed (0)
            0x00,
            // Entry count (0)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // Reserved
            0x00, 0x00,
            // String data length (0)
            0x00, 0x00, 0x00, 0x00
        ];
    }

    /// <summary>
    /// Creates a minimal valid STBL binary with one entry.
    /// </summary>
    /// <param name="keyHash">The string key hash.</param>
    /// <param name="value">The string value.</param>
    public static byte[] CreateStblBinaryWithEntry(uint keyHash, string value)
    {
        var valueBytes = System.Text.Encoding.UTF8.GetBytes(value);
        var result = new List<byte>();

        // Header
        result.AddRange([0x53, 0x54, 0x42, 0x4C]); // Magic "STBL"
        result.AddRange([0x05, 0x00]); // Version 5
        result.Add(0x00); // IsCompressed
        result.AddRange(BitConverter.GetBytes((ulong)1)); // Entry count
        result.AddRange([0x00, 0x00]); // Reserved
        result.AddRange(BitConverter.GetBytes((uint)valueBytes.Length)); // String data length

        // Entry
        result.AddRange(BitConverter.GetBytes(keyHash)); // KeyHash
        result.Add(0x00); // Flags
        result.AddRange(BitConverter.GetBytes((ushort)valueBytes.Length)); // String length
        result.AddRange(valueBytes); // String

        return result.ToArray();
    }

    /// <summary>
    /// Creates a minimal valid NameMap binary with no entries.
    /// </summary>
    public static byte[] CreateEmptyNameMapBinary()
    {
        return
        [
            // Magic "NMAP" (version 1)
            0x01, 0x00, 0x00, 0x00,
            // Entry count (0)
            0x00, 0x00, 0x00, 0x00
        ];
    }

    /// <summary>
    /// Minimal PNG header for format detection tests.
    /// </summary>
    public static readonly byte[] MinimalPngHeader =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D, // IHDR length
        0x49, 0x48, 0x44, 0x52, // IHDR
        0x00, 0x00, 0x00, 0x01, // Width = 1
        0x00, 0x00, 0x00, 0x01, // Height = 1
        0x08, 0x02, 0x00, 0x00, 0x00 // Bit depth, color type, etc.
    ];

    /// <summary>
    /// Minimal DDS header for format detection tests.
    /// </summary>
    public static readonly byte[] MinimalDdsHeader =
    [
        0x44, 0x44, 0x53, 0x20, // "DDS "
        0x7C, 0x00, 0x00, 0x00, // Header size = 124
        0x07, 0x10, 0x00, 0x00, // Flags
        0x01, 0x00, 0x00, 0x00, // Height = 1
        0x01, 0x00, 0x00, 0x00, // Width = 1
    ];

    #endregion

    #region Test Data Generators

    /// <summary>
    /// Generates test data with a repeating pattern.
    /// Useful for testing compression (highly compressible).
    /// </summary>
    /// <param name="size">The size of data to generate.</param>
    /// <param name="pattern">The byte pattern to repeat.</param>
    public static byte[] GenerateRepetitiveData(int size, byte pattern = 0xAA)
    {
        var data = new byte[size];
        Array.Fill(data, pattern);
        return data;
    }

    /// <summary>
    /// Generates test data with sequential bytes.
    /// </summary>
    /// <param name="size">The size of data to generate.</param>
    public static byte[] GenerateSequentialData(int size)
    {
        var data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)(i % 256);
        }
        return data;
    }

    /// <summary>
    /// Generates pseudo-random test data with a fixed seed.
    /// </summary>
    /// <param name="size">The size of data to generate.</param>
    /// <param name="seed">The random seed for reproducibility.</param>
    public static byte[] GenerateRandomData(int size, int seed = 42)
    {
        var data = new byte[size];
        new Random(seed).NextBytes(data);
        return data;
    }

    #endregion
}
