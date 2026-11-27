using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Registry-based viewer selection.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2644-2670 (wrapper resolved
/// through WrapperDealer, AlwaysDefault = HexOnly) and 2947-2983 (GetPreviewControl fallbacks), and
/// legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs (DDS/RLE controls gated by EnableDDSPreview).
/// </remarks>
public sealed class ViewerRoutingTests
{
    private static readonly ResourceViewerOptions Defaults = new(false, true, false, false);

    private static ResourceHandlerRegistry CreateRegistry() =>
        WrapperRegistryService.CreateRegistry([], null, out _, out _);

    private static uint TypeOf<TFactory>(ResourceHandlerRegistry registry) =>
        registry.TypeMap.First(kv => kv.Value == typeof(TFactory)).Key;

    private static ResourceViewerSelection Route<TFactory>(ResourceViewerOptions? options = null)
    {
        var registry = CreateRegistry();
        var key = new ResourceKey(TypeOf<TFactory>(registry), 0, 1);
        var data = registry.CreateNewResource(key).Data;
        return ResourceViewerFactory.Create(key, data, registry, options ?? Defaults);
    }

    internal static byte[] CreateDds(int width = 8, int height = 8)
    {
        var rgba = new byte[width * height * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = (byte)(i * 7);
            rgba[i + 1] = 0x80;
            rgba[i + 2] = 0x20;
            rgba[i + 3] = 0xFF;
        }
        return DdsImage.FromRgba32(rgba, width, height).SaveToDds(DdsFormat.Dxt5);
    }

    [Fact]
    public void StringTable_RoutesToStblEditor()
    {
        var selection = Route<StblResourceFactory>();
        selection.Kind.Should().Be(ResourceViewKind.StringTable);
        selection.EditorViewModel.Should().BeOfType<StblEditorViewModel>();
        selection.Wrapper.Should().BeOfType<StblResource>();
    }

    [Fact]
    public void AllTextTypes_RouteToTextEditor()
    {
        var registry = CreateRegistry();
        foreach (var type in TextResourceFactory.SupportedTypes.Take(10).Append(TextResourceFactory.JazzTuningType))
        {
            var key = new ResourceKey(type, 0, 1);
            var selection = ResourceViewerFactory.Create(key, "<I/>"u8.ToArray(), registry, Defaults);
            selection.EditorViewModel.Should().BeOfType<TextEditorViewModel>($"type 0x{type:X8} is a text type");
        }
    }

    [Fact]
    public void ImageTypes_RouteToImageViewer()
    {
        var registry = CreateRegistry();
        foreach (var type in ImageResourceFactory.SupportedTypes.Take(5))
        {
            var selection = ResourceViewerFactory.Create(new ResourceKey(type, 0, 1), ReadOnlyMemory<byte>.Empty, registry, Defaults);
            selection.Kind.Should().Be(ResourceViewKind.Image);
            selection.EditorViewModel.Should().BeOfType<ImageViewerViewModel>();
        }
    }

    [Fact]
    public void DstTexture_RoutesToImageViewer_WhenDdsPreviewEnabled()
    {
        var registry = CreateRegistry();
        var key = new ResourceKey(ImageResourceFactory.DstType, 0, 1);
        var selection = ResourceViewerFactory.Create(key, CreateDds(), registry, Defaults);

        selection.Kind.Should().Be(ResourceViewKind.Image);
        var viewer = selection.EditorViewModel.Should().BeOfType<ImageViewerViewModel>().Subject;
        viewer.Width.Should().Be(8);
    }

    [Fact]
    public void DstTexture_FallsBackToHex_WhenDdsPreviewDisabled()
    {
        var registry = CreateRegistry();
        var key = new ResourceKey(ImageResourceFactory.DstType, 0, 1);
        var selection = ResourceViewerFactory.Create(key, CreateDds(), registry, Defaults with { EnableDdsPreview = false });

        selection.Kind.Should().Be(ResourceViewKind.Hex);
        selection.Wrapper.Should().BeOfType<ImageResource>("the property grid still gets the wrapper");
    }

    [Fact]
    public void LegacyDdsType_WithDefaultWrapper_GetsDdsPreview()
    {
        var registry = CreateRegistry();
        var key = new ResourceKey(ResourceViewerFactory.LegacyDdsType, 0, 1);
        var selection = ResourceViewerFactory.Create(key, CreateDds(), registry, Defaults);

        selection.Kind.Should().Be(ResourceViewKind.Image);
        selection.Wrapper.Should().BeOfType<ImageResource>();
    }

    [Fact]
    public void RleTexture_RoutesToImageViewer()
    {
        var selection = Route<RleResourceFactory>();
        selection.Kind.Should().Be(ResourceViewKind.Image);
        selection.Wrapper.Should().BeOfType<RleResource>();
    }

    [Fact]
    public void Thumbnail_RoutesToImageViewer()
    {
        var selection = Route<ThumbnailResourceFactory>();
        selection.Kind.Should().Be(ResourceViewKind.Image);
        selection.Wrapper.Should().BeOfType<ThumbnailResource>();
    }

    [Fact]
    public void AllCatalogFamilies_RouteToCatalogViewer()
    {
        Route<CobjResourceFactory>().EditorViewModel.Should().BeOfType<CatalogViewerViewModel>();
        Route<CflrResourceFactory>().EditorViewModel.Should().BeOfType<CatalogViewerViewModel>();
        Route<CwalResourceFactory>().EditorViewModel.Should().BeOfType<CatalogViewerViewModel>();
        Route<A8f7b517CatalogResourceFactory>().EditorViewModel.Should().BeOfType<CatalogViewerViewModel>();
        Route<RoofStyleResourceFactory>().Kind.Should().Be(ResourceViewKind.Catalog);
    }

    [Fact]
    public void Rcol_RoutesToRcolViewer()
    {
        Route<RcolResourceFactory>().EditorViewModel.Should().BeOfType<RcolViewerViewModel>();
        Route<GeomResourceFactory>().EditorViewModel.Should().BeOfType<RcolViewerViewModel>();
    }

    [Fact]
    public void SimDataAndDeformerMap_RouteToDedicatedViewers()
    {
        Route<SimDataResourceFactory>().EditorViewModel.Should().BeOfType<SimDataViewerViewModel>();
        Route<DeformerMapResourceFactory>().EditorViewModel.Should().BeOfType<DeformerMapViewerViewModel>();
    }

    [Fact]
    public void OtherWrappers_RouteToValueView()
    {
        Route<CasPartResourceFactory>().Kind.Should().Be(ResourceViewKind.Value);
        Route<BoneResourceFactory>().Kind.Should().Be(ResourceViewKind.Value);
        Route<SkinToneResourceFactory>().Kind.Should().Be(ResourceViewKind.Value);
        Route<ThumResourceFactory>().Kind.Should().Be(ResourceViewKind.Value);
        Route<RigResourceFactory>().Kind.Should().Be(ResourceViewKind.Value);

        var selection = Route<ComplateResourceFactory>();
        selection.Kind.Should().Be(ResourceViewKind.Value);
        selection.EditorViewModel.Should().BeOfType<ValueViewerViewModel>().Which.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EveryRegisteredWrapper_GetsAViewAndWrapper()
    {
        var registry = CreateRegistry();
        foreach (var type in registry.RegisteredTypes)
        {
            var key = new ResourceKey(type, 0, 1);
            IResource empty;
            try
            {
                empty = registry.CreateNewResource(key);
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException)
            {
                continue;
            }

            var selection = ResourceViewerFactory.Create(key, empty.Data, registry, Defaults);
            selection.EditorViewModel.Should().NotBeNull();
            if (selection.Kind != ResourceViewKind.Error)
                selection.Wrapper.Should().NotBeNull($"type 0x{type:X8} should keep its wrapper for the property grid");
        }
    }

    [Fact]
    public void DisabledWrapper_FallsBackToDefaultWrapperAndHex()
    {
        var registry = CreateRegistry();
        var stblType = TypeOf<StblResourceFactory>(registry);
        registry.DisableWrapper(stblType, typeof(StblResourceFactory));

        var key = new ResourceKey(stblType, 0, 1);
        var data = new StblResource(key, ReadOnlyMemory<byte>.Empty).Data;
        var selection = ResourceViewerFactory.Create(key, data, registry, Defaults);

        selection.Kind.Should().Be(ResourceViewKind.Hex);
        selection.Wrapper.Should().BeOfType<DefaultResource>();
    }

    [Fact]
    public void ForceHex_UsesDefaultWrapper()
    {
        var selection = Route<StblResourceFactory>(Defaults with { ForceHex = true });
        selection.Kind.Should().Be(ResourceViewKind.Hex);
        selection.Wrapper.Should().BeOfType<DefaultResource>();
    }

    [Fact]
    public void UnknownBinary_ShowsHex()
    {
        var selection = ResourceViewerFactory.Create(new ResourceKey(0x12345678, 0, 1), new byte[] { 0, 1, 2, 3 }, CreateRegistry(),
            Defaults with { EnableFallbackTextPreview = true });
        selection.Kind.Should().Be(ResourceViewKind.Hex);
    }

    [Fact]
    public void UnknownText_ShowsFallbackText_WhenEnabled()
    {
        var data = "hello = world\nline two\n"u8.ToArray();
        var selection = ResourceViewerFactory.Create(new ResourceKey(0x12345678, 0, 1), data, CreateRegistry(),
            Defaults with { EnableFallbackTextPreview = true });

        selection.Kind.Should().Be(ResourceViewKind.FallbackText);
        var vm = selection.EditorViewModel.Should().BeOfType<ValueViewerViewModel>().Subject;
        vm.Text.Should().StartWith(ValueViewerViewModel.FallbackBanner).And.EndWith("line two\n");
    }

    [Fact]
    public void FallbackHex_TakesPrecedenceOverFallbackText()
    {
        var data = "plain text"u8.ToArray();
        var selection = ResourceViewerFactory.Create(new ResourceKey(0x12345678, 0, 1), data, CreateRegistry(),
            Defaults with { EnableFallbackTextPreview = true, EnableFallbackHexPreview = true });
        selection.Kind.Should().Be(ResourceViewKind.Hex);
    }

    [Fact]
    public void UnknownText_ShowsHex_WhenFallbackTextDisabled()
    {
        var selection = ResourceViewerFactory.Create(new ResourceKey(0x12345678, 0, 1), "plain text"u8.ToArray(), CreateRegistry(), Defaults);
        selection.Kind.Should().Be(ResourceViewKind.Hex);
    }

    [Fact]
    public void CorruptResource_ShowsErrorView()
    {
        var registry = CreateRegistry();
        var key = new ResourceKey(TypeOf<StblResourceFactory>(registry), 0, 1);
        var selection = ResourceViewerFactory.Create(key, new byte[] { 0xFF, 0xFE, 0x01 }, registry, Defaults);

        selection.Kind.Should().Be(ResourceViewKind.Error);
        selection.EditorViewModel.Should().BeOfType<ValueViewerViewModel>().Which.Text.Should().Contain("Error reading resource");
    }

    [Theory]
    [InlineData("abc\r\n\tdef", true)]
    [InlineData("café", true)]
    public void LooksLikeText_AcceptsText(string text, bool expected)
    {
        ValueViewerViewModel.LooksLikeText(System.Text.Encoding.UTF8.GetBytes(text)).Should().Be(expected);
    }

    [Fact]
    public void LooksLikeText_RejectsBinary()
    {
        ValueViewerViewModel.LooksLikeText(new byte[] { 0x41, 0x00, 0x42 }).Should().BeFalse();
        ValueViewerViewModel.LooksLikeText(new byte[] { 0xC3, 0x28 }).Should().BeFalse();
        ValueViewerViewModel.LooksLikeText([]).Should().BeFalse();
    }
}
