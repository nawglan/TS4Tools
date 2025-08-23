using System.Text;
using System.Xml;
using System.Xml.Linq;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Implementation of IJazzResource for handling JAZZ animation state machine resources.
/// JAZZ resources contain XML data that defines animation state machines and transitions.
/// </summary>
public class JazzResource : IJazzResource
{
    private readonly List<string> _contentFields =
    [
        "XmlContent",
        "StateMachineName", 
        "FormatVersion"
    ];

    private MemoryStream? _stream;
    private bool _disposed;
    private string _xmlContent = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="JazzResource"/> class.
    /// </summary>
    public JazzResource()
    {
        XmlContent = string.Empty;
        StateMachineName = string.Empty;
        FormatVersion = 1;
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JazzResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public JazzResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        LoadFromStreamAsync(stream).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public string XmlContent
    {
        get => _xmlContent;
        set
        {
            if (_xmlContent != value)
            {
                _xmlContent = value;
                ExtractStateMachineName();
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public string StateMachineName { get; set; } = string.Empty;

    /// <inheritdoc />
    public int FormatVersion { get; set; } = 1;

    /// <inheritdoc />
    public bool IsValidXml
    {
        get
        {
            if (string.IsNullOrWhiteSpace(XmlContent))
                return false;

            try
            {
                XDocument.Parse(XmlContent);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JazzResource));

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
                throw new ObjectDisposedException(nameof(JazzResource));

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
    public async Task<IReadOnlyList<string>> ValidateXmlAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(XmlContent))
        {
            errors.Add("XML content is empty or whitespace");
            return errors.AsReadOnly();
        }

        try
        {
            await Task.Run(() =>
            {
                var doc = XDocument.Parse(XmlContent);
                
                // Basic validation for JAZZ structure
                if (doc.Root == null)
                {
                    errors.Add("XML document has no root element");
                    return;
                }

                // Check for common JAZZ elements
                var rootName = doc.Root.Name.LocalName.ToLowerInvariant();
                if (!rootName.Contains("jazz") && !rootName.Contains("state") && !rootName.Contains("animation"))
                {
                    errors.Add($"Unexpected root element '{doc.Root.Name}' for JAZZ resource");
                }
            }).ConfigureAwait(false);
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
        }

        return errors.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            
            XmlContent = content;
            
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
            throw new InvalidOperationException($"Failed to load JAZZ resource from stream: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            var bytes = Encoding.UTF8.GetBytes(XmlContent);
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save JAZZ resource to stream: {ex.Message}", ex);
        }
    }

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
            "XmlContent" => TypedValue.Create(XmlContent),
            "StateMachineName" => TypedValue.Create(StateMachineName),
            "FormatVersion" => TypedValue.Create(FormatVersion),
            _ => TypedValue.Create<object?>(null)
        };
        set
        {
            switch (fieldName)
            {
                case "XmlContent":
                    XmlContent = value.GetValue<string>() ?? string.Empty;
                    break;
                case "StateMachineName":
                    StateMachineName = value.GetValue<string>() ?? string.Empty;
                    break;
                case "FormatVersion":
                    FormatVersion = value.GetValue<int>();
                    break;
            }
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
    /// Extracts the state machine name from the XML content.
    /// </summary>
    private void ExtractStateMachineName()
    {
        if (string.IsNullOrWhiteSpace(XmlContent))
        {
            StateMachineName = string.Empty;
            return;
        }

        try
        {
            var doc = XDocument.Parse(XmlContent);
            if (doc.Root != null)
            {
                // Try to find name attribute on root element
                var nameAttr = doc.Root.Attribute("name") ?? doc.Root.Attribute("Name");
                if (nameAttr != null)
                {
                    StateMachineName = nameAttr.Value;
                    return;
                }

                // Try to find name element
                var nameElement = doc.Root.Element("name") ?? doc.Root.Element("Name");
                if (nameElement != null)
                {
                    StateMachineName = nameElement.Value;
                    return;
                }

                // Use root element name as fallback
                StateMachineName = doc.Root.Name.LocalName;
            }
        }
        catch
        {
            // If XML parsing fails, leave the current name unchanged
        }
    }
}
