// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TMLTResource.cs lines 230-236

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating TmltResource instances.
/// </summary>
[ResourceHandler(0xB0118C15)]
public sealed class TmltResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new TmltResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new TmltResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
