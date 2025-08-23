using System.IO;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Implementation of ILightResource for handling light resources.
/// Light resources define lighting properties for scenes and objects.
/// </summary>
public class LightResource : ILightResource
{
    private static readonly byte[] MagicBytes = [0x4C, 0x49, 0x54, 0x45]; // "LITE"

    private readonly List<string> _contentFields =
    [
        "LightType",
        "Color",
        "Intensity",
        "Range",
        "Falloff",
        "CastsShadows",
        "Position",
        "Direction",
        "ConeAngle",
        "IsEnabled",
        "Priority"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LightResource"/> class.
    /// </summary>
    public LightResource()
    {
        LightType = LightType.Point;
        Color = LightColor.White;
        Intensity = 1.0f;
        Range = 10.0f;
        Falloff = LightFalloff.Quadratic;
        CastsShadows = true;
        Position = Vector3.Zero;
        Direction = Vector3.Forward;
        ConeAngle = 45.0f;
        IsEnabled = true;
        Priority = 0;
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public LightResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        LoadFromStreamAsync(stream).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public LightType LightType { get; set; }

    /// <inheritdoc />
    public LightColor Color { get; set; }

    /// <inheritdoc />
    public float Intensity { get; set; }

    /// <inheritdoc />
    public float Range { get; set; }

    /// <inheritdoc />
    public LightFalloff Falloff { get; set; }

    /// <inheritdoc />
    public bool CastsShadows { get; set; }

    /// <inheritdoc />
    public Vector3 Position { get; set; }

    /// <inheritdoc />
    public Vector3 Direction { get; set; }

    /// <inheritdoc />
    public float ConeAngle { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LightResource));

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
                throw new ObjectDisposedException(nameof(LightResource));

            using var ms = new MemoryStream();
            SaveToStreamAsync(ms).ConfigureAwait(false).GetAwaiter().GetResult();
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; set; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index < _contentFields.Count 
            ? this[_contentFields[index]]
            : TypedValue.Create<object?>(null);
        set
        {
            if (index < _contentFields.Count)
                this[_contentFields[index]] = value;
        }
    }

    /// <inheritdoc />
    public TypedValue this[string fieldName]
    {
        get => fieldName switch
        {
            "LightType" => TypedValue.Create(LightType),
            "Color" => TypedValue.Create(Color),
            "Intensity" => TypedValue.Create(Intensity),
            "Range" => TypedValue.Create(Range),
            "Falloff" => TypedValue.Create(Falloff),
            "CastsShadows" => TypedValue.Create(CastsShadows),
            "Position" => TypedValue.Create(Position),
            "Direction" => TypedValue.Create(Direction),
            "ConeAngle" => TypedValue.Create(ConeAngle),
            "IsEnabled" => TypedValue.Create(IsEnabled),
            "Priority" => TypedValue.Create(Priority),
            _ => TypedValue.Create<object?>(null)
        };
        set
        {
            switch (fieldName)
            {
                case "LightType":
                    LightType = value.GetValue<LightType>();
                    break;
                case "Color":
                    Color = value.GetValue<LightColor>();
                    break;
                case "Intensity":
                    Intensity = value.GetValue<float>();
                    break;
                case "Range":
                    Range = value.GetValue<float>();
                    break;
                case "Falloff":
                    Falloff = value.GetValue<LightFalloff>();
                    break;
                case "CastsShadows":
                    CastsShadows = value.GetValue<bool>();
                    break;
                case "Position":
                    Position = value.GetValue<Vector3>();
                    break;
                case "Direction":
                    Direction = value.GetValue<Vector3>();
                    break;
                case "ConeAngle":
                    ConeAngle = value.GetValue<float>();
                    break;
                case "IsEnabled":
                    IsEnabled = value.GetValue<bool>();
                    break;
                case "Priority":
                    Priority = value.GetValue<int>();
                    break;
            }
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Read LITE header magic bytes
            var magic = reader.ReadBytes(4);
            if (!magic.SequenceEqual(MagicBytes))
            {
                // Reset stream position for fallback binary reading
                stream.Position = 0;
                await ReadBinaryFormat(reader, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Read LITE format version
            var version = reader.ReadUInt32();
            
            // Read light properties based on version
            LightType = (LightType)reader.ReadInt32();
            
            // Read color
            var r = reader.ReadSingle();
            var g = reader.ReadSingle();
            var b = reader.ReadSingle();
            Color = new LightColor(r, g, b);
            
            Intensity = reader.ReadSingle();
            Range = reader.ReadSingle();
            Falloff = (LightFalloff)reader.ReadInt32();
            CastsShadows = reader.ReadBoolean();
            
            // Read position
            var px = reader.ReadSingle();
            var py = reader.ReadSingle();
            var pz = reader.ReadSingle();
            Position = new Vector3(px, py, pz);
            
            // Read direction
            var dx = reader.ReadSingle();
            var dy = reader.ReadSingle();
            var dz = reader.ReadSingle();
            Direction = new Vector3(dx, dy, dz);
            
            ConeAngle = reader.ReadSingle();
            IsEnabled = reader.ReadBoolean();
            Priority = reader.ReadInt32();

            // Update our internal stream
            _stream?.Dispose();
            _stream = new MemoryStream();
            stream.Position = 0;
            await stream.CopyToAsync(_stream, cancellationToken).ConfigureAwait(false);
            _stream.Position = 0;

            OnResourceChanged();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load light resource from stream: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Write LITE header
            writer.Write(MagicBytes);
            writer.Write(1u); // Version
            
            // Write light properties
            writer.Write((int)LightType);
            writer.Write(Color.Red);
            writer.Write(Color.Green);
            writer.Write(Color.Blue);
            writer.Write(Intensity);
            writer.Write(Range);
            writer.Write((int)Falloff);
            writer.Write(CastsShadows);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Position.Z);
            writer.Write(Direction.X);
            writer.Write(Direction.Y);
            writer.Write(Direction.Z);
            writer.Write(ConeAngle);
            writer.Write(IsEnabled);
            writer.Write(Priority);

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save light resource to stream: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the resource and cleans up managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resource and cleans up managed resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    protected virtual void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reads light data in a fallback binary format for unknown structures.
    /// </summary>
    private async Task ReadBinaryFormat(BinaryReader reader, CancellationToken cancellationToken)
    {
        // Simple fallback: read whatever data is available and set defaults
        var availableBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        
        if (availableBytes >= 4)
        {
            // Try to interpret first 4 bytes as light type
            var possibleType = reader.ReadInt32();
            if (Enum.IsDefined(typeof(LightType), possibleType))
                LightType = (LightType)possibleType;
        }
        
        if (availableBytes >= 16) // 3 floats for color + 1 for intensity
        {
            try
            {
                var r = reader.ReadSingle();
                var g = reader.ReadSingle();
                var b = reader.ReadSingle();
                if (r >= 0 && r <= 1 && g >= 0 && g <= 1 && b >= 0 && b <= 1)
                    Color = new LightColor(r, g, b);
                    
                var intensity = reader.ReadSingle();
                if (intensity >= 0 && intensity <= 10) // Reasonable intensity range
                    Intensity = intensity;
            }
            catch
            {
                // Keep defaults if parsing fails
            }
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }
}
