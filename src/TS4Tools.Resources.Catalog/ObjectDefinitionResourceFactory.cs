using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Factory for creating Object Definition Resources.
/// Object Definition Resources (0xC0DB5AE7) are one of the most common resource types
/// in Sims 4 packages, containing object metadata and configuration.
/// </summary>
public sealed class ObjectDefinitionResourceFactory : ResourceFactoryBase<ObjectDefinitionResource>
{
    /// <summary>
    /// Initializes a new instance of the ObjectDefinitionResourceFactory class.
    /// </summary>
    public ObjectDefinitionResourceFactory()
        : base(new[] { "0xC0DB5AE7" }, 100)
    {
    }

    /// <inheritdoc />
    public override async Task<ObjectDefinitionResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        var resource = new ObjectDefinitionResource(apiVersion);

        if (stream != null)
        {
            try
            {
                await resource.ParseFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to create object definition resource from stream: {ex.Message}", ex);
            }
        }

        return resource;
    }
}
