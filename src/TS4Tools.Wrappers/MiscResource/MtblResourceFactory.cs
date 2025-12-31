namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating MtblResource instances.
/// Handles MTBL (Model Table) resources.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/MTBLResource.cs line 343-349
/// </remarks>
[ResourceHandler(0x81CA1A10)]
public sealed class MtblResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new MtblResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new MtblResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
