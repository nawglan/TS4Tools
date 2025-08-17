using System.Drawing;
using TS4Tools.Core.Interfaces.Resources.Specialized;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Implementation of color swatch resources for character customization.
/// Manages collections of color swatches used for recoloring CAS parts and materials.
/// </summary>
public sealed class SwatchResource : ISwatchResource, IDisposable
{
    /// <summary>
    /// The resource type identifier for SwatchResource.
    /// </summary>
    public const uint ResourceType = 0x53574154; // "SWAT" in hex

    private readonly List<ColorSwatch> _swatches;
    private readonly List<string> _contentFields;
    private bool _disposed;

    private string _swatchName;
    private SwatchCategory _category;

    /// <summary>
    /// Event raised when the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Initializes a new empty SwatchResource.
    /// </summary>
    /// <param name="name">The display name for this swatch collection</param>
    /// <param name="category">The category for this swatch collection</param>
    /// <param name="requestedApiVersion">The requested API version</param>
    public SwatchResource(string name, SwatchCategory category, int requestedApiVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(name);

        _swatchName = name;
        _category = category;
        RequestedApiVersion = requestedApiVersion;

        _swatches = new List<ColorSwatch>();
        _contentFields = new List<string> { "SwatchName", "Category", "SwatchCount" };
    }

    /// <summary>
    /// Creates a SwatchResource from binary data.
    /// </summary>
    /// <param name="requestedApiVersion">The requested API version</param>
    /// <param name="data">The binary data to parse</param>
    /// <returns>A new SwatchResource instance</returns>
    public static SwatchResource FromData(int requestedApiVersion, ReadOnlySpan<byte> data)
    {
        var resource = new SwatchResource("Unnamed", SwatchCategory.General, requestedApiVersion);
        resource.ParseFromBinary(data);
        return resource;
    }

    /// <summary>
    /// Creates a SwatchResource from a stream asynchronously.
    /// </summary>
    /// <param name="requestedApiVersion">The requested API version</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created SwatchResource</returns>
    public static async Task<SwatchResource> FromStreamAsync(
        int requestedApiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[stream.Length];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        return FromData(requestedApiVersion, buffer);
    }

    #region ISwatchResource Implementation

