using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Package;

/// <summary>
/// Factory interface for creating package instances.
/// Provides modern dependency injection-based package creation.
/// </summary>
public interface IPackageFactory
{
    /// <summary>
    /// Creates a new empty package.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new empty package instance</returns>
    Task<IPackage> CreateEmptyPackageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a package from a file path.
    /// </summary>
    /// <param name="filePath">Path to the package file</param>
    /// <param name="readOnly">Whether to open the package in read-only mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded package instance</returns>
    Task<IPackage> LoadFromFileAsync(string filePath, bool readOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a package from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the package data</param>
    /// <param name="readOnly">Whether to open the package in read-only mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded package instance</returns>
    Task<IPackage> LoadFromStreamAsync(Stream stream, bool readOnly = false, CancellationToken cancellationToken = default);
}
