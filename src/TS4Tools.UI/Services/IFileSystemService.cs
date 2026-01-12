namespace TS4Tools.UI.Services;

/// <summary>
/// Abstraction for file system operations to improve testability.
/// </summary>
/// <remarks>
/// This interface allows ViewModels to be tested without actual file system access
/// by providing a mockable abstraction over System.IO operations.
/// </remarks>
public interface IFileSystemService
{
    /// <summary>
    /// Asynchronously reads all bytes from a file.
    /// </summary>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes bytes to a file.
    /// </summary>
    Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Asynchronously reads all text from a file.
    /// </summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Asynchronously writes text to a file.
    /// </summary>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Gets the last write time of a file in UTC.
    /// </summary>
    DateTime GetLastWriteTimeUtc(string path);

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Combines path segments into a single path.
    /// </summary>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Gets the system temporary folder path.
    /// </summary>
    string GetTempPath();

    /// <summary>
    /// Gets the application data folder path.
    /// </summary>
    string GetApplicationDataPath();

    /// <summary>
    /// Gets the file name from a path.
    /// </summary>
    string GetFileName(string path);

    /// <summary>
    /// Gets the directory name from a path.
    /// </summary>
    string? GetDirectoryName(string path);
}
