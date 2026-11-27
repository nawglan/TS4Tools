// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResource.cs lines 134-186 (highest version branch: 37),
// SkinToneResource.cs lines 85-92, SimOutfitResource.cs lines 66-123 and StyleLookResource.cs lines 61-82 (no version branches).
// Legacy has no equivalent exception: it parses newer versions with the newest layout it knows and silently
// misreads them. This port refuses instead, so a newer file is never rewritten corrupted.

namespace TS4Tools.Wrappers;

/// <summary>
/// Thrown when a resource's format version is newer than any layout the legacy s4pi wrapper knows, so it
/// cannot be parsed (or rewritten) faithfully.
/// </summary>
public sealed class UnsupportedResourceVersionException : ResourceFormatException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="resourceName">Wrapper name, e.g. "CASP".</param>
    /// <param name="version">The version read from the data.</param>
    /// <param name="maxSupportedVersion">The highest version this wrapper can parse.</param>
    public UnsupportedResourceVersionException(string resourceName, uint version, uint maxSupportedVersion)
        : base($"{resourceName} version 0x{version:X} is newer than the legacy s4pi layout (highest supported 0x{maxSupportedVersion:X})")
    {
        ResourceName = resourceName;
        Version = version;
        MaxSupportedVersion = maxSupportedVersion;
    }

    /// <summary>Wrapper name.</summary>
    public string ResourceName { get; }

    /// <summary>The version found in the data.</summary>
    public uint Version { get; }

    /// <summary>The highest version this wrapper parses.</summary>
    public uint MaxSupportedVersion { get; }
}
