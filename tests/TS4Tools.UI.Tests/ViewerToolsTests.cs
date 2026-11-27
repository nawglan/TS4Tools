using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Resources;
using TS4Tools.UI.Services;
using TS4Tools.UI.Services.ModelViewer;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Wrapper management, the DDS/RLE/DST/DMAP helper commands and the model viewer's mesh extraction.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs (disable list),
/// legacy_references/Sims4Tools/s4pe Helpers/DDSHelper, RLEDDSHelper, RLESDDSHelper, DMAPImageHelper (import/export),
/// legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs lines 257-385 (mesh assembly).
/// </remarks>
public sealed class ViewerToolsTests
{
    private readonly MockFileSystemService _fileSystem = new();

    #region Manage wrappers

    [Fact]
    public void ManageWrappers_ListsTypeMapAndAppliesDisabledSet()
    {
        var registry = WrapperRegistryService.CreateRegistry([], null, out _, out _);
        var service = new WrapperRegistryService(registry);
        var vm = new ManageWrappersViewModel(service);
        var raised = 0;
        service.WrappersChanged += (_, _) => raised++;

        vm.AllWrappers.Should().HaveCount(registry.TypeMap.Count);
        var stbl = vm.AllWrappers.Single(w => w.FactoryType == typeof(StblResourceFactory));
        stbl.IsActive.Should().BeTrue();
        stbl.Tag.Should().Be("STBL");

        vm.SelectedWrapper = stbl;
        vm.DisableSelectedCommand.Execute(null);
        var settings = new AppSettings();
        vm.Apply(settings);

        settings.DisabledWrappers.Should().Equal(ResourceHandlerRegistry.FormatDisabledWrapper(stbl.ResourceType, typeof(StblResourceFactory)));
        registry.GetFactory(stbl.ResourceType).Should().BeNull();
        raised.Should().Be(1);

        // Persisted entries round-trip through CreateRegistry.
        var reloaded = WrapperRegistryService.CreateRegistry(settings.DisabledWrappers, null, out _, out var unmatched);
        unmatched.Should().BeEmpty();
        reloaded.GetFactory(stbl.ResourceType).Should().BeNull();
    }

    [Fact]
    public void CreateRegistry_ReportsUnmatchedEntriesAndIgnoresMissingPluginFolder()
    {
        var registry = WrapperRegistryService.CreateRegistry(["0x12345678:No.Such.Factory"],
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"), out var errors, out var unmatched);

        errors.Should().BeEmpty();
        unmatched.Should().ContainSingle();
        registry.GetFactory(0x220557DA).Should().BeOfType<StblResourceFactory>();
    }

    [Fact]
    public void DescribeLoadErrors_SummarisesWithoutThrowing()
    {
        WrapperRegistryService.DescribeLoadErrors([]).Should().BeNull();
        var message = WrapperRegistryService.DescribeLoadErrors(
            [new WrapperLoadError("/x/Bad.dll", null, "boom", null, false)]);
        message.Should().Contain("Bad.dll").And.Contain("boom");
    }

    [Fact]
    public async Task DisabledWrapperInPackageRegistry_ShowsHex()
    {
        var key = new ResourceKey(0x220557DA, 0, 1);
        var package = DbpfPackage.CreateNew();
        var stbl = new StblResource(key, ReadOnlyMemory<byte>.Empty);
        stbl.Add(1, "x");
        package.AddResource(key, stbl.Data);
        var registry = WrapperRegistryService.CreateRegistry([], null, out _, out _);
        registry.DisableWrapper(key.ResourceType, typeof(StblResourceFactory));
        package.ResourceHandlers = registry;

        var vm = await SelectAsync(package, key);

        vm.CurrentEditorViewModel.Should().BeOfType<HexViewerViewModel>();
        vm.CurrentResourceWrapper.Should().BeOfType<DefaultResource>();
    }

    #endregion

    #region Import / export

    private async Task<MainWindowViewModel> SelectAsync(DbpfPackage package, ResourceKey key)
    {
        var vm = new MainWindowViewModel(_fileSystem, new FakeDialogService(), _ => null) { ShellSettings = new AppSettings() };
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources.Single(r => r.Key == key);
        await vm.PendingSelectionTask;
        return vm;
    }

    private static async Task<ReadOnlyMemory<byte>> ReadAsync(DbpfPackage package, ResourceKey key) =>
        await package.GetResourceDataAsync(package.Find(key)!);

    [Fact]
    public async Task ImportDds_IntoDst_CommitsDataThatReparses()
    {
        var key = new ResourceKey(ImageResourceFactory.DstType, 0, 1);
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, ViewerRoutingTests.CreateDds(8, 8));
        var vm = await SelectAsync(package, key);
        vm.CanImportDds.Should().BeTrue();
        vm.CanImportDdsToDst.Should().BeTrue();

        _fileSystem.SetupFile("/in/new.dds", ViewerRoutingTests.CreateDds(16, 8));
        (await vm.ImportIntoCurrentAsync(ResourceImportKind.DdsToDst, "/in/new.dds")).Should().BeTrue();
        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var data = await ReadAsync(package, key);
        DstDecoder.IsDst(data.Span).Should().BeTrue("the DDS was shuffled to DST");
        var image = new ImageResource(key, data);
        image.Width.Should().Be(16);
        image.ToDds().Should().NotBeNull();
    }

