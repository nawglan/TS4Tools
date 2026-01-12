using TS4Tools.UI.Services;

namespace TS4Tools.UI.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="IFileSystemService"/> for testing.
/// </summary>
/// <remarks>
/// This mock provides in-memory file system operations for unit testing
/// without accessing the actual file system.
/// </remarks>
public sealed class MockFileSystemService : IFileSystemService
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _textFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lastWriteTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks all operations performed for verification in tests.
    /// </summary>
    public List<string> OperationLog { get; } = [];

    /// <summary>
    /// Gets the files that have been written (for verification).
    /// </summary>
    public IReadOnlyDictionary<string, byte[]> WrittenFiles => _files;

    /// <summary>
    /// Gets the text files that have been written (for verification).
    /// </summary>
    public IReadOnlyDictionary<string, string> WrittenTextFiles => _textFiles;

    /// <summary>
    /// Gets the directories that have been created (for verification).
    /// </summary>
    public IReadOnlyCollection<string> CreatedDirectories => _directories;

    /// <summary>
    /// Configures a file to exist with the given content.
    /// </summary>
    public void SetupFile(string path, byte[] content)
    {
        _files[path] = content;
        _lastWriteTimes[path] = DateTime.UtcNow;
    }

    /// <summary>
    /// Configures a text file to exist with the given content.
    /// </summary>
    public void SetupTextFile(string path, string content)
    {
        _textFiles[path] = content;
        _lastWriteTimes[path] = DateTime.UtcNow;
    }

    /// <summary>
    /// Configures the last write time for a file.
    /// </summary>
    public void SetupLastWriteTime(string path, DateTime time)
    {
        _lastWriteTimes[path] = time;
    }

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        OperationLog.Add($"ReadAllBytesAsync: {path}");
        if (_files.TryGetValue(path, out var content))
        {
            return Task.FromResult(content);
        }
        throw new FileNotFoundException($"Mock file not found: {path}", path);
    }

    public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        OperationLog.Add($"WriteAllBytesAsync: {path}");
        _files[path] = bytes;
        _lastWriteTimes[path] = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public string ReadAllText(string path)
    {
        OperationLog.Add($"ReadAllText: {path}");
        if (_textFiles.TryGetValue(path, out var content))
        {
            return content;
        }
        throw new FileNotFoundException($"Mock file not found: {path}", path);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        OperationLog.Add($"ReadAllTextAsync: {path}");
        if (_textFiles.TryGetValue(path, out var content))
        {
            return Task.FromResult(content);
        }
        throw new FileNotFoundException($"Mock file not found: {path}", path);
    }

    public void WriteAllText(string path, string contents)
    {
        OperationLog.Add($"WriteAllText: {path}");
        _textFiles[path] = contents;
        _lastWriteTimes[path] = DateTime.UtcNow;
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        OperationLog.Add($"WriteAllTextAsync: {path}");
        _textFiles[path] = contents;
        _lastWriteTimes[path] = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public bool FileExists(string path)
    {
        OperationLog.Add($"FileExists: {path}");
        return _files.ContainsKey(path) || _textFiles.ContainsKey(path);
    }

    public void DeleteFile(string path)
    {
        OperationLog.Add($"DeleteFile: {path}");
        _files.Remove(path);
        _textFiles.Remove(path);
        _lastWriteTimes.Remove(path);
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        OperationLog.Add($"GetLastWriteTimeUtc: {path}");
        if (_lastWriteTimes.TryGetValue(path, out var time))
        {
            return time;
        }
        return DateTime.MinValue;
    }

    public void CreateDirectory(string path)
    {
        OperationLog.Add($"CreateDirectory: {path}");
        _directories.Add(path);
    }

    public string CombinePath(params string[] paths)
    {
        // Use standard path combining logic
        return Path.Combine(paths);
    }

    public string GetTempPath()
    {
        return "/tmp/mock";
    }

    public string GetApplicationDataPath()
    {
        return "/mock/appdata";
    }

    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }
}
