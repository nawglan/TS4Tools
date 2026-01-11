using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Object (COBJ) resource.
/// This is the most common catalog type used for build/buy objects.
/// Resource Type: 0x319E4F1D
/// Source: COBJResource.cs lines 30-36
/// </summary>
public sealed class CobjResource : AbstractCatalogResource
{
    /// <summary>
    /// Resource type ID for COBJ resources.
    /// </summary>
    public const uint TypeId = 0x319E4F1D;

    /// <summary>
    /// Creates a new COBJ resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CobjResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }
}
