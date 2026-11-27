using TS4Tools.Extensions;
using TS4Tools.Package;

namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/SpecificResource.cs lines 28-237
//   (SpecificResource, SpecificIndexEntry)
// Modern differences: a SpecificResource is always resolved (lookups happen in GameFileTable.FindAsync, so
// there is no deferred searchList); Commit is not ported (read-only view); wrappers are obtained through
// DbpfPackage.GetResourceAsync.

/// <summary>
/// A resource index entry bound to the package it came from, with the file table source that supplied it.
/// </summary>
public sealed class SpecificResource : IEquatable<SpecificResource>
{
    /// <summary>Source labels, as in legacy <c>SpecificIndexEntry.PPSource</c>.</summary>
    public const string SourceCurrent = "current";

    /// <summary>Custom content package.</summary>
    public const string SourceCustomContent = "cc";

    /// <summary>Primary game content package.</summary>
    public const string SourceGameContent = "fb0";

    /// <summary>DDS image package.</summary>
    public const string SourceDdsImages = "dds";

    /// <summary>Thumbnail package.</summary>
    public const string SourceThumbnails = "tmb";

    /// <summary>Not in any file table list.</summary>
    public const string SourceUnknown = "unk";

    private readonly GameFolders? _gameFolders;

    /// <summary>Binds <paramref name="entry"/> to <paramref name="package"/>.</summary>
    /// <param name="package">The tuple containing the entry.</param>
    /// <param name="entry">The index entry; must belong to the package (when already opened).</param>
    /// <param name="source">The <see cref="PPSource"/> label; defaults to <see cref="SourceUnknown"/>.</param>
    /// <param name="gameFolders">Used to resolve <see cref="RGVsn"/>; optional.</param>
    /// <param name="requestedKey">The key by which the item was requested; defaults to the entry's key.</param>
    /// <exception cref="ArgumentException">The opened package does not contain <paramref name="entry"/>.</exception>
    public SpecificResource(
        PathPackageTuple package,
        IResourceIndexEntry entry,
        string source = SourceUnknown,
        GameFolders? gameFolders = null,
        ResourceKey? requestedKey = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(source);

        if (package.OpenedPackage is { } opened && opened.Find(e => ReferenceEquals(e, entry)) is null)
            throw new ArgumentException("Package does not contain specified ResourceIndexEntry", nameof(entry));

        PathPackage = package;
        Entry = entry;
        PPSource = source;
        RequestedKey = requestedKey ?? entry.Key;
        _gameFolders = gameFolders;
    }

    /// <summary>
    /// Which file table supplied this item: "current", "cc", "fb0" (game content), "dds", "tmb" or "unk".
    /// </summary>
    public string PPSource { get; }

    /// <summary>The package this item was resolved from.</summary>
    public PathPackageTuple PathPackage { get; }

    /// <summary>The index entry in the package.</summary>
    public IResourceIndexEntry Entry { get; }

    /// <summary>The key by which this item was requested.</summary>
    public ResourceKey RequestedKey { get; }

    /// <summary>Resource tag, the entry key and the path it was resolved from.</summary>
    public string LongName => $"{ExtList.GetTag(Entry.Key.ResourceType)}-{RK.Format(Entry.Key)} from {PathPackage.Path}";

    /// <summary>
    /// The game name for the Resource Group Version of this item (upper-cased), "" for the base game,
    /// or "Unk" if no game has that version.
    /// </summary>
    public string RGVsn
    {
        get
        {
            var version = (byte)(Entry.Key.ResourceGroup >> 27);
            if (version == 0) return "";
            var g = _gameFolders?.ByRGVersion(version);
            return g is null ? "Unk" : g.Name.ToUpperInvariant();
        }
    }

    /// <summary>Reads the raw (decompressed) resource data.</summary>
    public async ValueTask<ReadOnlyMemory<byte>> GetDataAsync(CancellationToken cancellationToken = default)
    {
        var package = await PathPackage.GetPackageAsync(cancellationToken).ConfigureAwait(false);
        return await package.GetResourceDataAsync(Entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the resource through the package's resource API.</summary>
    public async ValueTask<IResource> GetResourceAsync(CancellationToken cancellationToken = default)
    {
        var package = await PathPackage.GetPackageAsync(cancellationToken).ConfigureAwait(false);
        return await package.GetResourceAsync(Entry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Same entry key in the same package (legacy compared index entry and package path).</summary>
    public bool Equals(SpecificResource? other) =>
        other is not null && Entry.Key == other.Entry.Key && PathPackage.Equals(other.PathPackage);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SpecificResource);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Entry.Key, PathPackage);

    /// <inheritdoc />
    public override string ToString() => LongName;
}
