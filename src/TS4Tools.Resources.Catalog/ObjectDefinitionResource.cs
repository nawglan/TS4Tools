using System.Text;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// A modern implementation of The Sims 4 Object Definition resource.
/// Object Definition Resources (0xC0DB5AE7) contain the core definitions
/// for objects in the game, including their properties, components, and references.
/// </summary>
/// <remarks>
/// Object Definition Resources define the fundamental properties of game objects,
/// including their visual appearance, behavior, interactions, and metadata.
/// This is one of the most common resource types in Sims 4 packages.
/// </remarks>
public sealed class ObjectDefinitionResource : IObjectDefinitionResource, IDisposable
{
    /// <summary>
    /// The standard resource type identifier for Object Definition resources
    /// </summary>
    public const uint ResourceType = 0xC0DB5AE7;

    private readonly int _requestedApiVersion;
    private readonly List<uint> _propertyIds;
    private readonly List<uint> _components;
    private bool _isDisposed;

    // Core properties from the Sims 4 Object Definition Resource format
    private ushort _version = 2;
    private string _name = string.Empty;
    private string _tuning = string.Empty;
    private ulong _tuningId;
    private string _materialVariant = string.Empty;
    private IResourceReference? _icon;
    private IResourceReference? _rig;
    private IResourceReference? _slot;
    private IResourceReference? _model;
    private IResourceReference? _footprint;

    /// <summary>
    /// Initializes a new instance of the ObjectDefinitionResource class.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    public ObjectDefinitionResource(int requestedApiVersion)
    {
        _requestedApiVersion = requestedApiVersion;
        _propertyIds = new List<uint>();
        _components = new List<uint>();
    }

    /// <inheritdoc/>
    public uint TypeId => ResourceType;

    /// <inheritdoc/>
    public string ContentType => "Object Definition";

    /// <inheritdoc/>
    public Stream Stream { get; set; } = new MemoryStream();

    /// <inheritdoc/>
    public bool HasUnparsedData => false;

