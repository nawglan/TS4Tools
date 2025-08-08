using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Represents a Sims 4 Data Resource (DATA format) - Generic data containers with structured fields and metadata
/// </summary>
public sealed class DataResource : IResource, IDisposable
{
    private readonly ILogger<DataResource> _logger;
    private readonly List<DataEntry> _entries = new();
    private readonly List<StructureDefinition> _structures = new();
    private MemoryStream? _stream;
    private bool _disposed;

    // Constants
    private const uint DataFormatFourCC = 0x41544144; // "DATA" in little-endian
    private const uint NullOffset = 0x80000000;
    private const int ApiVersionValue = 1;

    // Events
    public event EventHandler? ResourceChanged;

    // Header fields
    private uint _version = 0x100;
    private uint _dataTablePosition;
    private uint _structureTablePosition;

    // Core data structures
    private readonly List<DataEntry> _dataEntries = new();
    private readonly List<StructureDefinition> _structureDefinitions = new();

    // Raw data fallback for non-DATA format files
    private byte[]? _rawData;
    private XmlDocument? _xmlDocument;
    private bool _isXmlFormat;

    /// <summary>
    /// Initializes a new instance of the DataResource class
    /// </summary>
    public DataResource(ILogger<DataResource> logger, ResourceKey key, Stream? stream = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ResourceKey = key ?? throw new ArgumentNullException(nameof(key));
        _stream = new MemoryStream();

        if (stream != null && stream.Length > 0)
        {
            ParseFromStream(stream);
        }
    }

    /// <summary>
    /// Gets the resource key associated with this resource
    /// </summary>
    public ResourceKey ResourceKey { get; }

    // IApiVersion Implementation
    public int RequestedApiVersion => ApiVersionValue;
    public int RecommendedApiVersion => ApiVersionValue;

    // IContentFields Implementation
    public IReadOnlyList<string> ContentFields => new List<string>
    {
        nameof(Version),
        nameof(DataTablePosition),
        nameof(StructureTablePosition),
        nameof(DataCount),
        nameof(StructureCount),
        nameof(IsXmlFormat)
    }.AsReadOnly();

    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    // IResource Implementation
    public Stream Stream
    {
        get
        {
            if (_stream == null)
                _stream = new MemoryStream();
            return _stream;
        }
    }

    public byte[] AsBytes
    {
        get
        {
            Stream.Position = 0;
            var buffer = new byte[Stream.Length];
            Stream.ReadExactly(buffer);
            return buffer;
        }
    }

    private TypedValue GetFieldByIndex(int index)
    {
        if (index < 0 || index >= ContentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return GetFieldByName(ContentFields[index]);
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        if (index < 0 || index >= ContentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        SetFieldByName(ContentFields[index], value);
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(DataTablePosition) => new TypedValue(typeof(uint), DataTablePosition),
            nameof(StructureTablePosition) => new TypedValue(typeof(uint), StructureTablePosition),
            nameof(DataCount) => new TypedValue(typeof(uint), DataCount),
            nameof(StructureCount) => new TypedValue(typeof(uint), StructureCount),
            nameof(IsXmlFormat) => new TypedValue(typeof(bool), IsXmlFormat),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            case nameof(Version):
                Version = value.Value is uint ver ? ver : 0;
                break;
            case nameof(DataTablePosition):
                DataTablePosition = value.Value is uint dataPos ? dataPos : 0;
                break;
            case nameof(StructureTablePosition):
                StructureTablePosition = value.Value is uint structPos ? structPos : 0;
                break;
            // Note: DataCount, StructureCount, IsXmlFormat are read-only computed properties
            default:
                throw new ArgumentException($"Unknown or read-only field: {name}", nameof(name));
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #region Properties

    /// <summary>
    /// Version of the DATA format
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnResourceChanged();
            }
        }
    }

    /// <summary>
    /// Position of the data table within the binary stream
    /// </summary>
    public uint DataTablePosition
    {
        get => _dataTablePosition;
        set
        {
            if (_dataTablePosition != value)
            {
                _dataTablePosition = value;
                OnResourceChanged();
            }
        }
    }

    /// <summary>
    /// Position of the structure table within the binary stream
    /// </summary>
    public uint StructureTablePosition
    {
        get => _structureTablePosition;
        set
        {
            if (_structureTablePosition != value)
            {
                _structureTablePosition = value;
                OnResourceChanged();
            }
        }
    }

