// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2644-2670 (SelectedResourceChanged: WrapperDealer.GetResource
//         with AlwaysDefault = HexOnly; a wrapper exception is shown instead of the resource)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2947-2983 (GetPreviewControl: Value text, built-in value
//         control, fallback hex, fallback text)
// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 90-472 (DDS/RLE controls gated by EnableDDSPreview,
//         image, thumbnail, deformer map and text controls)

using TS4Tools.Resources;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.Services;

/// <summary>Which kind of view was chosen for a resource.</summary>
public enum ResourceViewKind
{
    Hex,
    StringTable,
    NameMap,
    Text,
    Image,
    SimData,
    Rcol,
    Catalog,
    DeformerMap,
    Value,
    FallbackText,
    Error,
}

/// <summary>Display options read from the settings.</summary>
/// <param name="ForceHex">Hex only / force hex view: wrap with the default wrapper and show hex.</param>
/// <param name="EnableDdsPreview">Legacy EnableDDSPreview: DDS/DST/RLE textures get the image viewer.</param>
/// <param name="EnableFallbackTextPreview">Legacy EnableFallbackTextPreview: unknown types shown as text.</param>
/// <param name="EnableFallbackHexPreview">Legacy EnableFallbackHexPreview: unknown types shown as hex (checked first).</param>
public sealed record ResourceViewerOptions(
    bool ForceHex,
    bool EnableDdsPreview,
    bool EnableFallbackTextPreview,
    bool EnableFallbackHexPreview)
{
    /// <summary>Builds options from settings.</summary>
    public static ResourceViewerOptions From(AppSettings settings, bool forceHex)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(forceHex, settings.EnableDDSPreview, settings.EnableFallbackTextPreview, settings.EnableFallbackHexPreview);
    }
}

/// <summary>The editor/viewer chosen for a resource and the wrapper it edits (for the property grid and commit).</summary>
public sealed record ResourceViewerSelection(object EditorViewModel, IResource? Wrapper, ResourceViewKind Kind);

/// <summary>
/// Resolves a resource's wrapper through the registry and picks the editor or viewer for the wrapper's type.
/// </summary>
/// <remarks>
/// <para>Legacy picks a built-in value control by resource type and falls back to the wrapper's Value text. Here the
/// choice follows the wrapper type the registry produced, so a disabled wrapper (or one replaced by a plugin) changes
/// the view, as in legacy where the default wrapper only offers hex.</para>
/// <para>Routing: STBL, NameMap, Text, SimData, RCOL (incl. GEOM), DeformerMap and every catalog family use their
/// dedicated editors; images (PNG/JPEG, DDS/DST when DDS preview is on), RLE/RLES (DDS preview on) and JFIF
/// thumbnails use the image viewer; any other wrapper shows its Value text; the default wrapper falls back to a DDS
/// preview for DDS data (legacy DDSControl also lists 0x8FFB80F6), then fallback hex, then fallback text when the
/// data looks textual, and otherwise hex (legacy shows nothing, TS4Tools always offers hex).</para>
/// </remarks>
public static class ResourceViewerFactory
{
    /// <summary>Legacy DDSControl's second resource type (DDS image), not wrapped by any built-in wrapper.</summary>
    public const uint LegacyDdsType = 0x8FFB80F6;

    /// <summary>
    /// Wraps <paramref name="data"/> with <paramref name="registry"/> and chooses its view.
    /// </summary>
    public static ResourceViewerSelection Create(
        ResourceKey key,
        ReadOnlyMemory<byte> data,
        IResourceHandlerRegistry registry,
        ResourceViewerOptions options)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(options);

        IResource wrapper;
        try
        {
            wrapper = registry.CreateResource(key, data, alwaysDefault: options.ForceHex);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Source: MainForm.cs lines 2651-2659 (resException) and 2816-2818 (GetExceptionControl)
            return Error(key, ex, null);
        }

        if (options.ForceHex)
            return new ResourceViewerSelection(Hex(data), wrapper, ResourceViewKind.Hex);

