namespace TS4Tools.Core.Helpers;

/// <summary>
/// Information about a helper tool
/// </summary>
public class HelperToolInfo
{
    /// <summary>
    /// Gets the internal ID/name of the helper tool
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display label for the helper tool
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the description of what the helper tool does
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command to execute
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Gets the argument template
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resource types this helper supports
    /// </summary>
    public IReadOnlyList<uint> SupportedResourceTypes { get; init; } = Array.Empty<uint>();

    /// <summary>
    /// Gets whether this helper requires read-only access
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets whether to ignore write timestamp
    /// </summary>
    public bool IgnoreWriteTimestamp { get; init; }

    /// <summary>
    /// Gets the full path to the helper executable (if resolved)
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Gets whether this helper tool is currently available/executable
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Gets additional properties from the .helper file
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Returns a string representation of the helper tool info
    /// </summary>
    /// <returns>String representation with helper label</returns>
    public override string ToString() => $"{Label} ({Id})";
}
