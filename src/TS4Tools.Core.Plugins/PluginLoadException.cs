namespace TS4Tools.Core.Plugins;

/// <summary>
/// Exception thrown when plugin loading fails.
/// This provides specific error handling for the modern plugin system.
/// </summary>
public class PluginLoadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadException"/> class.
    /// </summary>
    public PluginLoadException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PluginLoadException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// The path to the assembly that failed to load, if available.
    /// </summary>
    public string? AssemblyPath { get; init; }

    /// <summary>
    /// Additional context about the plugin loading failure.
    /// </summary>
    public string? Context { get; init; }
}
