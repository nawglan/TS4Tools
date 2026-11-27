// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 40-129
// (handler discovery, GetResource/CreateNewResource, AlwaysDefault, TypeMap, Disabled)

using s4pi.Interfaces;
using s4pi.Package;
using s4pi.WrapperDealer;

namespace CompatTests;

public sealed class WrapperDealerTests : IDisposable
{
    private const uint NativeType = 0x0BADF00D;
    private static readonly string SampleKey = "0x" + SampleResource.TypeId.ToString("X8", CultureInfo.InvariantCulture);

    private readonly TS4Tools.Resources.ResourceHandlerRegistry savedRegistry = WrapperDealer.Registry;

    public void Dispose()
    {
        WrapperDealer.Disabled.Clear();
        WrapperDealer.Registry = savedRegistry;
    }

    private static (IPackage Package, IResourceIndexEntry Entry) PackageWith(uint type, byte[] data)
    {
        var pkg = Package.NewPackage(0);
        var rie = pkg.AddResource(new TGIBlock(0, null, type, 0, 1), new MemoryStream(data), true)!;
        return (pkg, rie);
    }

    [Fact]
    public void GetResource_UsesLegacyHandlerFoundInLoadedAssemblies()
    {
        var data = SampleResource.Build(5, (1, 2, 3));
        var (pkg, rie) = PackageWith(SampleResource.TypeId, data);
        using var _ = pkg;

        var res = WrapperDealer.GetResource(0, pkg, rie);

        var sample = res.Should().BeOfType<SampleResource>().Subject;
        sample.Version.Should().Be(5);
        sample.TGIBlocks.Should().ContainSingle().Which.Instance.Should().Be(3);
        sample.AsBytes.Should().Equal(data);
        sample.ContentFields.Should().StartWith(new[] { "Version", "TGIBlocks" });
    }

    [Fact]
    public void TypeMap_ListsLegacyAndTS4ToolsWrappers()
    {
        var map = WrapperDealer.TypeMap;

        map.Should().Contain(new KeyValuePair<string, Type>(SampleKey, typeof(SampleResource)));
        map.Should().Contain(new KeyValuePair<string, Type>("*", typeof(TS4Tools.Resources.DefaultResourceFactory)));
    }

    [Fact]
    public void AlwaysDefault_IgnoresRegisteredWrappers()
    {
        var data = SampleResource.Build(5);
        var (pkg, rie) = PackageWith(SampleResource.TypeId, data);
        using var _ = pkg;

        var res = WrapperDealer.GetResource(0, pkg, rie, AlwaysDefault: true);

        var wrapper = res.Should().BeOfType<TS4ToolsResource>().Subject;
        wrapper.GetNativeResource().Should().BeOfType<TS4Tools.Resources.DefaultResource>();
        wrapper.AsBytes.Should().Equal(data);
    }

    [Fact]
    public void Disabled_SkipsTheWrapper_UntilReEnabled()
    {
        var (pkg, rie) = PackageWith(SampleResource.TypeId, SampleResource.Build(5));
        using var _ = pkg;
        var pair = new KeyValuePair<string, Type>(SampleKey, typeof(SampleResource));

        WrapperDealer.Disabled.Add(pair);
        WrapperDealer.GetResource(0, pkg, rie).Should().BeOfType<TS4ToolsResource>();

        WrapperDealer.Disabled.Remove(pair);
        WrapperDealer.GetResource(0, pkg, rie).Should().BeOfType<SampleResource>();
    }

    [Fact]
    public void CreateNewResource_UsesWrapperWithNullStream()
    {
        var res = WrapperDealer.CreateNewResource(0, SampleKey);

        var sample = res.Should().BeOfType<SampleResource>().Subject;
        sample.Version.Should().Be(1);
        sample.TGIBlocks.Should().BeEmpty();

        var raw = WrapperDealer.CreateNewResource(0, "*");
        raw.Should().BeOfType<TS4ToolsResource>().Which.AsBytes.Should().BeEmpty();
    }

    [Fact]
    public void UnclaimedType_UsesTS4ToolsDefaultFactory()
    {
        var (pkg, rie) = PackageWith(0x00C0FFEE, [1, 2, 3]);
        using var _ = pkg;

        var res = WrapperDealer.GetResource(0, pkg, rie);

        res.Should().BeOfType<TS4ToolsResource>().Which.AsBytes.Should().Equal(1, 2, 3);
        res.ContentFields.Should().Equal("AsBytes", "Stream");
    }

    [Fact]
    public void TS4ToolsFactory_IsBridged_AndCanBeDisabled()
    {
        var registry = new TS4Tools.Resources.ResourceHandlerRegistry();
        registry.Register(NativeType, new ReversingFactory());
        WrapperDealer.Registry = registry;
        var (pkg, rie) = PackageWith(NativeType, [1, 2, 3]);
        using var _ = pkg;

        WrapperDealer.TypeMap.Should().Contain(new KeyValuePair<string, Type>("0x0BADF00D", typeof(ReversingFactory)));

        var res = WrapperDealer.GetResource(0, pkg, rie);
        var native = res.Should().BeOfType<TS4ToolsResource>().Which.GetNativeResource().Should().BeOfType<ReversedResource>().Subject;
        native.Stored.Should().Equal(3, 2, 1);
        res.AsBytes.Should().Equal(1, 2, 3);

        WrapperDealer.Disabled.Add(new KeyValuePair<string, Type>("0x0BADF00D", typeof(ReversingFactory)));
        ((TS4ToolsResource)WrapperDealer.GetResource(0, pkg, rie)).GetNativeResource().Should().BeOfType<TS4Tools.Resources.DefaultResource>();
    }

    [Fact]
    public void TS4ToolsResourceChanges_RaiseResourceChanged_AndReachThePackage()
    {
        var registry = new TS4Tools.Resources.ResourceHandlerRegistry();
        registry.Register(NativeType, new ReversingFactory());
        WrapperDealer.Registry = registry;
        var (pkg, rie) = PackageWith(NativeType, [1, 2, 3]);
        using var _ = pkg;

        using var res = (TS4ToolsResource)WrapperDealer.GetResource(0, pkg, rie);
        int changed = 0;
        res.ResourceChanged += (_, _) => changed++;

        ((ReversedResource)res.GetNativeResource()).Set([9, 8]);
        changed.Should().Be(1);
        pkg.ReplaceResource(rie, res);

        WrapperDealer.GetResource(0, pkg, rie).AsBytes.Should().Equal(9, 8);
    }

    [Fact]
    public void LegacyWrapper_ReplaceResource_WritesUnParsedData()
    {
        var (pkg, rie) = PackageWith(SampleResource.TypeId, SampleResource.Build(5, (1, 2, 3)));
        using var _ = pkg;
        var sample = (SampleResource)WrapperDealer.GetResource(0, pkg, rie);

        sample.TGIBlocks.Add(new TGIBlock(0, null, 4, 5, 6));
        sample.Version = 6;
        pkg.ReplaceResource(rie, sample);

        var reread = (SampleResource)WrapperDealer.GetResource(0, pkg, rie);
        reread.Version.Should().Be(6);
        reread.TGIBlocks.Should().HaveCount(2);
        rie.Memsize.Should().Be((uint)SampleResource.Build(6, (1, 2, 3), (4, 5, 6)).Length);
    }
}
