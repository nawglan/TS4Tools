using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Image import (PNG/JPEG to DDS/DST/RLE) and the DDS panel edits: HSV shift, alpha from greyscale, delete alpha.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DDSPanel.cs lines 417-462
/// (Import), 625-680 (HSVShift, SetAlphaFromGreyscale), s4pi Wrappers/ImageResource DdsFile.cs lines 1392-1560, and
/// s4pe Helpers/DDSHelper, RLEDDSHelper Import.cs (storing the DDS in the resource).
/// </remarks>
public sealed class ImageEditTests
{
    private static readonly ResourceKey DdsKey = new(ImageResourceFactory.DstType, 0, 1);
    private static readonly ResourceKey RleKey = new(RleResource.TypeId, 0, 2);
    private readonly MockFileSystemService _fileSystem = new();

    private static byte[] Solid(int width, int height, byte r, byte g, byte b, byte a = 255)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = r;
            rgba[i + 1] = g;
            rgba[i + 2] = b;
            rgba[i + 3] = a;
        }
        return rgba;
    }

    private static byte[] SolidDds(byte r, byte g, byte b, byte a = 255, DdsFormat format = DdsFormat.Dxt5) =>
        DdsImage.FromRgba32(Solid(8, 8, r, g, b, a), 8, 8).SaveToDds(format);

    private async Task<(MainWindowViewModel Vm, DbpfPackage Package)> OpenAsync(ResourceKey key, byte[] data)
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, data);
        var vm = new MainWindowViewModel(_fileSystem, new FakeDialogService(), _ => null)
        {
            ShellSettings = new AppSettings(),
            ImageDecoder = bytes => bytes.Length == 3 ? null : new DecodedImage(Solid(16, 8, 0, 0, 255), 16, 8)
        };
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources.Single(r => r.Key == key);
        await vm.PendingSelectionTask;
        return (vm, package);
    }

    private static async Task<DdsImage> ReadDdsAsync(DbpfPackage package, ResourceKey key)
    {
        var data = await package.GetResourceDataAsync(package.Find(key)!);
        var image = new ImageResource(key, data);
        return DdsImage.Load(image.ToDds()!);
    }

    private static (byte R, byte G, byte B, byte A) Pixel(DdsImage image, int x, int y)
    {
        var rgba = image.ToRgba32();
        var o = ((y * image.Width) + x) * 4;
        return (rgba[o], rgba[o + 1], rgba[o + 2], rgba[o + 3]);
    }

    #region Import from image

    [Fact]
    public async Task ImportImage_IntoDds_StoresAConvertedDdsOnCommit()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(255, 0, 0));
        vm.CanEditDdsImage.Should().BeTrue();
        vm.HasViewerTools.Should().BeTrue();
        _fileSystem.SetupFile("/in/pic.png", [1, 2, 3, 4]);

        (await vm.ImportImageIntoCurrentAsync("/in/pic.png")).Should().BeTrue();
        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var image = await ReadDdsAsync(package, DdsKey);
        (image.Width, image.Height).Should().Be((16, 8));
        var (r, g, b, a) = Pixel(image, 3, 3);
        (r, g, b, a).Should().Be((0, 0, 255, 255));
    }

    [Fact]
    public async Task ImportImage_IntoDst_KeepsTheDstLayout()
    {
        var dst = DstDecoder.ImportToDst(SolidDds(255, 0, 0));
        var (vm, package) = await OpenAsync(DdsKey, dst);
        _fileSystem.SetupFile("/in/pic.jpg", [1, 2, 3, 4]);

        (await vm.ImportImageIntoCurrentAsync("/in/pic.jpg")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var data = await package.GetResourceDataAsync(package.Find(DdsKey)!);
        DstDecoder.IsDst(data.Span).Should().BeTrue();
        var image = new ImageResource(DdsKey, data);
        image.Width.Should().Be(16);
        image.DxtFormat.Should().Be(DxtFormat.Dxt5);
    }

    [Fact]
    public async Task ExportDds_FromShuffledDst_IsAPlainDds()
    {
        var dst = DstDecoder.ImportToDst(SolidDds(255, 0, 0));
        var (vm, _) = await OpenAsync(DdsKey, dst);
        _fileSystem.SetupFile("/in/x", [1]);

        (await vm.ExportCurrentAsync(ResourceExportKind.Dds, "/out/a.dds")).Should().BeTrue();

        var exported = _fileSystem.WrittenFiles["/out/a.dds"];
        DstDecoder.IsDst(exported).Should().BeFalse("the export is unshuffled");
        Pixel(DdsImage.Load(exported), 1, 1).R.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task ImportImage_IntoDxt1_StaysDxt1()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(255, 0, 0, format: DdsFormat.Dxt1));
        _fileSystem.SetupFile("/in/pic.png", [1, 2, 3, 4]);

        (await vm.ImportImageIntoCurrentAsync("/in/pic.png")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        new ImageResource(DdsKey, await package.GetResourceDataAsync(package.Find(DdsKey)!)).DxtFormat.Should().Be(DxtFormat.Dxt1);
    }

    [Fact]
    public async Task ImportImage_IntoRle_ReencodesAsRle()
    {
        var (vm, package) = await OpenAsync(RleKey, RleResource.EncodeFromDds(SolidDds(255, 0, 0)));
        vm.CanEditDdsImage.Should().BeTrue();
        _fileSystem.SetupFile("/in/pic.png", [1, 2, 3, 4]);

        (await vm.ImportImageIntoCurrentAsync("/in/pic.png")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var rle = new RleResource(RleKey, await package.GetResourceDataAsync(package.Find(RleKey)!));
        rle.Width.Should().Be(16);
        rle.Version.Should().Be(RleVersion.Rle2);
    }

    [Fact]
    public async Task ImportImage_NotAnImage_ReportsAndChangesNothing()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(255, 0, 0));
        _fileSystem.SetupFile("/in/bad.png", [1, 2, 3]); // the test decoder rejects 3 byte files

        (await vm.ImportImageIntoCurrentAsync("/in/bad.png")).Should().BeFalse();

        vm.IsResourceDirty.Should().BeFalse();
        vm.StatusMessage.Should().Contain("not a PNG or JPEG");
        (await ReadDdsAsync(package, DdsKey)).Width.Should().Be(8);
    }

    [Fact]
    public async Task EditTools_AreNotOfferedForOtherResources()
    {
        var key = new ResourceKey(0x00AA0001, 0, 1);
        var (vm, _) = await OpenAsync(key, [1, 2, 3, 4, 5]);

        vm.CanEditDdsImage.Should().BeFalse();
        (await vm.ImportImageIntoCurrentAsync("/in/pic.png")).Should().BeFalse();
        vm.ImportImageCommand.CanExecute(null).Should().BeTrue(); // the command guards itself
    }

    #endregion

    #region HSV and alpha

    [Fact]
    public async Task ApplyHsvShift_ChangesThePixelsPermanently_AndResetsTheSliders()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(255, 0, 0));
        var viewer = vm.CurrentEditorViewModel.Should().BeOfType<ImageViewerViewModel>().Subject;
        viewer.DecodedPixels.Should().NotBeNull();

        viewer.HueShift = 1f / 3f; // red -> green
        viewer.IsHsvNeutral.Should().BeFalse();
        var preview = viewer.PreviewPixels!;
        preview[1].Should().BeGreaterThan(200, "the preview shows the shifted colour");
        preview[0].Should().BeLessThan(60);
        viewer.DecodedPixels![0].Should().BeGreaterThan(200, "the stored pixels are untouched until applied");

        vm.ApplyHsvShiftCommand.Execute(null);

        viewer.IsHsvNeutral.Should().BeTrue();
        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);
        var (r, g, b, a) = Pixel(await ReadDdsAsync(package, DdsKey), 2, 2);
        g.Should().BeGreaterThan(200);
        r.Should().BeLessThan(60);
        b.Should().BeLessThan(60);
        a.Should().Be(255);
    }

    [Fact]
    public async Task ApplyHsvShift_WithNeutralSliders_DoesNothing()
    {
        var (vm, _) = await OpenAsync(DdsKey, SolidDds(255, 0, 0));

        vm.ApplyHsvShiftCommand.Execute(null);

        vm.IsResourceDirty.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAlphaChannel_MakesTheImageOpaque()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(10, 200, 30, 64));

        vm.DeleteAlphaChannelCommand.Execute(null);
        vm.CommitCommand.Execute(null);

        var image = await ReadDdsAsync(package, DdsKey);
        Pixel(image, 1, 1).A.Should().Be(255);
        image.AlphaDepth.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task AlphaFromGreyscale_UsesTheAverageOfTheImageChannels()
    {
        var (vm, package) = await OpenAsync(DdsKey, SolidDds(10, 200, 30));
        // 16x8 solid (30, 30, 30) greyscale source: alpha = (30 + 30 + 30) / 3
        vm.ImageDecoder = _ => new DecodedImage(Solid(16, 8, 30, 30, 30), 16, 8);
        _fileSystem.SetupFile("/in/grey.png", [1, 2, 3, 4]);

        (await vm.SetAlphaFromGreyscaleFileAsync("/in/grey.png")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var image = await ReadDdsAsync(package, DdsKey);
        image.Width.Should().Be(8, "only the alpha channel changes");
        Pixel(image, 2, 2).A.Should().BeInRange(28, 32);
        image.AlphaDepth.Should().Be(5);
    }

    [Fact]
    public async Task Edits_OnRle_StayRle()
    {
        var (vm, package) = await OpenAsync(RleKey, RleResource.EncodeFromDds(SolidDds(255, 0, 0)));

        vm.DeleteAlphaChannelCommand.Execute(null);
        vm.CommitCommand.Execute(null);

        var rle = new RleResource(RleKey, await package.GetResourceDataAsync(package.Find(RleKey)!));
        rle.Width.Should().Be(8);
        rle.ToDds().Should().NotBeNull();
    }

    #endregion
}
