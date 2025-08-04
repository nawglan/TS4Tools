using System.ComponentModel;
using System.Xml.Serialization;

namespace TS4Tools.Resources.Common.CatalogTags;

/// <summary>
/// Represents a category tag that contains index and value.
/// Modernized version of the legacy Tag class with nullable reference types and improved patterns.
/// </summary>
[TypeConverter(typeof(TagTypeConverter))]
public sealed record class Tag
{
    /// <summary>
    /// Gets or sets the index of the tag.
    /// </summary>
    [XmlAttribute("index")]
    public uint Index { get; init; }

    /// <summary>
    /// Gets or sets the value of the tag.
    /// </summary>
    [XmlAttribute("value")]
    public string Value { get; init; } = "unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> class.
    /// </summary>
    public Tag()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> class with the specified index and value.
    /// </summary>
    /// <param name="index">The tag index.</param>
    /// <param name="value">The tag value.</param>
    public Tag(uint index, string value)
    {
        Index = index;
        Value = value ?? "unknown";
    }

    /// <summary>
    /// Returns a string representation of this tag.
    /// </summary>
    public override string ToString() => $"{Value} (0x{Index:X8})";

    /// <summary>
    /// Determines whether this tag represents an unknown/default tag.
    /// </summary>
    /// <returns>True if this is an unknown tag with default value.</returns>
    public bool IsUnknown() => Value == "unknown";
}

/// <summary>
/// Type converter for Tag objects to support property grid editing.
/// </summary>
public sealed class TagTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            // Try to parse format like "TagName (0x12345678)"
            var match = System.Text.RegularExpressions.Regex.Match(stringValue, @"^(.+?)\s*\(0x([0-9A-Fa-f]+)\)$");
            if (match.Success)
            {
                var tagValue = match.Groups[1].Value.Trim();
                if (uint.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, culture, out var index))
                {
                    return new Tag(index, tagValue);
                }
            }
            
            // Fallback to treating the entire string as the value
            return new Tag(0, stringValue);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is Tag tag && destinationType == typeof(string))
        {
            return tag.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
