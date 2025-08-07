using System.Collections.ObjectModel;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Implementation of ICharacterResource for handling character resources.
/// </summary>
public class CharacterResource : ICharacterResource
{
    private static readonly byte[] MagicBytes = [0x43, 0x48, 0x41, 0x52]; // "CHAR"
    
    private readonly List<CharacterPart> _characterParts = [];
    private readonly List<string> _contentFields = 
    [
        "CharacterType",
        "CharacterName", 
        "AgeCategory",
        "Gender",
        "Species",
        "SupportsMorphing",
        "Priority",
        "CharacterParts"
    ];
    
    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterResource"/> class.
    /// </summary>
    public CharacterResource()
    {
        CharacterType = CharacterType.None;
        CharacterName = string.Empty;
        AgeCategory = AgeCategory.None;
        Gender = Gender.None;
        Species = Species.None;
        SupportsMorphing = false;
        Priority = 0;
        CharacterParts = new ReadOnlyCollection<CharacterPart>(_characterParts);
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public CharacterResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ReadFromStream(stream);
    }

    /// <inheritdoc />
    public CharacterType CharacterType { get; set; }

    /// <inheritdoc />
    public string CharacterName { get; set; } = string.Empty;

    /// <inheritdoc />
    public AgeCategory AgeCategory { get; set; }

    /// <inheritdoc />
    public Gender Gender { get; set; }

    /// <inheritdoc />
    public Species Species { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<CharacterPart> CharacterParts { get; private set; }

    /// <inheritdoc />
    public bool SupportsMorphing { get; set; }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CharacterResource));
                
            _stream ??= new MemoryStream();
            return _stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CharacterResource));
                
            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields;

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Adds a character part to the resource.
    /// </summary>
    /// <param name="part">The character part to add</param>
    public void AddCharacterPart(CharacterPart part)
    {
        _characterParts.Add(part);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a character part from the resource.
    /// </summary>
    /// <param name="part">The character part to remove</param>
    /// <returns>True if the part was removed</returns>
    public bool RemoveCharacterPart(CharacterPart part)
    {
        var removed = _characterParts.Remove(part);
        if (removed)
            OnResourceChanged();
        return removed;
    }

    /// <summary>
    /// Clears all character parts.
    /// </summary>
    public void ClearCharacterParts()
    {
        if (_characterParts.Count > 0)
        {
            _characterParts.Clear();
            OnResourceChanged();
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
            
        return GetFieldValue(_contentFields[index]);
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "CharacterType" => TypedValue.Create(CharacterType),
            "CharacterName" => TypedValue.Create(CharacterName),
            "AgeCategory" => TypedValue.Create(AgeCategory),
            "Gender" => TypedValue.Create(Gender),
            "Species" => TypedValue.Create(Species),
            "SupportsMorphing" => TypedValue.Create(SupportsMorphing),
            "Priority" => TypedValue.Create(Priority),
            "CharacterParts" => TypedValue.Create(CharacterParts.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
            
        SetFieldValue(_contentFields[index], value);
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "CharacterType":
                if (value.Value is CharacterType characterType)
                    CharacterType = characterType;
                else if (value.Value is int characterIntValue)
                    CharacterType = (CharacterType)characterIntValue;
                break;
            case "CharacterName":
                CharacterName = value.Value?.ToString() ?? string.Empty;
                break;
            case "AgeCategory":
                if (value.Value is AgeCategory ageCategory)
                    AgeCategory = ageCategory;
                else if (value.Value is int ageIntValue)
                    AgeCategory = (AgeCategory)ageIntValue;
                break;
            case "Gender":
                if (value.Value is Gender gender)
                    Gender = gender;
                else if (value.Value is int genderIntValue)
                    Gender = (Gender)genderIntValue;
                break;
            case "Species":
                if (value.Value is Species species)
                    Species = species;
                else if (value.Value is int speciesIntValue)
                    Species = (Species)speciesIntValue;
                break;
            case "SupportsMorphing":
                if (value.Value is bool supportsMorphing)
                    SupportsMorphing = supportsMorphing;
                break;
            case "Priority":
                if (value.Value is int priority)
                    Priority = priority;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }
        
        OnResourceChanged();
    }

    /// <summary>
    /// Loads the character resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Copy stream to memory stream for processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        ReadFromStream(memoryStream);
    }

    private void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        
        // Handle empty stream gracefully
        if (stream.Length == 0)
        {
            // Initialize with default values for empty stream
            CharacterType = CharacterType.None;
            CharacterName = string.Empty;
            CharacterParts = new List<CharacterPart>();
            return;
        }
        
        // Read and validate magic bytes
        if (stream.Length < 4)
        {
            throw new InvalidDataException("Invalid character resource format");
        }
        
        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException("Invalid character resource format");
        }
        
        // Read character data
        CharacterType = (CharacterType)reader.ReadInt32();
        var nameLength = reader.ReadInt32();
        CharacterName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
        AgeCategory = (AgeCategory)reader.ReadInt32();
        Gender = (Gender)reader.ReadInt32();
        Species = (Species)reader.ReadInt32();
        SupportsMorphing = reader.ReadBoolean();
        Priority = reader.ReadInt32();
        
        // Read character parts
        var partCount = reader.ReadInt32();
        _characterParts.Clear();
        
        for (int i = 0; i < partCount; i++)
        {
            var instanceId = reader.ReadUInt32();
            var category = (PartCategory)reader.ReadInt32();
            var partNameLength = reader.ReadInt32();
            var partName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(partNameLength));
            var ageCategory = (AgeCategory)reader.ReadInt32();
            var gender = (Gender)reader.ReadInt32();
            var species = (Species)reader.ReadInt32();
            var sortPriority = reader.ReadInt32();
            
            _characterParts.Add(new CharacterPart(instanceId, category, partName, ageCategory, gender, species, sortPriority));
        }
    }

    private void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
        
        // Write magic bytes
        writer.Write(MagicBytes);
        
        // Write character data
        writer.Write((int)CharacterType);
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(CharacterName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write((int)AgeCategory);
        writer.Write((int)Gender);
        writer.Write((int)Species);
        writer.Write(SupportsMorphing);
        writer.Write(Priority);
        
        // Write character parts
        writer.Write(_characterParts.Count);
        
        foreach (var part in _characterParts)
        {
            writer.Write(part.InstanceId);
            writer.Write((int)part.Category);
            var partNameBytes = System.Text.Encoding.UTF8.GetBytes(part.Name);
            writer.Write(partNameBytes.Length);
            writer.Write(partNameBytes);
            writer.Write((int)part.AgeCategory);
            writer.Write((int)part.Gender);
            writer.Write((int)part.Species);
            writer.Write(part.SortPriority);
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _disposed = true;
        }
    }
}
