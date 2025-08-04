namespace TS4Tools.Resources.Strings;

/// <summary>
/// Factory for creating StringTableResource instances from STBL data.
/// Handles the standard resource type 0x220557DA for String Tables.
/// </summary>
public sealed class StringTableResourceFactory : ResourceFactoryBase<StringTableResource>
{
    /// <summary>
    /// The resource type ID for String Table resources
    /// </summary>
    public const string ResourceTypeId = "0x220557DA";

    /// <summary>
    /// Initializes a new instance of the <see cref="StringTableResourceFactory"/> class.
    /// </summary>
    public StringTableResourceFactory() : base(new[] { ResourceTypeId }, priority: 100)
    {
    }

    /// <summary>
    /// Creates a StringTableResource from a stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">The stream containing STBL data, or null for empty resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created StringTableResource</returns>
    /// <exception cref="ArgumentException">Thrown when the stream contains invalid STBL data</exception>
    /// <exception cref="NotSupportedException">Thrown when the API version is unsupported</exception>
    public override async Task<StringTableResource> CreateResourceAsync(
        int apiVersion, 
        Stream? stream = null, 
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        if (stream == null)
        {
            return new StringTableResource(apiVersion);
        }

        try
        {
            return await StringTableResource.FromStreamAsync(apiVersion, stream, null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to parse STBL resource: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Creates an empty StringTableResource.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing an empty StringTableResource</returns>
    public Task<StringTableResource> CreateEmptyAsync(int apiVersion = 1, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();
        
        var resource = new StringTableResource(apiVersion);
        return Task.FromResult(resource);
    }

    /// <summary>
    /// Creates a StringTableResource with predefined strings.
    /// </summary>
    /// <param name="strings">Dictionary of key-value pairs for the string table</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the populated StringTableResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when strings is null</exception>
    public Task<StringTableResource> CreateWithStringsAsync(
        IDictionary<uint, string> strings,
        int apiVersion = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(strings);
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new StringTableResource(apiVersion);
        
        foreach (var kvp in strings)
        {
            resource.SetString(kvp.Key, kvp.Value);
        }
        
        return Task.FromResult(resource);
    }

    /// <summary>
    /// Creates a StringTableResource from binary data.
    /// </summary>
    /// <param name="data">The binary STBL data</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created StringTableResource</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid</exception>
    public Task<StringTableResource> CreateFromDataAsync(
        ReadOnlyMemory<byte> data,
        int apiVersion = 1,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var resource = StringTableResource.FromData(apiVersion, data.Span, encoding);
            return Task.FromResult(resource);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to parse STBL data: {ex.Message}", nameof(data), ex);
        }
    }

    /// <summary>
    /// Validates whether this factory can handle the specified resource type.
    /// </summary>
    /// <param name="resourceTypeId">The resource type identifier</param>
    /// <returns>True if this factory can handle the resource type</returns>
    public bool CanHandle(string resourceTypeId)
    {
        return string.Equals(resourceTypeId, ResourceTypeId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates whether this factory can handle the specified numeric resource type.
    /// </summary>
    /// <param name="resourceType">The numeric resource type</param>
    /// <returns>True if this factory can handle the resource type</returns>
    public bool CanHandle(uint resourceType)
    {
        return resourceType == StringTableResource.ResourceType;
    }

    /// <summary>
    /// Gets a human-readable description of this factory.
    /// </summary>
    public string Description => "Creates String Table (STBL) resources for game localization";

    /// <summary>
    /// Gets the name of this factory.
    /// </summary>
    public string FactoryName => "String Table Resource Factory";
}