    [Fact]
    public async Task ImportDds_PlainDds_IsStoredAsDds()
    {
        var key = new ResourceKey(ImageResourceFactory.DstType, 0, 1);
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, ViewerRoutingTests.CreateDds());
        var vm = await SelectAsync(package, key);

        var dds = ViewerRoutingTests.CreateDds(4, 4);
        _fileSystem.SetupFile("/in/a.dds", dds);
        (await vm.ImportIntoCurrentAsync(ResourceImportKind.Dds, "/in/a.dds")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var image = new ImageResource(key, await ReadAsync(package, key));
        image.Format.Should().Be(ImageFormat.Dds);
        image.Width.Should().Be(4);
    }

    [Fact]
    public async Task ImportDds_RejectsNonDds()
    {
        var key = new ResourceKey(ImageResourceFactory.DstType, 0, 1);
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, ViewerRoutingTests.CreateDds());
        var vm = await SelectAsync(package, key);

        _fileSystem.SetupFile("/in/bad.dds", new byte[200]);
        (await vm.ImportIntoCurrentAsync(ResourceImportKind.Dds, "/in/bad.dds")).Should().BeFalse();
        vm.IsResourceDirty.Should().BeFalse();
        vm.StatusMessage.Should().StartWith("Import error");
    }

    [Theory]
    [InlineData(RleResource.TypeId, RleVersion.Rle2)]
    [InlineData(RleResource.TypeIdAlternate, RleVersion.Rles)]
    public async Task ImportDds_IntoRle_ReencodesAndReparses(uint type, RleVersion expected)
    {
        var key = new ResourceKey(type, 0, 1);
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, RleResource.EncodeFromDds(ViewerRoutingTests.CreateDds(8, 8), expected));
        var vm = await SelectAsync(package, key);
        vm.CanExportDds.Should().BeTrue();

        _fileSystem.SetupFile("/in/r.dds", ViewerRoutingTests.CreateDds(16, 16));
        (await vm.ImportIntoCurrentAsync(ResourceImportKind.Dds, "/in/r.dds")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var rle = new RleResource(key, await ReadAsync(package, key));
        rle.Version.Should().Be(expected);
        rle.Width.Should().Be(16);

        (await vm.ExportCurrentAsync(ResourceExportKind.Dds, "/out/r.dds")).Should().BeTrue();
        var exported = _fileSystem.WrittenFiles["/out/r.dds"];
        ResourceViewerFactory.IsDds(exported).Should().BeTrue();
        DdsImage.Load(exported).Width.Should().Be(16);
    }

    [Fact]
    public async Task DeformerMap_ExportAndImportBitmap()
    {
        var key = new ResourceKey(DeformerMapResource.TypeId, 0, 1);
        var map = new DeformerMapResource(key, ReadOnlyMemory<byte>.Empty);
        DeformerMapImageConverter.ImportFromImage(map, Enumerable.Range(0, 4 * 2 * 3).Select(i => (byte)i).ToArray(), default, 4, 2);
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, map.Data);
        var vm = await SelectAsync(package, key);
        vm.CanEditDeformerMapImage.Should().BeTrue();

        (await vm.ExportCurrentAsync(ResourceExportKind.DeformerMapSkin, "/out/skin.bmp")).Should().BeTrue();
        _fileSystem.WrittenFiles["/out/skin.bmp"][..2].Should().Equal((byte)'B', (byte)'M');

        var other = new DeformerMapResource(key, ReadOnlyMemory<byte>.Empty);
        DeformerMapImageConverter.ImportFromImage(other, Enumerable.Repeat((byte)7, 3 * 3 * 3).ToArray(), default, 3, 3);
        _fileSystem.SetupFile("/in/other.bmp", DeformerMapImageConverter.ToBitmap(other, DeformerMapImageType.Skin)!);

        (await vm.ImportIntoCurrentAsync(ResourceImportKind.DeformerMapBitmap, "/in/other.bmp")).Should().BeTrue();
        vm.CommitCommand.Execute(null);

        var reread = new DeformerMapResource(key, await ReadAsync(package, key));
        var image = DeformerMapImageConverter.ToImage(reread, DeformerMapImageType.Skin);
        image.Should().NotBeNull();
        image!.Width.Should().Be(3);
        image.Pixels.Should().OnlyContain(b => b == 7);
    }

    #endregion

    #region Model viewer

    private static VrtfBlock PositionOnlyVrtf()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("VRTF"u8.ToArray());
        w.Write(2u); w.Write(12); w.Write(1); w.Write(0u);
        w.Write(new byte[] { (byte)ElementUsage.Position, 0, (byte)ElementFormat.Float3, 0 });
        return new VrtfBlock(ms.ToArray());
    }

    private static VbufBlock Vbuf(params float[] xyz)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("VBUF"u8.ToArray());
        w.Write(0x101u); w.Write(0u); w.Write(0u);
        foreach (var f in xyz) w.Write(f);
        return new VbufBlock(ms.ToArray());
    }

    private static IbufBlock Ibuf(params short[] indices)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("IBUF"u8.ToArray());
        w.Write(2u); w.Write(0u); w.Write(0u);
        foreach (var i in indices) w.Write(i);
        return new IbufBlock(ms.ToArray());
    }

    private static RcolResource CreateMlod()
    {
        var rcol = new RcolResource(new ResourceKey(RcolConstants.Mlod, 0, 1), ReadOnlyMemory<byte>.Empty);
        var mlod = new MlodBlock();
        rcol.AddChunk(new RcolTgiBlock(1, RcolConstants.Mlod, 0), mlod);
        rcol.AddChunk(new RcolTgiBlock(2, VrtfBlock.TypeId, 0), PositionOnlyVrtf());
        // Vertex 0 belongs to another mesh; this mesh uses vertices 1-4 (a quad).
        rcol.AddChunk(new RcolTgiBlock(3, VbufBlock.TypeId, 0), Vbuf(9, 9, 9, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0));
        rcol.AddChunk(new RcolTgiBlock(4, IbufBlock.TypeId, 0), Ibuf(0, 1, 2, 0, 2, 3, 0, 1, 9));
        rcol.PublicChunksCount = 4;
        mlod.Meshes.Add(new MlodMesh
        {
            Name = 0xABCD,
            VertexFormatIndex = new RcolChunkReference(RcolReferenceType.Public, 1),
            VertexBufferIndex = new RcolChunkReference(RcolReferenceType.Public, 2),
            IndexBufferIndex = new RcolChunkReference(RcolReferenceType.Public, 3),
            PrimitiveType = ModelPrimitiveType.TriangleList,
            StreamOffset = 12,
            VertexCount = 4,
            StartIndex = 0,
            PrimitiveCount = 3,
        });
        mlod.Meshes.Add(new MlodMesh
        {
            Name = 0xDEAD,
            VertexBufferIndex = new RcolChunkReference(RcolReferenceType.Public, 7), // missing chunk
            IndexBufferIndex = new RcolChunkReference(RcolReferenceType.Public, 3),
            PrimitiveType = ModelPrimitiveType.TriangleList,
        });
        return rcol;
    }

    [Fact]
    public void RcolViewer_OffersTheModelViewer_OnlyForMeshes()
    {
        var withMesh = new RcolViewerViewModel();
        withMesh.LoadResource(CreateMlod());
        withMesh.HasModel.Should().BeTrue();

        var without = new RcolViewerViewModel();
        without.LoadResource(new RcolResource(new ResourceKey(RcolConstants.Mlod, 0, 2), ReadOnlyMemory<byte>.Empty));
        without.HasModel.Should().BeFalse();
    }

    [Fact]
    public void MeshExtractor_ResolvesMlodChunksAndDropsBadTriangles()
    {
        var model = MeshExtractor.Extract(CreateMlod());

        model.Meshes.Should().ContainSingle();
        var mesh = model.Meshes[0];
        mesh.Name.Should().Be("0x0000ABCD");
        mesh.VertexCount.Should().Be(4);
        mesh.Positions[..3].Should().Equal(0f, 0f, 0f);
        mesh.Positions[6..9].Should().Equal(1f, 1f, 0f);
        mesh.Indices.Should().Equal(0, 1, 2, 0, 2, 3); // third triangle references vertex 9
        model.VertexCount.Should().Be(4);
        model.PolygonCount.Should().Be(3);
        model.Warnings.Should().ContainSingle().Which.Should().Contain("0x0000DEAD");
    }

    [Fact]
    public void MeshExtractor_GetBlock_HandlesPrivateAndExternalReferences()
    {
        var rcol = CreateMlod();
        rcol.PublicChunksCount = 2;
        MeshExtractor.GetBlock(rcol, new RcolChunkReference(RcolReferenceType.Private, 0)).Should().BeOfType<VbufBlock>();
        MeshExtractor.GetBlock(rcol, new RcolChunkReference(RcolReferenceType.External, 0)).Should().BeNull();
        MeshExtractor.GetBlock(rcol, default).Should().BeNull();
    }

    [Fact]
    public void MeshExtractor_FromVertices_KeepsNormalsWhenAllPresent()
    {
        var verts = new[]
        {
            new VbufVertex { Position = [0, 0, 0], Normal = [0, 0, 1] },
            new VbufVertex { Position = [1, 0, 0], Normal = [0, 0, 1] },
            new VbufVertex { Position = [0, 1, 0], Normal = [0, 0, 1] },
        };
        var mesh = MeshExtractor.FromVertices("m", verts, [0, 1, 2]);
        mesh.Normals.Should().NotBeNull();
        mesh.TriangleCount.Should().Be(1);
    }

    [Fact]
    public void MeshExtractor_EmptyGeom_HasNoTriangles()
    {
        var mesh = MeshExtractor.FromGeom(new GeomBlock());
        mesh.VertexCount.Should().Be(0);
        mesh.TriangleCount.Should().Be(0);
    }

    [Theory]
    [InlineData(ModelRenderMode.Solid)]
    [InlineData(ModelRenderMode.Wireframe)]
    [InlineData(ModelRenderMode.Both)]
    public void SoftwareRenderer_DrawsTheMesh(ModelRenderMode mode)
    {
        var mesh = MeshExtractor.Extract(CreateMlod()).Meshes[0];
        var pixels = SoftwareRenderer.Render([mesh], 64, 64, new OrbitCamera(0, 0, 1), mode);

        pixels.Should().HaveCount(64 * 64 * 4);
        var background = pixels[..4];
        var centre = (32 * 64 + 32) * 4;
        var drawn = Enumerable.Range(0, 64 * 64).Count(i => !pixels.AsSpan(i * 4, 4).SequenceEqual(background));
        drawn.Should().BeGreaterThan(mode == ModelRenderMode.Wireframe ? 20 : 500);
        if (mode != ModelRenderMode.Wireframe)
            pixels.AsSpan(centre, 4).SequenceEqual(background).Should().BeFalse("the quad covers the centre");
    }

    [Fact]
    public async Task ModelViewer_AvailableForMlodResource()
    {
        var rcol = CreateMlod();
        var package = DbpfPackage.CreateNew();
        package.AddResource(rcol.Key, rcol.Data);
        var vm = await SelectAsync(package, rcol.Key);

        vm.CurrentEditorViewModel.Should().BeOfType<RcolViewerViewModel>();
        vm.CanOpenModelViewer.Should().BeTrue();
        vm.HasViewerTools.Should().BeTrue();

        var viewer = new ModelViewerViewModel();
        viewer.Load((RcolResource)vm.CurrentResourceWrapper!, "test");
        viewer.Meshes.Should().ContainSingle();
        viewer.Statistics.Should().Contain("Vertices: 4").And.Contain("Polygons: 3");
        var requests = 0;
        viewer.RenderRequested += (_, _) => requests++;
        viewer.Meshes[0].IsVisible = false;
        viewer.Rotate(10, 5);
        requests.Should().Be(2);
        viewer.VisibleMeshes.Should().BeEmpty();
        viewer.Dispose();
    }

    #endregion
}
