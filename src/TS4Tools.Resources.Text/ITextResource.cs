using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Text;

/// <summary>
/// Interface for text-based resources including XML, JSON, and plain text content.
/// Represents various configuration files, tuning data, and script content in Sims 4.
/// </summary>
public interface ITextResource : IResource
{
    /// <summary>
    /// Gets or sets the text content of the resource.
    /// </summary>
    /// <value>
    /// The complete text content as a string, preserving original encoding and line endings.
    /// </value>
    string Content { get; set; }

    /// <summary>
    /// Gets the detected or specified encoding of the text content.
    /// </summary>
    /// <value>
    /// The text encoding used for the content. Common encodings include UTF-8, UTF-16, and Windows-1252.
    /// </value>
    System.Text.Encoding Encoding { get; }

    /// <summary>
    /// Gets a value indicating whether the content appears to be XML.
    /// </summary>
    /// <value>
    /// <c>true</c> if the content starts with XML declaration or root element; otherwise, <c>false</c>.
    /// </value>
    bool IsXml { get; }

    /// <summary>
    /// Gets a value indicating whether the content appears to be JSON.
    /// </summary>
    /// <value>
    /// <c>true</c> if the content appears to be valid JSON format; otherwise, <c>false</c>.
    /// </value>
    bool IsJson { get; }

    /// <summary>
    /// Gets the line ending style used in the content.
    /// </summary>
    /// <value>
    /// The line ending style (CRLF, LF, or CR) detected in the content.
    /// </value>
    LineEndingStyle LineEndings { get; }

    /// <summary>
    /// Gets the content as an XML document if the content is valid XML.
    /// </summary>
    /// <returns>
    /// An <see cref="System.Xml.XmlDocument"/> if the content is valid XML; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown when the content is not valid XML.
    /// </exception>
    System.Xml.XmlDocument? AsXmlDocument();

    /// <summary>
    /// Gets the content as a JSON element if the content is valid JSON.
    /// </summary>
    /// <returns>
    /// A <see cref="System.Text.Json.JsonElement"/> if the content is valid JSON; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown when the content is not valid JSON.
    /// </exception>
    System.Text.Json.JsonElement? AsJsonElement();

    /// <summary>
    /// Normalizes line endings to the specified style.
    /// </summary>
    /// <param name="lineEndingStyle">The target line ending style.</param>
    /// <returns>
    /// The content with normalized line endings.
    /// </returns>
    string NormalizeLineEndings(LineEndingStyle lineEndingStyle);

    /// <summary>
    /// Converts the content to the specified encoding.
    /// </summary>
    /// <param name="targetEncoding">The target encoding.</param>
    /// <returns>
    /// The byte array representation of the content in the target encoding.
    /// </returns>
    byte[] ToBytes(System.Text.Encoding targetEncoding);
}

/// <summary>
/// Specifies the line ending style used in text content.
/// </summary>
public enum LineEndingStyle
{
    /// <summary>
    /// Carriage Return + Line Feed (Windows style: \r\n).
    /// </summary>
    CrLf,

    /// <summary>
    /// Line Feed only (Unix/Linux style: \n).
    /// </summary>
    Lf,

    /// <summary>
    /// Carriage Return only (Classic Mac style: \r).
    /// </summary>
    Cr,

    /// <summary>
    /// Mixed line endings detected.
    /// </summary>
    Mixed
}
