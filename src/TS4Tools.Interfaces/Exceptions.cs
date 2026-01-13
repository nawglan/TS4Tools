namespace TS4Tools;

/// <summary>
/// Exception thrown when a package file has an invalid format.
/// </summary>
public class PackageFormatException : Exception
{
    /// <inheritdoc />
    public PackageFormatException() { }
    /// <inheritdoc />
    public PackageFormatException(string message) : base(message) { }
    /// <inheritdoc />
    public PackageFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a resource has an invalid format.
/// </summary>
public class ResourceFormatException : Exception
{
    /// <inheritdoc />
    public ResourceFormatException() { }
    /// <inheritdoc />
    public ResourceFormatException(string message) : base(message) { }
    /// <inheritdoc />
    public ResourceFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a resource is not found in a package.
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// The key of the resource that was not found.
    /// </summary>
    public ResourceKey Key { get; }

    /// <summary>
    /// Initializes a new instance with the specified resource key.
    /// </summary>
    /// <param name="key">The key of the resource that was not found.</param>
    public ResourceNotFoundException(ResourceKey key)
        : base($"Resource not found: {key}")
    {
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance with the specified resource key and message.
    /// </summary>
    /// <param name="key">The key of the resource that was not found.</param>
    /// <param name="message">The error message.</param>
    public ResourceNotFoundException(ResourceKey key, string message)
        : base(message)
    {
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance with the specified resource key, message, and inner exception.
    /// </summary>
    /// <param name="key">The key of the resource that was not found.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ResourceNotFoundException(ResourceKey key, string message, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }
}
