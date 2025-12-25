using System.Text;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Text/XML resource for tuning files and other plain text content.
/// Resource Types: 0x03B33DDF (Tuning), 0x6017E896 (Tuning XML), and others.
/// </summary>
[ResourceHandler(0x03B33DDF)] // Tuning
[ResourceHandler(0x6017E896)] // Tuning XML
public sealed class TextResource : TypedResource
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private string _text = string.Empty;
    private bool _hasBom;

    /// <summary>
    /// The text content of this resource.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Whether the original data had a UTF-8 BOM.
    /// </summary>
    public bool HasBom
    {
        get => _hasBom;
        set
        {
            if (_hasBom != value)
            {
                _hasBom = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Gets the text encoding (always UTF-8).
    /// </summary>
    public static Encoding Encoding => Encoding.UTF8;

    /// <summary>
    /// Creates a new text resource by parsing data.
    /// </summary>
    public TextResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            _text = string.Empty;
            _hasBom = false;
            return;
        }

        // Check for UTF-8 BOM
        if (data.Length >= 3 && data[0] == Utf8Bom[0] && data[1] == Utf8Bom[1] && data[2] == Utf8Bom[2])
        {
            _hasBom = true;
            data = data[3..];
        }
        else
        {
            _hasBom = false;
        }

        _text = Encoding.UTF8.GetString(data);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        byte[] textBytes = Encoding.UTF8.GetBytes(_text);

        if (_hasBom)
        {
            byte[] result = new byte[Utf8Bom.Length + textBytes.Length];
            Utf8Bom.CopyTo(result.AsSpan());
            textBytes.CopyTo(result.AsSpan(Utf8Bom.Length));
            return result;
        }

        return textBytes;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _text = string.Empty;
        _hasBom = false;
    }

    /// <summary>
    /// Gets the number of lines in the text.
    /// </summary>
    public int LineCount => string.IsNullOrEmpty(_text) ? 0 : _text.Replace("\r\n", "\n").Split('\n').Length;

    /// <summary>
    /// Gets whether this appears to be XML content.
    /// </summary>
    public bool IsXml => _text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                      || _text.TrimStart().StartsWith('<');
}