    /// <inheritdoc/>
    public byte[] AsBytes
    {
        get
        {
            if (Stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            using var ms = new MemoryStream();
            Stream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc/>
    public event EventHandler? ResourceChanged;

    /// <inheritdoc/>
    public int RequestedApiVersion => _requestedApiVersion;

    /// <inheritdoc/>
    public int RecommendedApiVersion => 1;

    /// <inheritdoc/>
    public ushort Version
    {
        get => _version;
        private set => _version = value;
    }

    /// <inheritdoc/>
    public string Name
    {
        get => _name;
        set => _name = value ?? string.Empty;
    }

    /// <inheritdoc/>
    public string Tuning
    {
        get => _tuning;
        set => _tuning = value ?? string.Empty;
    }

    /// <inheritdoc/>
    public ulong TuningId
    {
        get => _tuningId;
        set => _tuningId = value;
    }

    /// <inheritdoc/>
    public IReadOnlyList<uint> PropertyIds => _propertyIds.AsReadOnly();

    /// <inheritdoc/>
    public string MaterialVariant
    {
        get => _materialVariant;
        set => _materialVariant = value ?? string.Empty;
    }

    /// <inheritdoc/>
    public IResourceReference? Icon => _icon;

    /// <inheritdoc/>
    public IResourceReference? Rig => _rig;

    /// <inheritdoc/>
    public IResourceReference? Slot => _slot;

    /// <inheritdoc/>
    public IResourceReference? Model => _model;

    /// <inheritdoc/>
    public IResourceReference? Footprint => _footprint;

    /// <inheritdoc/>
    public IReadOnlyList<uint> Components => _components.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<string> ContentFields => GetContentFieldNames().ToArray();

    /// <inheritdoc/>
    public TypedValue this[int index] 
    { 
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    /// <inheritdoc/>
    public TypedValue this[string name] 
    { 
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    /// <inheritdoc/>
    public void AddPropertyId(uint propertyId)
    {
        if (!_propertyIds.Contains(propertyId))
        {
            _propertyIds.Add(propertyId);
            OnResourceChanged();
        }
    }

    /// <inheritdoc/>
    public bool RemovePropertyId(uint propertyId)
    {
        if (_propertyIds.Remove(propertyId))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public void AddComponent(uint componentId)
    {
        if (!_components.Contains(componentId))
        {
            _components.Add(componentId);
            OnResourceChanged();
        }
    }

    /// <inheritdoc/>
    public bool RemoveComponent(uint componentId)
    {
        if (_components.Remove(componentId))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets a resource reference for the specified property.
    /// </summary>
    /// <param name="propertyId">The property ID</param>
    /// <param name="reference">The resource reference to set</param>
    public void SetResourceReference(uint propertyId, IResourceReference? reference)
    {
        switch (propertyId)
        {
            case 0xCADED888: // Icon
                _icon = reference;
                break;
            case 0xE206AE4F: // Rig
                _rig = reference;
                break;
            case 0x8A85AFF3: // Slot
                _slot = reference;
                break;
            case 0x8D20ACC6: // Model
                _model = reference;
                break;
            case 0x6C737AD8: // Footprint
                _footprint = reference;
                break;
        }
        OnResourceChanged();
    }

    /// <inheritdoc/>
    public Task<Stream> ToStreamAsync()
    {
        var memoryStream = new MemoryStream();
        var writer = new BinaryWriter(memoryStream);

        try
        {
            // Write version (2 bytes)
            writer.Write(_version);

            // Calculate and write table position (4 bytes) - we'll come back to this
            var tablePositionOffset = memoryStream.Position;
            writer.Write((uint)0); // Placeholder

            // Write property data sections first
            var propertyOffsets = new Dictionary<uint, uint>();

            // Write Name property (0xE7F07786)
            if (!string.IsNullOrEmpty(_name))
            {
                propertyOffsets[0xE7F07786] = (uint)memoryStream.Position;
                WriteString(writer, _name);
            }

            // Write Tuning property (0x790FA4BC)
            if (!string.IsNullOrEmpty(_tuning))
            {
                propertyOffsets[0x790FA4BC] = (uint)memoryStream.Position;
                var tuningBytes = Encoding.ASCII.GetBytes(_tuning);
                writer.Write(tuningBytes.Length);
                writer.Write(tuningBytes);
            }

            // Write TuningID property (0xB994039B)
            if (_tuningId != 0)
            {
                propertyOffsets[0xB994039B] = (uint)memoryStream.Position;
                writer.Write(_tuningId);
            }

            // Write MaterialVariant property (0xECD5A95F)
            if (!string.IsNullOrEmpty(_materialVariant))
            {
                propertyOffsets[0xECD5A95F] = (uint)memoryStream.Position;
                WriteString(writer, _materialVariant);
            }

            // Write Icon reference (0xCADED888)
            if (_icon != null)
            {
                propertyOffsets[0xCADED888] = (uint)memoryStream.Position;
                WriteResourceReference(writer, _icon);
            }

            // Write Rig reference (0xE206AE4F)
            if (_rig != null)
            {
                propertyOffsets[0xE206AE4F] = (uint)memoryStream.Position;
                WriteResourceReference(writer, _rig);
            }

            // Write Slot reference (0x8A85AFF3)
            if (_slot != null)
            {
                propertyOffsets[0x8A85AFF3] = (uint)memoryStream.Position;
                WriteResourceReference(writer, _slot);
            }

            // Write Model reference (0x8D20ACC6)
            if (_model != null)
            {
                propertyOffsets[0x8D20ACC6] = (uint)memoryStream.Position;
                WriteResourceReference(writer, _model);
            }

            // Write Footprint reference (0x6C737AD8)
            if (_footprint != null)
            {
                propertyOffsets[0x6C737AD8] = (uint)memoryStream.Position;
                WriteResourceReference(writer, _footprint);
            }

            // Write Components (0xE6E421FB)
            if (_components.Count > 0)
            {
                propertyOffsets[0xE6E421FB] = (uint)memoryStream.Position;
                writer.Write(_components.Count);
                foreach (var component in _components)
                {
                    writer.Write(component);
                }
            }

            // Now write the property table
            var tablePosition = (uint)memoryStream.Position;

            // Write number of properties as ushort
            writer.Write((ushort)propertyOffsets.Count);

            // Write property ID and offset pairs
            foreach (var kvp in propertyOffsets.OrderBy(x => x.Key))
            {
                writer.Write(kvp.Key);   // Property ID
                writer.Write(kvp.Value); // Offset to property data
            }

            // Go back and write the actual table position
            var endPosition = memoryStream.Position;
            memoryStream.Seek(tablePositionOffset, SeekOrigin.Begin);
            writer.Write(tablePosition);
            memoryStream.Seek(endPosition, SeekOrigin.Begin);

            memoryStream.Position = 0;
            return Task.FromResult<Stream>(memoryStream);
        }
        catch
        {
            writer.Dispose();
            memoryStream.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public Task ParseFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        try
        {
            // Read version (2 bytes)
            _version = reader.ReadUInt16();
            if (_version != 1 && _version != 2)
            {
                throw new InvalidDataException($"Unsupported Object Definition Resource version: {_version}");
            }

            // Read table position (4 bytes)
            var tablePosition = reader.ReadUInt32();

            // Navigate to the property table
            stream.Seek(tablePosition, SeekOrigin.Begin);

            // Read number of properties (ushort, not uint)
            var propertyCount = reader.ReadUInt16();

            // Read property ID and offset pairs
            var properties = new Dictionary<uint, uint>();
            for (int i = 0; i < propertyCount; i++)
            {
                var propertyId = reader.ReadUInt32();
                var offset = reader.ReadUInt32();
                properties[propertyId] = offset;
                AddPropertyId(propertyId);
            }

            // Parse each property by seeking to its offset
            foreach (var kvp in properties)
            {
                var propertyId = kvp.Key;
                var offset = kvp.Value;

                stream.Seek(offset, SeekOrigin.Begin);

                switch (propertyId)
                {
                    case 0xE7F07786: // Name
                        _name = ReadString(reader);
                        break;

                    case 0x790FA4BC: // Tuning
                        var tuningLength = reader.ReadInt32();
                        var tuningBytes = reader.ReadBytes(tuningLength);
                        _tuning = Encoding.ASCII.GetString(tuningBytes);
                        break;

                    case 0xB994039B: // TuningID
                        _tuningId = reader.ReadUInt64();
                        break;

                    case 0xECD5A95F: // MaterialVariant
                        _materialVariant = ReadString(reader);
                        break;

                    case 0xCADED888: // Icon
                        _icon = ReadResourceReference(reader);
                        break;

                    case 0xE206AE4F: // Rig (corrected PropertyID)
                        _rig = ReadResourceReference(reader);
                        break;

                    case 0x8A85AFF3: // Slot (corrected PropertyID)
                        _slot = ReadResourceReference(reader);
                        break;

                    case 0x8D20ACC6: // Model (corrected PropertyID)
                        _model = ReadResourceReference(reader);
                        break;

                    case 0x6C737AD8: // Footprint (corrected PropertyID)
                        _footprint = ReadResourceReference(reader);
                        break;

                    case 0xE6E421FB: // Components
                        var componentCount = reader.ReadInt32();
                        for (int j = 0; j < componentCount; j++)
                        {
                            _components.Add(reader.ReadUInt32());
                        }
                        break;

                    default:
                        // Unknown property - skip for now
                        break;
                }
            }

            OnResourceChanged();
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Unexpected end of stream while parsing Object Definition Resource", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Error parsing Object Definition Resource: {ex.Message}", ex);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads a string with length prefix from the binary reader.
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <returns>The string value</returns>
    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length == 0)
            return string.Empty;

        var bytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Writes a string with length prefix to the binary writer.
    /// </summary>
    /// <param name="writer">The binary writer</param>
    /// <param name="value">The string to write</param>
    private static void WriteString(BinaryWriter writer, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write(0);
            return;
        }

        var bytes = Encoding.ASCII.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>
    /// Reads a resource reference (TGI block) from the binary reader.
    /// </summary>
    /// <param name="reader">The binary reader</param>
    /// <returns>The resource reference</returns>
    private static IResourceReference ReadResourceReference(BinaryReader reader)
    {
        var typeId = reader.ReadUInt32();
        var groupId = reader.ReadUInt32();
        var instanceBytes = reader.ReadBytes(8);
        
        // Swap bytes for instance (Sims 4 format)
        Array.Reverse(instanceBytes);
        var instanceId = BitConverter.ToUInt64(instanceBytes, 0);

        return new ResourceReference(typeId, groupId, instanceId);
    }

    /// <summary>
    /// Writes a resource reference (TGI block) to the binary writer.
    /// </summary>
    /// <param name="writer">The binary writer</param>
    /// <param name="reference">The resource reference to write</param>
    private static void WriteResourceReference(BinaryWriter writer, IResourceReference reference)
    {
        writer.Write(reference.Type);
        writer.Write(reference.Group);
        
        // Swap bytes for instance (Sims 4 format)
        var instanceBytes = BitConverter.GetBytes(reference.Instance);
        Array.Reverse(instanceBytes);
        writer.Write(instanceBytes);
    }

    private IEnumerable<string> GetContentFieldNames()
    {
        yield return "Name";
        yield return "Tuning";
        yield return "TuningId";
        yield return "MaterialVariant";
        yield return "PropertyCount";
        yield return "ComponentCount";
        
        if (_icon != null)
            yield return "Icon";
        if (_rig != null)
            yield return "Rig";
        if (_slot != null)
            yield return "Slot";
        if (_model != null)
            yield return "Model";
        if (_footprint != null)
            yield return "Footprint";
    }

    private TypedValue GetFieldByIndex(int index)
    {
        var fields = GetContentFieldNames().ToArray();
        if (index < 0 || index >= fields.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return GetFieldByName(fields[index]);
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            "Name" => new TypedValue(typeof(string), _name),
            "Tuning" => new TypedValue(typeof(string), _tuning),
            "TuningId" => new TypedValue(typeof(ulong), _tuningId),
            "MaterialVariant" => new TypedValue(typeof(string), _materialVariant),
            "PropertyCount" => new TypedValue(typeof(int), _propertyIds.Count),
            "ComponentCount" => new TypedValue(typeof(int), _components.Count),
            "Icon" => new TypedValue(typeof(string), _icon?.ToString() ?? "None"),
            "Rig" => new TypedValue(typeof(string), _rig?.ToString() ?? "None"),
            "Slot" => new TypedValue(typeof(string), _slot?.ToString() ?? "None"),
            "Model" => new TypedValue(typeof(string), _model?.ToString() ?? "None"),
            "Footprint" => new TypedValue(typeof(string), _footprint?.ToString() ?? "None"),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        var fields = GetContentFieldNames().ToArray();
        if (index < 0 || index >= fields.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        SetFieldByName(fields[index], value);
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            case "Name":
                Name = value.ToString() ?? string.Empty;
                break;
            case "Tuning":
                Tuning = value.ToString() ?? string.Empty;
                break;
            case "TuningId":
                if (value.Value is ulong ulongVal)
                    TuningId = ulongVal;
                else if (ulong.TryParse(value.ToString(), out var parsed))
                    TuningId = parsed;
                break;
            case "MaterialVariant":
                MaterialVariant = value.ToString() ?? string.Empty;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _propertyIds.Clear();
            _components.Clear();
            Stream?.Dispose();
            _isDisposed = true;
        }
    }
}
