namespace TS4Tools.UI.Services;

/// <summary>
/// Default implementation of <see cref="IFileSystemService"/> using System.IO.
/// </summary>
/// <remarks>
/// This implementation wraps System.IO operations for production use.
/// For testing, use a mock implementation of <see cref="IFileSystemService"/>.
/// </remarks>
public sealed class FileSystemService : IFileSystemService
{
    private static FileSystemService? _instance;

    /// <summary>
    /// Gets the singleton instance of the file system service.
    /// </summary>
    public static FileSystemService Instance => _instance ??= new FileSystemService();

    private FileSystemService()
    {
    }

    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllBytesAsync(path, cancellationToken);

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        => File.WriteAllBytesAsync(path, bytes, cancellationToken);

    /// <inheritdoc />
    public string ReadAllText(string path)
        => File.ReadAllText(path);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);

    /// <inheritdoc />
    public void WriteAllText(string path, string contents)
        => File.WriteAllText(path, contents);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);

    /// <inheritdoc />
    public bool FileExists(string path)
        => File.Exists(path);

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore deletion errors (temp file cleanup)
        }
    }

    /// <inheritdoc />
    public DateTime GetLastWriteTimeUtc(string path)
        => File.GetLastWriteTimeUtc(path);

    /// <inheritdoc />
    public void CreateDirectory(string path)
        => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public string CombinePath(params string[] paths)
        => Path.Combine(paths);

    /// <inheritdoc />
    public string GetTempPath()
        => Path.GetTempPath();

    /// <inheritdoc />
    public string GetApplicationDataPath()
        => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    /// <inheritdoc />
    public string GetFileName(string path)
        => Path.GetFileName(path);

    /// <inheritdoc />
    public string? GetDirectoryName(string path)
        => Path.GetDirectoryName(path);
}
