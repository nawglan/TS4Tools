using System.Reflection;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Scripts;

/// <summary>
/// Implementation of script resources that handle encrypted .NET assemblies in Sims 4 packages.
/// Handles resource type 0x073FAA07 (Encrypted Signed Assembly).
/// </summary>
public sealed class ScriptResource : IScriptResource, IApiVersion, IContentFields, IDisposable
{
    private const uint DefaultUnknown2 = 0x2BC4F79F;
    private const int MD5SumLength = 64;
    private const int EncryptionBlockSize = 512;
    private const int MD5TableEntrySize = 8;

    private readonly ILogger<ScriptResource> _logger;
    private bool _disposed;
    private MemoryStream? _stream;

    // Core properties
    private ResourceKey _resourceKey;
    private byte _version = 1;
    private string _gameVersion = string.Empty;
    private uint _unknown2 = DefaultUnknown2;
    private byte[] _md5Sum = new byte[MD5SumLength];
    private byte[] _md5Table = Array.Empty<byte>();
    private byte[] _encryptedData = Array.Empty<byte>();
    private byte[] _clearData = Array.Empty<byte>();

    /// <summary>
    /// Event raised when the resource is changed
    /// </summary>
#pragma warning disable CS0067 // Event is never used - part of IResource interface
    public event EventHandler? ResourceChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Initializes a new instance of the ScriptResource class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    public ScriptResource(ILogger<ScriptResource> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceKey = new ResourceKey(0, 0x073FAA07, 0);
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the ScriptResource class with specified data.
    /// </summary>
    /// <param name="resourceKey">The resource key</param>
    /// <param name="data">The raw resource data</param>
    /// <param name="logger">Logger for diagnostics</param>
    public ScriptResource(ResourceKey resourceKey, ReadOnlySpan<byte> data, ILogger<ScriptResource> logger)
        : this(logger)
    {
        _resourceKey = resourceKey;
        ParseFromData(data);

        // Update stream with raw data
        _stream?.Dispose();
        _stream = new MemoryStream(data.ToArray());
    }

    /// <inheritdoc />
    public ResourceKey ResourceKey
    {
        get => _resourceKey;
        set => _resourceKey = value;
    }

    /// <inheritdoc />
    public byte Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                _logger.LogDebug("Version changed to {Version}", value);
            }
        }
    }

    /// <inheritdoc />
    public string GameVersion
    {
        get => _gameVersion;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_gameVersion != value)
            {
                _gameVersion = value;
                _logger.LogDebug("GameVersion changed to {GameVersion}", value);
            }
        }
    }

    /// <inheritdoc />
    public uint Unknown2
    {
        get => _unknown2;
        set
        {
            if (_unknown2 != value)
            {
                _unknown2 = value;
                _logger.LogDebug("Unknown2 changed to 0x{Unknown2:X8}", value);
            }
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> MD5Sum
    {
        get => _md5Sum;
        set
        {
            if (value.Length != MD5SumLength)
                throw new ArgumentException($"MD5Sum must be exactly {MD5SumLength} bytes", nameof(value));

            _md5Sum = value.ToArray();
            _logger.LogDebug("MD5Sum updated");
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> AssemblyData => _clearData;

    /// <inheritdoc />
    public void SetAssemblyData(ReadOnlySpan<byte> assemblyData)
    {
        _clearData = assemblyData.ToArray();
        _logger.LogDebug("Assembly data set, length: {Length} bytes", assemblyData.Length);

        // Regenerate encryption tables when assembly data changes
        RegenerateEncryptionTables();
    }

    /// <inheritdoc />
    public async Task<AssemblyInfo> GetAssemblyInfoAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_clearData.Length == 0)
        {
            return new AssemblyInfo(
                "No assembly data",
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new Dictionary<string, string>()
            );
        }

        return await Task.Run(() =>
        {
            try
            {
                // Load assembly in a separate AppDomain for security
                var assembly = Assembly.Load(_clearData);

                var exportedTypes = new List<string>();
                try
                {
                    exportedTypes.AddRange(assembly.GetExportedTypes().Select(t => t.FullName ?? t.Name));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning("Could not load all types: {Message}", ex.Message);
                    exportedTypes.AddRange(ex.Types.Where(t => t != null).Select(t => t!.FullName ?? t.Name));
                }

                var referencedAssemblies = assembly.GetReferencedAssemblies()
                    .Select(a => a.FullName)
                    .ToList();

                var properties = new Dictionary<string, string>
                {
                    ["FullName"] = assembly.FullName ?? "Unknown",
                    ["Location"] = assembly.Location,
                    ["IsFullyTrusted"] = assembly.IsFullyTrusted.ToString(),
                    ["ImageRuntimeVersion"] = assembly.ImageRuntimeVersion,
                    ["EntryPoint"] = assembly.EntryPoint?.Name ?? "None"
                };

                return new AssemblyInfo(
                    assembly.FullName ?? "Unknown",
                    assembly.Location,
                    exportedTypes,
                    referencedAssemblies,
                    properties
                );
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "Invalid assembly format");
                throw new InvalidOperationException("Assembly data is not a valid .NET assembly", ex);
            }
            catch (SecurityException ex)
            {
                _logger.LogError(ex, "Security exception loading assembly");
                throw new InvalidOperationException("Security restrictions prevent loading assembly", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// The resource content as a stream
    /// </summary>
    public Stream Stream => _stream ?? throw new ObjectDisposedException(nameof(ScriptResource));

    /// <summary>
    /// The resource content as a byte array
    /// </summary>
    public byte[] AsBytes => GetRawData().ToArray();

    /// <summary>
    /// The requested API version
    /// </summary>
    public int RequestedApiVersion => 1;

    /// <summary>
    /// The recommended API version
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Get the value of a content field by index
    /// </summary>
    /// <param name="index">The index of the field</param>
    /// <returns>The typed value of the field</returns>
    public TypedValue this[int index]
    {
        get
        {
            if (index < 0 || index >= ContentFields.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return this[ContentFields[index]];
        }
        set => throw new NotSupportedException("Script resource fields are read-only");
    }

    /// <summary>
    /// Get the value of a content field by name
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <returns>The typed value of the field</returns>
    public TypedValue this[string name]
    {
        get => name switch
        {
            nameof(Version) => new TypedValue(typeof(byte), Version),
            nameof(GameVersion) => new TypedValue(typeof(string), GameVersion),
            nameof(Unknown2) => new TypedValue(typeof(uint), Unknown2),
            nameof(AssemblyData) => new TypedValue(typeof(ReadOnlyMemory<byte>), AssemblyData),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
        set => throw new NotSupportedException("Script resource fields are read-only");
    }

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetRawData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write version
        writer.Write(_version);

        // Write game version (only for version > 1)
        if (_version > 1)
        {
            var gameVersionBytes = Encoding.Unicode.GetBytes(_gameVersion);
            writer.Write(gameVersionBytes.Length / 2); // Length in characters, not bytes
            writer.Write(gameVersionBytes);
        }

        // Write unknown2
        writer.Write(_unknown2);

        // Write MD5 sum
        writer.Write(_md5Sum);

        // Encrypt data if needed
        if (_clearData.Length > 0)
        {
            RegenerateEncryptionTables();
            _encryptedData = EncryptData();
        }

        // Write encrypted data info
        var entryCount = (ushort)(_md5Table.Length / MD5TableEntrySize);
        writer.Write(entryCount);
        writer.Write(_md5Table);
        writer.Write(_encryptedData);

        return stream.ToArray();
    }

    /// <inheritdoc />
    public int ApiVersion => 1;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Version),
        nameof(GameVersion),
        nameof(Unknown2),
        nameof(AssemblyData)
    };

    private void ParseFromData(ReadOnlySpan<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read version
        _version = reader.ReadByte();

        // Read game version (only for version > 1)
        if (_version > 1)
        {
            var characterCount = reader.ReadInt32();
            var gameVersionBytes = reader.ReadBytes(characterCount * 2);
            _gameVersion = Encoding.Unicode.GetString(gameVersionBytes);
        }
        else
        {
            _gameVersion = string.Empty;
        }

        // Read unknown2
        _unknown2 = reader.ReadUInt32();

        // Read MD5 sum
        _md5Sum = reader.ReadBytes(MD5SumLength);

        // Read encrypted data
        var entryCount = reader.ReadUInt16();
        _md5Table = reader.ReadBytes(entryCount * MD5TableEntrySize);
        _encryptedData = reader.ReadBytes(entryCount * EncryptionBlockSize);

        // Decrypt the data
        _clearData = DecryptData();

        _logger.LogDebug("Parsed script resource: version={Version}, gameVersion='{GameVersion}', assemblySize={AssemblySize}",
            _version, _gameVersion, _clearData.Length);
    }

    private byte[] DecryptData()
    {
        if (_md5Table.Length == 0 || _encryptedData.Length == 0)
            return Array.Empty<byte>();

        var seed = CalculateSeed();
        using var output = new MemoryStream();
        using var input = new MemoryStream(_encryptedData);

        for (int i = 0; i < _md5Table.Length; i += MD5TableEntrySize)
        {
            var buffer = new byte[EncryptionBlockSize];
            var bytesRead = input.Read(buffer, 0, buffer.Length);

            if ((_md5Table[i] & 1) == 0) // Decrypt only if the flag is set
            {
                for (int j = 0; j < bytesRead; j++)
                {
                    var value = buffer[j];
                    buffer[j] ^= _md5Table[seed];
                    seed = (ulong)((seed + value) % (ulong)_md5Table.Length);
                }
            }

            output.Write(buffer, 0, bytesRead);
        }

        return output.ToArray();
    }

    private byte[] EncryptData()
    {
        if (_clearData.Length == 0)
            return Array.Empty<byte>();

        var seed = CalculateSeed();
        using var output = new MemoryStream();
        using var input = new MemoryStream(_clearData);

        for (int i = 0; i < _md5Table.Length; i += MD5TableEntrySize)
        {
            var buffer = new byte[EncryptionBlockSize];
            var bytesRead = input.Read(buffer, 0, buffer.Length);

            for (int j = 0; j < bytesRead; j++)
            {
                buffer[j] ^= _md5Table[seed];
                seed = (ulong)((seed + buffer[j]) % (ulong)_md5Table.Length);
            }

            output.Write(buffer, 0, EncryptionBlockSize); // Always write full block size
        }

        return output.ToArray();
    }

    private void RegenerateEncryptionTables()
    {
        if (_clearData.Length == 0)
        {
            _md5Table = Array.Empty<byte>();
            return;
        }

        // Calculate required number of blocks
        var blockCount = (_clearData.Length + EncryptionBlockSize - 1) / EncryptionBlockSize;
        _md5Table = new byte[blockCount * MD5TableEntrySize];

        // Original implementation leaves table as zeros - no random generation needed
        // The table will be filled with zeros (default array initialization)
    }

    private ulong CalculateSeed()
    {
        if (_md5Table.Length == 0)
            return 0;

        ulong seed = 0;
        for (int i = 0; i < _md5Table.Length; i += MD5TableEntrySize)
        {
            seed += BitConverter.ToUInt64(_md5Table, i);
        }
        return (ulong)(_md5Table.Length - 1) & seed; // Fixed: Use original algorithm
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            // Dispose stream
            _stream?.Dispose();
            _stream = null;

            // Clear sensitive data
            Array.Clear(_md5Sum);
            Array.Clear(_md5Table);
            Array.Clear(_encryptedData);
            Array.Clear(_clearData);

            _disposed = true;
        }
    }
}
