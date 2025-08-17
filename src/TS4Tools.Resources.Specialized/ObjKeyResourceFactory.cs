using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating ObjKeyResource instances from OBJK data.
/// Handles the standard resource type 0x48C28979 for Object Key resources.
/// </summary>
public sealed class ObjKeyResourceFactory : ResourceFactoryBase<IObjKeyResource>
{
    /// <summary>
    /// The resource type ID for Object Key resources
    /// </summary>
    public const string ResourceTypeId = "0x48C28979";

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjKeyResourceFactory"/> class.
    /// </summary>
    public ObjKeyResourceFactory() : base(new[] { ResourceTypeId }, priority: 100)
    {
    }

    /// <summary>
    /// Creates an ObjKeyResource from a stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">The stream containing OBJK data, or null for empty resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ObjKeyResource</returns>
    /// <exception cref="ArgumentException">Thrown when the stream contains invalid OBJK data</exception>
    /// <exception cref="NotSupportedException">Thrown when the API version is unsupported</exception>
    public override async Task<IObjKeyResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        if (stream == null)
        {
            return new ObjKeyResource(apiVersion);
        }

        try
        {
            return await ObjKeyResource.FromStreamAsync(apiVersion, stream, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to parse OBJK resource: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Creates an empty ObjKeyResource.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing an empty ObjKeyResource</returns>
    public Task<IObjKeyResource> CreateEmptyAsync(int apiVersion = 1, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new ObjKeyResource(apiVersion);
        return Task.FromResult<IObjKeyResource>(resource);
    }

    /// <summary>
    /// Creates an ObjKeyResource with predefined object key and type.
    /// </summary>
    /// <param name="objectKey">The object key identifier</param>
    /// <param name="objectType">The object type identifier</param>
    /// <param name="additionalData">Optional additional data</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the populated ObjKeyResource</returns>
    public Task<IObjKeyResource> CreateWithDataAsync(
        ulong objectKey,
        uint objectType,
        byte[]? additionalData = null,
        int apiVersion = 1,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new ObjKeyResource(apiVersion)
        {
            ObjectKey = objectKey,
            ObjectType = objectType,
            AdditionalData = additionalData ?? Array.Empty<byte>()
        };

        return Task.FromResult<IObjKeyResource>(resource);
    }

    /// <summary>
    /// Creates an ObjKeyResource from binary data.
    /// </summary>
    /// <param name="data">The binary OBJK data</param>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ObjKeyResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    /// <exception cref="ArgumentException">Thrown when data is invalid</exception>
    public Task<IObjKeyResource> CreateFromDataAsync(
        byte[] data,
        int apiVersion = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateApiVersion(apiVersion);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var resource = ObjKeyResource.FromData(apiVersion, data);
            return Task.FromResult<IObjKeyResource>(resource);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse OBJK data: {ex.Message}", nameof(data), ex);
        }
    }
}
