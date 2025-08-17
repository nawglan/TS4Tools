using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Text;

/// <summary>
/// Implementation of text-based resources including XML, JSON, and plain text content.
/// Handles various configuration files, tuning data, and script content in Sims 4.
/// </summary>
public sealed class TextResource : ITextResource, IApiVersion, IContentFields, IDisposable, INotifyPropertyChanged
{
    private readonly ILogger<TextResource> _logger;
    private readonly int _requestedApiVersion;
    private string _content = string.Empty;
    private Encoding _encoding = Encoding.UTF8;
    private LineEndingStyle? _lineEndings;
    private bool? _isXml;
    private bool? _isJson;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResource"/> class.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestedApiVersion">The requested API version.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="resourceKey"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public TextResource(IResourceKey resourceKey, ILogger<TextResource> logger, int requestedApiVersion = 1)
    {
        ResourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestedApiVersion = requestedApiVersion;
        _logger.LogDebug("Created TextResource for key {ResourceKey}", resourceKey);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResource"/> class from a data stream.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <param name="data">The resource data stream.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestedApiVersion">The requested API version.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public TextResource(IResourceKey resourceKey, Stream data, ILogger<TextResource> logger, int requestedApiVersion = 1)
        : this(resourceKey, logger, requestedApiVersion)
    {
        ArgumentNullException.ThrowIfNull(data);
        LoadFromStream(data);
    }

    /// <inheritdoc />
    public IResourceKey ResourceKey { get; }

    /// <inheritdoc />
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value ?? string.Empty;
                InvalidateCache();
                OnPropertyChanged();
                _logger.LogDebug("Content updated for resource {ResourceKey}, length: {Length}",
                    ResourceKey, _content.Length);
            }
        }
    }

    /// <inheritdoc />
    public Encoding Encoding => _encoding;

    /// <inheritdoc />
    public bool IsXml
    {
        get
        {
            if (_isXml is null)
            {
                _isXml = DetectXmlContent();
            }
            return _isXml.Value;
        }
    }

    /// <inheritdoc />
    public bool IsJson
    {
        get
        {
            if (_isJson is null)
            {
                _isJson = DetectJsonContent();
            }
            return _isJson.Value;
        }
    }

    /// <inheritdoc />
    public LineEndingStyle LineEndings
    {
        get
        {
            if (_lineEndings is null)
            {
                _lineEndings = DetectLineEndings();
            }
            return _lineEndings.Value;
        }
    }

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            var bytes = _encoding.GetBytes(_content);
            return new MemoryStream(bytes);
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return _encoding.GetBytes(_content);
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public int RequestedApiVersion => _requestedApiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IContentFields Implementation

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Content),
        nameof(Encoding),
        nameof(IsXml),
        nameof(IsJson),
        nameof(LineEndings)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Content) => new TypedValue(typeof(string), Content),
            nameof(Encoding) => new TypedValue(typeof(Encoding), Encoding),
            nameof(IsXml) => new TypedValue(typeof(bool), IsXml),
            nameof(IsJson) => new TypedValue(typeof(bool), IsJson),
            nameof(LineEndings) => new TypedValue(typeof(LineEndingStyle), LineEndings),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Text resource fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Content)],
            1 => this[nameof(Encoding)],
            2 => this[nameof(IsXml)],
            3 => this[nameof(IsJson)],
            4 => this[nameof(LineEndings)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-4, got {index}")
        };
        set => throw new NotSupportedException("Text resource fields are read-only via integer indexer");
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    /// <inheritdoc />
    public XmlDocument? AsXmlDocument()
    {
        if (!IsXml)
        {
            return null;
        }

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(_content);
            _logger.LogDebug("Successfully parsed XML content for resource {ResourceKey}", ResourceKey);
            return xmlDoc;
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML content for resource {ResourceKey}", ResourceKey);
            throw;
        }
    }

    /// <inheritdoc />
    public JsonElement? AsJsonElement()
    {
        if (!IsJson)
        {
            return null;
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(_content);
            _logger.LogDebug("Successfully parsed JSON content for resource {ResourceKey}", ResourceKey);
            return jsonDoc.RootElement;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON content for resource {ResourceKey}", ResourceKey);
            throw;
        }
    }

    /// <inheritdoc />
    public string NormalizeLineEndings(LineEndingStyle lineEndingStyle)
    {
        var targetLineEnding = lineEndingStyle switch
        {
            LineEndingStyle.CrLf => "\r\n",
            LineEndingStyle.Lf => "\n",
            LineEndingStyle.Cr => "\r",
            _ => Environment.NewLine
        };

        // First normalize all line endings to \n, then apply target
        var normalized = _content
            .Replace("\r\n", "\n")  // Windows to Unix
            .Replace("\r", "\n");   // Mac to Unix

        if (lineEndingStyle != LineEndingStyle.Lf)
        {
            normalized = normalized.Replace("\n", targetLineEnding);
        }

        _logger.LogDebug("Normalized line endings for resource {ResourceKey} to {Style}",
            ResourceKey, lineEndingStyle);

        return normalized;
    }

    /// <inheritdoc />
    public byte[] ToBytes(Encoding targetEncoding)
    {
        ArgumentNullException.ThrowIfNull(targetEncoding);

        var bytes = targetEncoding.GetBytes(_content);
        _logger.LogDebug("Converted resource {ResourceKey} to {Encoding}, {ByteCount} bytes",
            ResourceKey, targetEncoding.EncodingName, bytes.Length);

        return bytes;
    }

    /// <summary>
    /// Converts the text resource to binary format asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the binary data</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public Task<byte[]> ToBinaryAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = _encoding.GetBytes(_content);
        _logger.LogDebug("Serialized TextResource {ResourceKey} to {ByteCount} bytes using {Encoding}",
            ResourceKey, bytes.Length, _encoding.EncodingName);

        return Task.FromResult(bytes);
    }

    /// <summary>
    /// Loads content from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the async operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ThrowIfDisposed();

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        _content = await reader.ReadToEndAsync(cancellationToken);
        _encoding = reader.CurrentEncoding;

        InvalidateCache();
        OnResourceChanged();

        _logger.LogDebug("Loaded TextResource {ResourceKey} from stream: {Length} characters, encoding: {Encoding}",
            ResourceKey, _content.Length, _encoding.EncodingName);
    }

    private void LoadFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ThrowIfDisposed();

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        _content = reader.ReadToEnd();
        _encoding = reader.CurrentEncoding;

        InvalidateCache();
        OnResourceChanged();

        _logger.LogDebug("Loaded TextResource {ResourceKey} from stream: {Length} characters, encoding: {Encoding}",
            ResourceKey, _content.Length, _encoding.EncodingName);
    }

    private bool DetectXmlContent()
    {
        if (string.IsNullOrWhiteSpace(_content))
        {
            return false;
        }

        var trimmed = _content.TrimStart();
        return trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<", StringComparison.Ordinal);
    }

    private bool DetectJsonContent()
    {
        if (string.IsNullOrWhiteSpace(_content))
        {
            return false;
        }

        var trimmed = _content.Trim();
        return (trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
               (trimmed.StartsWith('[') && trimmed.EndsWith(']'));
    }

    private LineEndingStyle DetectLineEndings()
    {
        if (string.IsNullOrEmpty(_content))
        {
            return LineEndingStyle.Lf; // Default
        }

        var hasCrLf = _content.Contains("\r\n");
        var hasStandaloneLf = false;
        var hasStandaloneCr = false;

        // Check for standalone LF (not part of CRLF)
        var lfIndex = _content.IndexOf('\n');
        while (lfIndex != -1)
        {
            if (lfIndex == 0 || _content[lfIndex - 1] != '\r')
            {
                hasStandaloneLf = true;
                break;
            }
            lfIndex = _content.IndexOf('\n', lfIndex + 1);
        }

        // Check for standalone CR (not part of CRLF)
        var crIndex = _content.IndexOf('\r');
        while (crIndex != -1)
        {
            if (crIndex == _content.Length - 1 || _content[crIndex + 1] != '\n')
            {
                hasStandaloneCr = true;
                break;
            }
            crIndex = _content.IndexOf('\r', crIndex + 1);
        }

        var styleCount = 0;
        if (hasCrLf) styleCount++;
        if (hasStandaloneLf) styleCount++;
        if (hasStandaloneCr) styleCount++;

        return styleCount switch
        {
            0 => LineEndingStyle.Lf, // No line endings, default to LF
            1 when hasCrLf => LineEndingStyle.CrLf,
            1 when hasStandaloneLf => LineEndingStyle.Lf,
            1 when hasStandaloneCr => LineEndingStyle.Cr,
            _ => LineEndingStyle.Mixed
        };
    }

    private void InvalidateCache()
    {
        _lineEndings = null;
        _isXml = null;
        _isJson = null;
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the resources used by this TextResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _content = string.Empty;
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}
