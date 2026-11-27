// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 129-154
// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResources.txt (embedded verbatim)

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating TextResource instances.
/// </summary>
/// <remarks>
/// Like legacy TextResourceHandler, the handled types are read once from the embedded TextResources.txt
/// (first token of each line). 0x02D5DF13 ("JAZZ tuning") is listed there; legacy has no other WrapperDealer
/// handler for it, so it carries an explicit higher-priority claim to make sure Text wins over any other
/// discovered claim (e.g. a chunk-level RCOL registration).
/// </remarks>
[ResourceHandler(JazzTuningType, Priority = 1)]
public sealed class TextResourceFactory : IResourceFactory, IResourceTypeProvider
{
    /// <summary>Manifest name of the embedded type list.</summary>
    public const string TypeListResourceName = "TS4Tools.Wrappers.TextResource.TextResources.txt";

    /// <summary>"JAZZ tuning" resource type from TextResources.txt.</summary>
    public const uint JazzTuningType = 0x02D5DF13;

    private static readonly Lazy<IReadOnlyList<uint>> Types =
        new(() => ResourceTypeList.LoadEmbedded(typeof(TextResourceFactory).Assembly, TypeListResourceName));

    /// <summary>Gets the resource types listed in TextResources.txt.</summary>
    public static IReadOnlyList<uint> SupportedTypes => Types.Value;

    /// <inheritdoc/>
    public IReadOnlyCollection<uint> ResourceTypes => Types.Value;

    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new TextResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new TextResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
