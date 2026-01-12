namespace TS4Tools;

/// <summary>
/// Exception thrown when a package file has an invalid format.
/// </summary>
public class PackageFormatException : Exception
{
    public PackageFormatException() { }
    public PackageFormatException(string message) : base(message) { }
    public PackageFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a resource has an invalid format.
/// </summary>
public class ResourceFormatException : Exception
{
    public ResourceFormatException() { }
    public ResourceFormatException(string message) : base(message) { }
    public ResourceFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a resource is not found in a package.
/// </summary>
public class ResourceNotFoundException : Exception
{
    public ResourceKey Key { get; }

    public ResourceNotFoundException(ResourceKey key)
        : base($"Resource not found: {key}")
    {
        Key = key;
    }

    public ResourceNotFoundException(ResourceKey key, string message)
        : base(message)
    {
        Key = key;
    }

    public ResourceNotFoundException(ResourceKey key, string message, Exception innerException)
        : base(message, innerException)
    {
        Key = key;
    }
}