    /// <summary>
    /// Number of data entries in this resource
    /// </summary>
    public int DataCount => _dataEntries.Count;

    /// <summary>
    /// Number of structure definitions in this resource
    /// </summary>
    public int StructureCount => _structureDefinitions.Count;

    /// <summary>
    /// Indicates if this resource is in XML text format instead of binary DATA format
    /// </summary>
    public bool IsXmlFormat => _isXmlFormat;

    /// <summary>
    /// XML content (if resource is in XML format)
    /// </summary>
    public string? XmlContent => _isXmlFormat && _xmlDocument != null ? _xmlDocument.OuterXml : null;

    /// <summary>
    /// Collection of data entries
    /// </summary>
    public IReadOnlyList<DataEntry> DataEntries => _dataEntries.AsReadOnly();

    /// <summary>
    /// Collection of structure definitions
    /// </summary>
    public IReadOnlyList<StructureDefinition> StructureDefinitions => _structureDefinitions.AsReadOnly();

    #endregion

    #region Parsing

    /// <summary>
    /// Parses the resource from a stream
    /// </summary>
    private void ParseFromStream(Stream stream)
    {
        try
        {
            stream.Position = 0;
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read magic number to determine format
            var magic = reader.ReadUInt32();

            if (magic == DataFormatFourCC)
            {
                _logger.LogDebug("Parsing binary DATA format resource");
                ParseBinaryFormat(reader);
            }
            else
            {
                // Try to parse as XML
                _logger.LogDebug("Attempting to parse as XML format");
                if (TryParseAsXml(stream))
                {
                    _isXmlFormat = true;
                    _logger.LogDebug("Successfully parsed as XML format");
                }
                else
                {
                    // Fallback to raw data
                    _logger.LogDebug("Storing as raw data - unrecognized format");
                    stream.Position = 0;
                    _rawData = new byte[stream.Length];
                    stream.ReadExactly(_rawData);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DataResource from stream");
            // Store as raw data on parse failure
            stream.Position = 0;
            _rawData = new byte[stream.Length];
            stream.ReadExactly(_rawData);
        }
    }

    /// <summary>
    /// Parses the binary DATA format
    /// </summary>
    private void ParseBinaryFormat(BinaryReader reader)
    {
        // Header: magic(4) + version(4) + dataTableOffset(4) + dataCount(4) + structTableOffset(4) + structCount(4)
        _version = reader.ReadUInt32();

        var dataTableOffset = reader.ReadUInt32();
        var dataCount = reader.ReadInt32();

        var structTableOffset = reader.ReadUInt32();
        var structCount = reader.ReadInt32();

        _logger.LogDebug("DATA header: version={Version}, dataCount={DataCount}, structCount={StructCount}",
            _version, dataCount, structCount);

        // Validate offsets
        if (dataTableOffset == 0 || structTableOffset == 0)
        {
            _logger.LogWarning("Invalid table offsets: data={DataOffset:X8}, struct={StructOffset:X8}",
                dataTableOffset, structTableOffset);
            return;
        }

        // Calculate absolute positions
        _dataTablePosition = dataTableOffset + 8; // Relative to position after data table offset field
        _structureTablePosition = structTableOffset + 20; // Relative to position after struct table offset field

        // Parse structure definitions first (they're referenced by data entries)
        if (structCount > 0)
        {
            reader.BaseStream.Position = _structureTablePosition;
            ParseStructureDefinitions(reader, structCount);
        }

        // Parse data entries
        if (dataCount > 0)
        {
            reader.BaseStream.Position = _dataTablePosition;
            ParseDataEntries(reader, dataCount);
        }
    }

    /// <summary>
    /// Parses structure definitions from the binary stream
    /// </summary>
    private void ParseStructureDefinitions(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            try
            {
                var structure = StructureDefinition.Parse(reader, _logger);
                _structureDefinitions.Add(structure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing structure definition {Index}", i);
                break; // Stop parsing on error to prevent corruption
            }
        }
    }

    /// <summary>
    /// Parses data entries from the binary stream
    /// </summary>
    private void ParseDataEntries(BinaryReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            try
            {
                var dataEntry = DataEntry.Parse(reader, _structureDefinitions, _logger);
                _dataEntries.Add(dataEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing data entry {Index}", i);
                break; // Stop parsing on error to prevent corruption
            }
        }
    }

    /// <summary>
    /// Attempts to parse the stream as XML
    /// </summary>
    private bool TryParseAsXml(Stream stream)
    {
        try
        {
            stream.Position = 0;
            var settings = new XmlReaderSettings
            {
                CloseInput = false,
                IgnoreComments = false,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = false,
                ValidationType = ValidationType.None
            };

            _xmlDocument = new XmlDocument();
            using var xmlReader = XmlReader.Create(stream, settings);
            _xmlDocument.Load(xmlReader);
            return true;
        }
        catch
        {
            _xmlDocument = null;
            return false;
        }
    }

    #endregion

    #region Resource Implementation

    /// <summary>
    /// Serializes the resource to a stream
    /// </summary>
    public async Task<Stream> SerializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isXmlFormat && _xmlDocument != null)
        {
            var xmlStream = new MemoryStream();
            using var writer = new StreamWriter(xmlStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(_xmlDocument.OuterXml.AsMemory(), cancellationToken);
            xmlStream.Position = 0;
            return xmlStream;
        }

        if (_rawData != null)
        {
            return new MemoryStream(_rawData);
        }

        // Generate binary DATA format
        return await SerializeBinaryFormatAsync(cancellationToken);
    }

    /// <summary>
    /// Serializes to binary DATA format
    /// </summary>
    private async Task<Stream> SerializeBinaryFormatAsync(CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write header with placeholder offsets
        writer.Write(DataFormatFourCC); // "DATA"
        writer.Write(_version);
        writer.Write(0u); // Data table offset (will be filled later)
        writer.Write(_dataEntries.Count);
        writer.Write(0u); // Structure table offset (will be filled later)
        writer.Write(_structureDefinitions.Count);

        // Add padding to align to 16-byte boundary
        while (stream.Position % 16 != 0)
        {
            writer.Write((byte)0);
        }

        // Write data table
        var dataTablePos = (uint)stream.Position;
        foreach (var dataEntry in _dataEntries)
        {
            dataEntry.WriteTo(writer);
        }

        // Write structure table
        var structTablePos = (uint)stream.Position;
        foreach (var structure in _structureDefinitions)
        {
            structure.WriteTo(writer);
        }

        // Update header with actual offsets
        stream.Position = 8; // Position of data table offset
        writer.Write(dataTablePos - 8); // Relative offset
        stream.Position = 16; // Position of structure table offset
        writer.Write(structTablePos - 20); // Relative offset

        stream.Position = 0;
        await Task.CompletedTask; // Make method async for interface compliance
        return stream;
    }

    /// <summary>
    /// Creates a deep copy of this resource
    /// </summary>
    public IResource Clone()
    {
        using var stream = SerializeAsync().GetAwaiter().GetResult();
        return new DataResource(_logger, ResourceKey, stream);
    }

    /// <summary>
    /// Validates the resource structure and data
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (_isXmlFormat)
        {
            if (_xmlDocument == null)
            {
                errors.Add("XML format specified but no XML document available");
                return false;
            }
            return true;
        }

        if (_rawData != null)
        {
            // Raw data is always valid
            return true;
        }

        // Validate binary format
        foreach (var dataEntry in _dataEntries)
        {
            if (!dataEntry.Validate(out var dataErrors))
            {
                errors.AddRange(dataErrors.Select(e => $"Data entry validation: {e}"));
            }
        }

        foreach (var structure in _structureDefinitions)
        {
            if (!structure.Validate(out var structErrors))
            {
                errors.AddRange(structErrors.Select(e => $"Structure definition validation: {e}"));
            }
        }

        return errors.Count == 0;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Adds a new data entry to the resource
    /// </summary>
    public void AddDataEntry(DataEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _dataEntries.Add(entry);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a data entry from the resource
    /// </summary>
    public bool RemoveDataEntry(DataEntry entry)
    {
        if (_dataEntries.Remove(entry))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a new structure definition to the resource
    /// </summary>
    public void AddStructureDefinition(StructureDefinition structure)
    {
        ArgumentNullException.ThrowIfNull(structure);
        _structureDefinitions.Add(structure);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a structure definition from the resource
    /// </summary>
    public bool RemoveStructureDefinition(StructureDefinition structure)
    {
        if (_structureDefinitions.Remove(structure))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Finds a structure definition by name
    /// </summary>
    public StructureDefinition? FindStructureByName(string name)
    {
        return _structureDefinitions.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds data entries by name
    /// </summary>
    public IEnumerable<DataEntry> FindDataEntriesByName(string name)
    {
        return _dataEntries.Where(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _stream?.Dispose();
        _disposed = true;
    }
}
