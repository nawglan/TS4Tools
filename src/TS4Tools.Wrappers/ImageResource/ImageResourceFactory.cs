// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ImageResource.cs lines 71-96
// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ImageResources.txt (embedded verbatim)
// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs lines 277-282 (DST 0x00B2D882)

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ImageResource instances.
/// </summary>
/// <remarks>
/// <para>Like legacy ImageResourceHandler, the handled types are read once from the embedded ImageResources.txt.</para>
/// <para>Legacy registers DST (0x00B2D882) through a separate DSTResourceHandler; the modern
/// <see cref="ImageResource"/> detects and decodes DST itself (ImageFormat.Dst), so this factory also claims it.</para>
/// <para>ImageResources.txt overlaps ThumbnailResourceHandler on 0x3C1AF1F2, 0x5B282D45 and 0xCD9DE247. Legacy
/// WrapperDealer's choice is unspecified there (unstable sort, WrapperDealer.cs lines 114, 120); the registry ranks
/// the Thumbnail factory's attribute claims above this list, so the JFIF+ALFA thumbnail wrapper handles them.</para>
/// </remarks>
[ResourceHandler(DstType)]
public sealed class ImageResourceFactory : IResourceFactory, IResourceTypeProvider
{
    /// <summary>Manifest name of the embedded type list.</summary>
    public const string TypeListResourceName = "TS4Tools.Wrappers.ImageResource.ImageResources.txt";

    /// <summary>DST image resource type (legacy DSTResourceHandler).</summary>
    public const uint DstType = 0x00B2D882;

    private static readonly Lazy<IReadOnlyList<uint>> Types =
        new(() => ResourceTypeList.LoadEmbedded(typeof(ImageResourceFactory).Assembly, TypeListResourceName));

    /// <summary>Gets the resource types listed in ImageResources.txt.</summary>
    public static IReadOnlyList<uint> SupportedTypes => Types.Value;

    /// <inheritdoc/>
    public IReadOnlyCollection<uint> ResourceTypes => Types.Value;

    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ImageResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ImageResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