    /// <inheritdoc />
    public string SwatchName
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _swatchName;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _swatchName = value ?? throw new ArgumentNullException(nameof(value));
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public SwatchCategory Category
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _category;
        }
    }

    /// <inheritdoc />
    public IList<ColorSwatch> Swatches
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _swatches;
        }
    }

    /// <inheritdoc />
    public Task AddSwatchAsync(ColorSwatch swatch, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        _swatches.Add(swatch);
        OnResourceChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveSwatchAsync(int index, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (index < 0 || index >= _swatches.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _swatches.RemoveAt(index);
        OnResourceChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Color GetPrimaryColor(int swatchIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (swatchIndex < 0 || swatchIndex >= _swatches.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(swatchIndex));
        }

        return _swatches[swatchIndex].PrimaryColor;
    }

    /// <inheritdoc />
    public Color? GetSecondaryColor(int swatchIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (swatchIndex < 0 || swatchIndex >= _swatches.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(swatchIndex));
        }

        return _swatches[swatchIndex].SecondaryColor;
    }

    /// <inheritdoc />
    public Task SetColorsAsync(int swatchIndex, Color primary, Color? secondary = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (swatchIndex < 0 || swatchIndex >= _swatches.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(swatchIndex));
        }

        var existingSwatch = _swatches[swatchIndex];
        _swatches[swatchIndex] = existingSwatch with
        {
            PrimaryColor = primary,
            SecondaryColor = secondary
        };

        OnResourceChanged();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<ColorSwatch> GetSwatchesByCategory(SwatchCategory category)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _swatches.Where(s => s.Category == category);
    }

    /// <inheritdoc />
    public Task<ColorSwatch?> FindClosestMatchAsync(Color targetColor, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_swatches.Count == 0)
        {
            return Task.FromResult<ColorSwatch?>(null);
        }

        var closestSwatch = _swatches
            .OrderBy(s => s.GetColorDistance(targetColor))
            .First();

        return Task.FromResult<ColorSwatch?>(closestSwatch);
    }

    #endregion

    #region IResource Implementation

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new MemoryStream(ToByteArray());
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ToByteArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _contentFields.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return index switch
            {
                0 => TypedValue.Create(SwatchName),
                1 => TypedValue.Create(Category.ToString()),
                2 => TypedValue.Create(_swatches.Count.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            switch (index)
            {
                case 0:
                    _swatchName = value.GetValue<string>() ?? string.Empty;
                    break;
                case 1:
                    if (Enum.TryParse<SwatchCategory>(value.GetValue<string>(), out var category))
                        _category = category;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return name switch
            {
                "SwatchName" => TypedValue.Create(SwatchName),
                "Category" => TypedValue.Create(Category.ToString()),
                "SwatchCount" => TypedValue.Create(_swatches.Count.ToString()),
                _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
            };
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            switch (name)
            {
                case "SwatchName":
                    _swatchName = value.GetValue<string>() ?? string.Empty;
                    break;
                case "Category":
                    if (Enum.TryParse<SwatchCategory>(value.GetValue<string>(), out var category))
                        _category = category;
                    break;
                default:
                    throw new ArgumentException($"Unknown field: {name}", nameof(name));
            }
            OnResourceChanged();
        }
    }

    #endregion

    #region Binary Serialization

    private void ParseFromBinary(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16) // Minimum size for header
        {
            throw new ArgumentException("Data too short for swatch resource", nameof(data));
        }

        using var reader = new BinaryReader(new MemoryStream(data.ToArray()));

        // Read header
        var version = reader.ReadInt32(); // Version (reserved for future use)

        // Read swatch name
        var nameLength = reader.ReadInt32();
        var nameBytes = reader.ReadBytes(nameLength);
        _swatchName = System.Text.Encoding.UTF8.GetString(nameBytes);

        // Read category
        _category = (SwatchCategory)reader.ReadInt32();

        // Read swatch count
        var swatchCount = reader.ReadInt32();

        // Read swatches
        _swatches.Clear();
        for (int i = 0; i < swatchCount; i++)
        {
            // Read primary color
            var primaryArgb = reader.ReadUInt32();
            var primaryColor = Color.FromArgb((int)primaryArgb);

            // Read secondary color (if available)
            Color? secondaryColor = null;
            var hasSecondary = reader.ReadBoolean();
            if (hasSecondary)
            {
                var secondaryArgb = reader.ReadUInt32();
                secondaryColor = Color.FromArgb((int)secondaryArgb);
            }

            // Read name
            var swatchNameLength = reader.ReadInt32();
            var swatchNameBytes = reader.ReadBytes(swatchNameLength);
            var swatchName = System.Text.Encoding.UTF8.GetString(swatchNameBytes);

            // Read category
            var swatchCategory = (SwatchCategory)reader.ReadInt32();

            var swatch = new ColorSwatch(primaryColor, secondaryColor, swatchName, swatchCategory);
            _swatches.Add(swatch);
        }
    }

    private byte[] ToByteArray()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(1); // Version

        // Write swatch name
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(_swatchName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);

        // Write category
        writer.Write((int)_category);

        // Write swatch count
        writer.Write(_swatches.Count);

        // Write swatches
        foreach (var swatch in _swatches)
        {
            // Write primary color
            writer.Write((uint)swatch.PrimaryColor.ToArgb());

            // Write secondary color
            writer.Write(swatch.SecondaryColor.HasValue);
            if (swatch.SecondaryColor.HasValue)
            {
                writer.Write((uint)swatch.SecondaryColor.Value.ToArgb());
            }

            // Write name
            var swatchNameBytes = System.Text.Encoding.UTF8.GetBytes(swatch.Name);
            writer.Write(swatchNameBytes.Length);
            writer.Write(swatchNameBytes);

            // Write category
            writer.Write((int)swatch.Category);
        }

        return stream.ToArray();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the resources used by this SwatchResource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _swatches.Clear();
        _disposed = true;
    }

    #endregion

    #region ToString

    /// <inheritdoc />
    public override string ToString()
    {
        if (_disposed)
        {
            return "SwatchResource (Disposed)";
        }

        return $"SwatchResource (Name: {SwatchName}, Category: {Category}, Swatches: {_swatches.Count:N0})";
    }

    #endregion
}
