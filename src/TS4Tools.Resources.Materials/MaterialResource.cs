using System.ComponentModel;
using System.Runtime.CompilerServices;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Materials;

/// <summary>
/// Represents a material resource (resource type 0x545AC67A).
/// Materials control the visual appearance of 3D objects, including textures, lighting, and surface properties.
/// Also known as SWB (Surface Wetness Buffer) resources.
/// </summary>
public sealed class MaterialResource : IMaterialResource, INotifyPropertyChanged
{
    private uint _materialId;
    private string _shaderType = "default";
    private bool _hasTransparency;
    private byte[] _data = Array.Empty<byte>();
    private readonly List<string> _contentFields = [];

    /// <summary>
    /// Gets the material identifier/hash if available.
    /// </summary>
    public uint MaterialId
    {
        get => _materialId;
        private set => SetProperty(ref _materialId, value);
    }

    /// <summary>
    /// Gets the shader type used by this material.
    /// </summary>
    public string ShaderType
    {
        get => _shaderType;
        private set => SetProperty(ref _shaderType, value ?? string.Empty);
    }

    /// <summary>
    /// Gets whether this material has transparency/alpha channel.
    /// </summary>
    public bool HasTransparency
    {
        get => _hasTransparency;
        private set => SetProperty(ref _hasTransparency, value);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Data => _data;

    /// <inheritdoc />
    public long Size => _data.Length;

    /// <inheritdoc />
    public Stream Stream => new MemoryStream(_data);

    /// <inheritdoc />
    public byte[] AsBytes => _data.ToArray();

    /// <inheritdoc />
    public int RequestedApiVersion { get; private set; } = 1;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();

    /// <inheritdoc />
    public TypedValue this[int index] 
    { 
        get => index < _contentFields.Count ? new TypedValue(typeof(string), _contentFields[index]) : new TypedValue(typeof(object), null);
        set => throw new NotSupportedException("Material resources do not support content field modification.");
    }

    /// <inheritdoc />
    public TypedValue this[string name] 
    { 
        get 
        {
            var field = _contentFields.FirstOrDefault(f => f.Equals(name, StringComparison.OrdinalIgnoreCase));
            return new TypedValue(typeof(string), field);
        }
        set => throw new NotSupportedException("Material resources do not support content field modification.");
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialResource"/> class.
    /// </summary>
    public MaterialResource()
    {
        ShaderType = "default";
    }

    /// <summary>
    /// Loads material data from the provided data array.
    /// </summary>
    /// <param name="data">The material data to load.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task LoadFromDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        _data = data.ToArray();
        
        if (data.Length >= 16)
        {
            using var stream = new MemoryStream(data.ToArray());
            using var reader = new BinaryReader(stream);
            
            var signature = reader.ReadUInt32();
            var version = reader.ReadUInt32();
            MaterialId = reader.ReadUInt32();
            var flags = reader.ReadUInt32();
            
            HasTransparency = (flags & 0x1) != 0;
            
            // Read shader type if available
            if (stream.Length > 16)
            {
                var shaderTypeLength = Math.Min(32, (int)(stream.Length - 16));
                if (shaderTypeLength > 0)
                {
                    var shaderBytes = reader.ReadBytes(shaderTypeLength);
                    var nullIndex = Array.IndexOf(shaderBytes, (byte)0);
                    if (nullIndex >= 0)
                    {
                        shaderBytes = shaderBytes[..nullIndex];
                    }
                    ShaderType = System.Text.Encoding.UTF8.GetString(shaderBytes);
                }
            }
        }
        
        OnResourceChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Saves the material data to a byte array.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The material data as a byte array.</returns>
    public async Task<ReadOnlyMemory<byte>> SaveToDataAsync(CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // Write basic material header (placeholder format)
        writer.Write(0x4D544C52u); // "MTLR" signature
        writer.Write(1u); // Version
        writer.Write(MaterialId);
        
        uint flags = 0;
        if (HasTransparency) flags |= 0x1;
        writer.Write(flags);
        
        // Write shader type
        var shaderBytes = System.Text.Encoding.UTF8.GetBytes(ShaderType);
        writer.Write(shaderBytes);
        if (shaderBytes.Length < 32)
        {
            writer.Write(new byte[32 - shaderBytes.Length]); // Padding
        }
        
        var result = stream.ToArray();
        _data = result;
        
        return await Task.FromResult<ReadOnlyMemory<byte>>(result);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to dispose
        GC.SuppressFinalize(this);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        if (propertyName == nameof(Data))
        {
            OnPropertyChanged(nameof(Size));
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }
}
