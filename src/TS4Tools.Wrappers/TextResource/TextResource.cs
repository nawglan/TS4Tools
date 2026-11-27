// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 46-77
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Text/XML resource for tuning files and other plain text content.
/// Resource Types: 0x03B33DDF (Tuning), 0x6017E896 (Tuning XML), and others.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs
///
/// Legacy keeps the resource stream untouched and only decodes it on demand (<c>Value</c> =
/// <c>new StreamReader(stream, true).ReadToEnd()</c>, lines 52-77); <c>Stream</c> returns the original
/// bytes. Types such as 0x02D5DF13 (JAZZ) in TextResources.txt hold binary data, so decoding and
/// re-encoding would corrupt them. This port likewise keeps the original bytes and only re-encodes once
/// <see cref="Text"/> or <see cref="HasBom"/> is assigned.
/// </remarks>
public sealed class TextResource : TypedResource
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private string? _text = string.Empty;
    private bool _hasBom;

    // Original bytes, returned verbatim until the text is changed (legacy Stream getter, line 55).
    private byte[]? _raw;

    /// <summary>
    /// The text content of this resource.
    /// </summary>
    public string Text
    {
        get => _text ??= Decode(_raw!);
        set
        {
            if (Text != value)
            {
                _text = value;
                _raw = null;
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
                _text = Text;
                _raw = null;
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
        _raw = data.ToArray();
        _text = null; // decoded lazily, like legacy Value
        _hasBom = data.StartsWith(Utf8Bom);
    }

    /// <summary>
    /// Decodes like legacy <c>new StreamReader(stream, true).ReadToEnd()</c> (TextResource.cs line 64):
    /// byte-order-mark detection, UTF-8 otherwise.
    /// </summary>
    private static string Decode(byte[] raw)
    {
        using var reader = new StreamReader(new MemoryStream(raw, writable: false), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        if (_raw is not null)
            return _raw;

        byte[] textBytes = Encoding.UTF8.GetBytes(Text);

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
        _raw = null;
        _hasBom = false;
    }

    /// <summary>
    /// Gets the number of lines in the text.
    /// </summary>
    public int LineCount => string.IsNullOrEmpty(Text) ? 0 : Text.Replace("\r\n", "\n").Split('\n').Length;

    /// <summary>
    /// Gets whether this appears to be XML content.
    /// </summary>
    public bool IsXml => Text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                      || Text.TrimStart().StartsWith('<');
}
