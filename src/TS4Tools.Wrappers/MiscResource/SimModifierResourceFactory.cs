namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating SimModifierResource instances.
/// Handles SMOD (SimModifier) resources.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs lines 319-326
/// </remarks>
[ResourceHandler(0xC5F6763E)]
public sealed class SimModifierResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SimModifierResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SimModifierResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