        return CreateForWrapper(key, data, wrapper, options);
    }

    /// <summary>
    /// Chooses the view for an already created wrapper.
    /// </summary>
    public static ResourceViewerSelection CreateForWrapper(
        ResourceKey key,
        ReadOnlyMemory<byte> data,
        IResource wrapper,
        ResourceViewerOptions options)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            switch (wrapper)
            {
                case StblResource stbl:
                {
                    var vm = new StblEditorViewModel();
                    vm.LoadResource(stbl);
                    return new(vm, wrapper, ResourceViewKind.StringTable);
                }
                case NameMapResource nameMap:
                {
                    var vm = new NameMapEditorViewModel();
                    vm.LoadResource(nameMap);
                    return new(vm, wrapper, ResourceViewKind.NameMap);
                }
                case TextResource text:
                {
                    var vm = new TextEditorViewModel();
                    vm.LoadResource(text);
                    return new(vm, wrapper, ResourceViewKind.Text);
                }
                case SimDataResource simData:
                {
                    var vm = new SimDataViewerViewModel();
                    vm.LoadResource(simData);
                    return new(vm, wrapper, ResourceViewKind.SimData);
                }
                case ImageResource image:
                {
                    // Source: BuiltInValueControl.cs line 137 (DDSControl.IsAvailable = EnableDDSPreview);
                    // PNG/JPEG (ImageControl) is always available.
                    if (image.Format is ImageFormat.Dds or ImageFormat.Dst && !options.EnableDdsPreview)
                        return Fallback(key, data, wrapper, options);
                    var vm = new ImageViewerViewModel();
                    vm.LoadResource(image);
                    return new(vm, wrapper, ResourceViewKind.Image);
                }
                case RleResource rle:
                {
                    // Source: BuiltInValueControl.cs line 253 (RLEControl.IsAvailable = EnableDDSPreview)
                    if (!options.EnableDdsPreview)
                        return Fallback(key, data, wrapper, options);
                    var vm = new ImageViewerViewModel();
                    vm.LoadRleResource(rle);
                    return new(vm, wrapper, ResourceViewKind.Image);
                }
                case ThumbnailResource thumbnail:
                {
                    // Source: BuiltInValueControl.cs lines 428-472 (ThumbnailControl)
                    var vm = new ImageViewerViewModel();
                    vm.LoadThumbnailResource(thumbnail);
                    return new(vm, wrapper, ResourceViewKind.Image);
                }
                case RcolResource rcol:
                {
                    var vm = new RcolViewerViewModel();
                    vm.LoadResource(rcol);
                    return new(vm, wrapper, ResourceViewKind.Rcol);
                }
                case DeformerMapResource dmap:
                {
                    // Source: BuiltInValueControl.cs lines 343-425 (DeformerMapControl)
                    var vm = new DeformerMapViewerViewModel();
                    vm.LoadResource(dmap);
                    return new(vm, wrapper, ResourceViewKind.DeformerMap);
                }
                case AbstractCatalogResource catalog:
                {
                    var vm = new CatalogViewerViewModel();
                    vm.LoadResource(catalog);
                    return new(vm, wrapper, ResourceViewKind.Catalog);
                }
                case SimpleCatalogResource simpleCatalog:
                {
                    var vm = new CatalogViewerViewModel();
                    vm.LoadResource(simpleCatalog);
                    return new(vm, wrapper, ResourceViewKind.Catalog);
                }
                case ObjectCatalogResource objectCatalog:
                {
                    var vm = new CatalogViewerViewModel();
                    vm.LoadResource(objectCatalog);
                    return new(vm, wrapper, ResourceViewKind.Catalog);
                }
                case DefaultResource:
                    return Fallback(key, data, wrapper, options);
                default:
                {
                    // Every other wrapper: its Value text (legacy HasStringValueContentField -> TextControl).
                    var vm = new ValueViewerViewModel();
                    vm.LoadWrapper(wrapper);
                    return new(vm, wrapper, ResourceViewKind.Value);
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Source: MainForm.cs lines 2979-2982 (preview exception -> GetExceptionControl)
            return Error(key, ex, wrapper);
        }
    }

    /// <summary>
    /// The view for data no wrapper understands (or whose built-in control is switched off).
    /// </summary>
    /// <remarks>Source: MainForm.cs lines 2956-2975.</remarks>
    private static ResourceViewerSelection Fallback(ResourceKey key, ReadOnlyMemory<byte> data, IResource wrapper, ResourceViewerOptions options)
    {
        // Source: BuiltInValueControl.cs lines 92-95 (DDSControl handles 0x00B2D882 and 0x8FFB80F6)
        if (options.EnableDdsPreview && wrapper is DefaultResource
            && (key.ResourceType == LegacyDdsType || IsDds(data.Span)))
        {
            var image = new ImageResource(key, data);
            if (image.Format is ImageFormat.Dds or ImageFormat.Dst)
            {
                var vm = new ImageViewerViewModel();
                vm.LoadResource(image);
                return new(vm, image, ResourceViewKind.Image);
            }
        }

        if (options.EnableFallbackHexPreview)
            return new(Hex(data), wrapper, ResourceViewKind.Hex);

        if (options.EnableFallbackTextPreview && ValueViewerViewModel.LooksLikeText(data.Span))
        {
            var vm = new ValueViewerViewModel();
            vm.LoadFallbackText(data.Span);
            return new(vm, wrapper, ResourceViewKind.FallbackText);
        }

        return new(Hex(data), wrapper, ResourceViewKind.Hex);
    }

    private static ResourceViewerSelection Error(ResourceKey key, Exception ex, IResource? wrapper)
    {
        var vm = new ValueViewerViewModel();
        vm.LoadError($"0x{key.ResourceType:X8}-0x{key.ResourceGroup:X8}-0x{key.Instance:X16}", ex);
        return new(vm, wrapper, ResourceViewKind.Error);
    }

    private static HexViewerViewModel Hex(ReadOnlyMemory<byte> data)
    {
        var vm = new HexViewerViewModel();
        vm.LoadData(data);
        return vm;
    }

    /// <summary>Whether data starts with the DDS magic.</summary>
    public static bool IsDds(ReadOnlySpan<byte> data) =>
        data.Length >= 128 && data[0] == (byte)'D' && data[1] == (byte)'D' && data[2] == (byte)'S' && data[3] == (byte)' ';
}
