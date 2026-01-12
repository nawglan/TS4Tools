namespace TS4Tools;

/// <summary>
/// Security limits for package parsing to prevent resource exhaustion attacks.
/// </summary>
public static class PackageLimits
{
    /// <summary>
    /// Maximum number of resources allowed in a package.
    /// </summary>
    public const int MaxResourceCount = 500_000;

    /// <summary>
    /// Maximum size of a single resource (100 MB).
    /// </summary>
    public const int MaxResourceSize = 100 * 1024 * 1024;

    /// <summary>
    /// Maximum size of a package file (4 GB - 1).
    /// </summary>
    public const long MaxPackageSize = uint.MaxValue;

    /// <summary>
    /// Maximum string length for resource names.
    /// </summary>
    public const int MaxStringLength = 1024 * 1024;

    /// <summary>
    /// Size of the DBPF header in bytes.
    /// </summary>
    public const int HeaderSize = 96;

    /// <summary>
    /// DBPF magic bytes: "DBPF"
    /// </summary>
    public const uint Magic = 0x46504244; // "DBPF" in little-endian
}
