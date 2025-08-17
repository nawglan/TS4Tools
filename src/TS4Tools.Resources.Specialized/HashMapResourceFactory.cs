using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating HashMapResource instances from HMAP data.
/// Handles the standard resource type 0x9C58F96E for Hash Map resources.
/// </summary>
public sealed class HashMapResourceFactory : ResourceFactoryBase<IHashMapResource>
{
    /// <summary>
    /// The resource type ID for Hash Map resources
    /// </summary>
    public const string ResourceTypeId = "0x9C58F96E";

    /// <summary>
    /// Initializes a new instance of the <see cref="HashMapResourceFactory"/> class.
    /// </summary>
    public HashMapResourceFactory() : base(new[] { ResourceTypeId }, priority: 100)
    {
    }

    /// <summary>
    /// Creates a HashMapResource from a stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">The stream containing HMAP data, or null for empty resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created HashMapResource</returns>
    /// <exception cref="ArgumentException">Thrown when the stream contains invalid HMAP data</exception>
    /// <exception cref="NotSupportedException">Thrown when the API version is unsupported</exception>
    public override async Task<IHashMapResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        if (stream == null)
        {
            return new HashMapResource(apiVersion);
        }

        try
        {
            return await HashMapResource.FromStreamAsync(apiVersion, stream, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to parse HMAP resource: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Creates an empty HashMapResource.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="capacity">The initial capacity of the hash map</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing an empty HashMapResource</returns>
    public Task<IHashMapResource> CreateEmptyAsync(int apiVersion = 1, int capacity = 16, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new HashMapResource(apiVersion, capacity);
        return Task.FromResult<IHashMapResource>(resource);
    }

    /// <summary>
    /// Creates a HashMapResource with predefined entries.
    /// </summary>
    /// <param name="entries">Dictionary of key-value pairs for the hash map</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the populated HashMapResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when entries is null</exception>
    public Task<IHashMapResource> CreateWithEntriesAsync(
        IDictionary<uint, object> entries,
        int apiVersion = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new HashMapResource(apiVersion, Math.Max(entries.Count * 2, 16));

        foreach (var kvp in entries)
        {
            resource.SetValue(kvp.Key, kvp.Value);
        }

        return Task.FromResult<IHashMapResource>(resource);
    }

    /// <summary>
    /// Creates a HashMapResource from binary data.
    /// </summary>
    /// <param name="data">The binary HMAP data</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created HashMapResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    /// <exception cref="ArgumentException">Thrown when data is invalid</exception>
    public Task<IHashMapResource> CreateFromDataAsync(
        byte[] data,
        int apiVersion = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var resource = HashMapResource.FromData(apiVersion, data);
            return Task.FromResult<IHashMapResource>(resource);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse HMAP data: {ex.Message}", nameof(data), ex);
        }
    }
}
