// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 263-338 (header fields recorded in the manifest)
// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 51-130 (index entry fields recorded per resource)

using TS4Tools.Package;

namespace TS4Tools.Cli;

/// <summary>
/// <c>manifest.json</c> written by <c>unpack</c> and read by <c>pack</c>: everything needed to rebuild the package
/// header and index (keys in index order, compression label, Unknown2) next to the decompressed resource files.
/// </summary>
internal sealed class PackageManifest
{
    public const int CurrentFormatVersion = 1;
    public const string FileName = "manifest.json";
    public const string ResourcesDirectory = "resources";

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    /// <summary>File name (no directory) of the package that was unpacked.</summary>
    public string? Source { get; set; }

    public ManifestHeader Header { get; set; } = new();

    public List<ManifestResource> Resources { get; set; } = [];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task<PackageManifest> ReadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        PackageManifest? manifest;
        try
        {
            manifest = await JsonSerializer.DeserializeAsync<PackageManifest>(stream, Options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new CliException($"{path}: invalid manifest JSON: {ex.Message}");
        }
        if (manifest is null)
            throw new CliException($"{path}: empty manifest");
        if (manifest.FormatVersion != CurrentFormatVersion)
            throw new CliException(
                $"{path}: unsupported manifest formatVersion {manifest.FormatVersion} (expected {CurrentFormatVersion})");
        manifest.Header ??= new ManifestHeader();
        manifest.Resources ??= [];
        if (manifest.Resources.Count > PackageLimits.MaxResourceCount)
            throw new CliException($"{path}: too many resources ({manifest.Resources.Count})");
        return manifest;
    }

    public async Task WriteAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, this, Options, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// The DBPF header fields. The index fields (<see cref="IndexCount"/>, <see cref="IndexSize"/>, <see cref="IndexPosition"/>,
/// <see cref="IndexRecordPositionLow"/>, <see cref="IndexType"/>) are informational: <c>pack</c> recomputes them.
/// </summary>
internal sealed class ManifestHeader
{
    public string Magic { get; set; } = "DBPF";
    public int Major { get; set; } = 2;
    public int Minor { get; set; } = 1;
    public int UserVersionMajor { get; set; }
    public int UserVersionMinor { get; set; }
    public int Unused1 { get; set; }
    public int CreationTime { get; set; }
    public int UpdatedTime { get; set; }
    public int Unused2 { get; set; }
    public int IndexCount { get; set; }
    public int IndexRecordPositionLow { get; set; }
    public int IndexSize { get; set; }
    public int Unused3 { get; set; }
    public int Unused4 { get; set; } = 3;
    public int IndexPosition { get; set; }

    /// <summary>The 28 bytes at offset 68, as 56 hex digits.</summary>
    public string Unused5 { get; set; } = new('0', DbpfPackage.Unused5Length * 2);

    public string IndexType { get; set; } = KeyFormat.Hex32(0);

    public static ManifestHeader From(DbpfPackage package) => new()
    {
        Magic = Encoding.ASCII.GetString(package.Magic.Span),
        Major = package.MajorVersion,
        Minor = package.MinorVersion,
        UserVersionMajor = package.UserVersionMajor,
        UserVersionMinor = package.UserVersionMinor,
        Unused1 = package.Unused1,
        CreationTime = package.CreationTime,
        UpdatedTime = package.UpdatedTime,
        Unused2 = package.Unused2,
        IndexCount = package.HeaderIndexCount,
        IndexRecordPositionLow = package.IndexRecordPositionLow,
        IndexSize = package.HeaderIndexSize,
        Unused3 = package.Unused3,
        Unused4 = package.Unused4,
        IndexPosition = package.HeaderIndexPosition,
        Unused5 = Convert.ToHexString(package.Unused5.Span),
        IndexType = KeyFormat.Hex32(package.IndexType),
    };

    /// <summary>Applies the settable header fields to a new package.</summary>
    /// <exception cref="CliException">Unsupported version or malformed <see cref="Unused5"/>.</exception>
    public void ApplyTo(DbpfPackage package)
    {
        if (Major != package.MajorVersion || Minor != package.MinorVersion)
            throw new CliException(
                $"manifest header version {Major}.{Minor} cannot be written (only {package.MajorVersion}.{package.MinorVersion} is supported)");
        byte[] unused5;
        try
        {
            unused5 = Convert.FromHexString(Unused5 ?? string.Empty);
        }
        catch (FormatException)
        {
            throw new CliException("manifest header unused5 is not a hex string");
        }
        if (unused5.Length != DbpfPackage.Unused5Length)
            throw new CliException($"manifest header unused5 must be {DbpfPackage.Unused5Length * 2} hex digits");

        package.UserVersionMajor = UserVersionMajor;
        package.UserVersionMinor = UserVersionMinor;
        package.Unused1 = Unused1;
        package.CreationTime = CreationTime;
        package.UpdatedTime = UpdatedTime;
        package.Unused2 = Unused2;
        package.Unused3 = Unused3;
        package.Unused4 = Unused4;
        package.Unused5 = unused5;
    }
}

/// <summary>One index entry, in index order.</summary>
internal sealed class ManifestResource
{
    public string Type { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;

    /// <summary>Path relative to the unpack directory, with '/' separators (e.g. <c>resources/....stbl</c>).</summary>
    public string File { get; set; } = string.Empty;

    /// <summary>Index compression label: "0x5A42" (ZLIB), "0x0000" (stored), "0xFFFF" (RefPack), "0xFFE0" (deletion record).</summary>
    public string CompressionType { get; set; } = KeyFormat.Hex16(0);

    public int Unknown2 { get; set; } = 1;

    /// <summary>Uncompressed size at unpack time (informational).</summary>
    public long MemSize { get; set; }

    /// <summary>SHA-256 of the decompressed bytes at unpack time, lowercase hex; used by <c>pack</c> to report edits.</summary>
    public string? Sha256 { get; set; }

    /// <exception cref="CliException">A key field is not valid hex.</exception>
    public ResourceKey GetKey()
    {
        if (!KeyFormat.TryParseHex32(Type, out uint t) || !KeyFormat.TryParseHex32(Group, out uint g) ||
            !KeyFormat.TryParseHex64(Instance, out ulong i))
            throw new CliException($"manifest resource '{File}' has an invalid key {Type}/{Group}/{Instance}");
        return new ResourceKey(t, g, i);
    }

    /// <exception cref="CliException">Not a 16-bit hex value or Unknown2 out of range.</exception>
    public (ushort Compression, ushort Unknown2) GetIndexFields()
    {
        if (!KeyFormat.TryParseHex16(CompressionType, out ushort compression))
            throw new CliException($"manifest resource '{File}' has an invalid compressionType '{CompressionType}'");
        if (Unknown2 is < 0 or > ushort.MaxValue)
            throw new CliException($"manifest resource '{File}' has an invalid unknown2 {Unknown2}");
        return (compression, (ushort)Unknown2);
    }
}
