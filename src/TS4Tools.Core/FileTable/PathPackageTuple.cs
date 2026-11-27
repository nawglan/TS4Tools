using TS4Tools.Package;

namespace TS4Tools.FileTable;

// Source: legacy_references/Sims4Tools/s4pi Extras/Filetable/PathPackageTuple.cs lines 28-96
// Modern differences: the package is opened lazily and read-only (legacy opened it in the constructor and
// could open read-write); AddResource/Commit are not ported because the file table is a read-only view.

/// <summary>
/// The path of a package and the (lazily opened, read-only) <see cref="DbpfPackage"/> stored there.
/// Disposing closes the package if it was opened.
/// </summary>
public sealed class PathPackageTuple : IEquatable<PathPackageTuple>, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DbpfPackage? _package;
    private bool _disposed;

    /// <summary>Stores the full pathname of <paramref name="path"/>; the package is not opened yet.</summary>
    public PathPackageTuple(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
    }

    /// <summary>The full path of the package.</summary>
    public string Path { get; }

    /// <summary>The package if it has already been opened, otherwise null.</summary>
    public DbpfPackage? OpenedPackage => _package;

    /// <summary>
    /// Opens (once) and returns the package read-only.
    /// </summary>
    /// <exception cref="PackageFormatException">The file is not a valid package.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public async ValueTask<DbpfPackage> GetPackageAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _package ??= await DbpfPackage.OpenAsync(Path, readWrite: false, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Like <see cref="GetPackageAsync"/> but returns null if the file is missing or not a readable package
    /// (legacy skipped such packages in the file table).
    /// </summary>
    public async ValueTask<DbpfPackage?> TryGetPackageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPackageAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is PackageFormatException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Finds the first resource matching <paramref name="match"/>, or null.</summary>
    public async ValueTask<SpecificResource?> FindAsync(Func<IResourceIndexEntry, bool> match, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(match);
        var package = await TryGetPackageAsync(cancellationToken).ConfigureAwait(false);
        var rie = package?.Find(match);
        return rie is null ? null : new SpecificResource(this, rie);
    }

    /// <summary>Finds all resources matching <paramref name="match"/>.</summary>
    public async ValueTask<IReadOnlyList<SpecificResource>> FindAllAsync(Func<IResourceIndexEntry, bool> match, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(match);
        var package = await TryGetPackageAsync(cancellationToken).ConfigureAwait(false);
        if (package is null) return [];
        return package.FindAll(match).Select(rie => new SpecificResource(this, rie)).ToList();
    }

    /// <summary>True if <paramref name="other"/> has the same path (ordinal, as legacy).</summary>
    public bool Equals(PathPackageTuple? other) => other is not null && Path.Equals(other.Path, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PathPackageTuple);

    /// <inheritdoc />
    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Path;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            if (_package is not null)
            {
                await _package.DisposeAsync().ConfigureAwait(false);
                _package = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
