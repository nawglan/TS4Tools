// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 87-129 (handler scan and wrapper
// construction) and legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs lines 67-121 (AsBytes / ResourceChanged)

using TS4Tools.Compatibility;
using TS4Tools.Resources;
using CoreResourceKey = TS4Tools.ResourceKey;

namespace CompatTests;

public class LegacyWrapperBridgeTests
{
    private static readonly CoreResourceKey SampleKey = new(SampleResource.TypeId, 0, 1);

    private static ResourceHandlerRegistry RegistryWithSample(out IReadOnlyList<TS4Tools.Resources.WrapperLoadError> errors,
        out IReadOnlyList<KeyValuePair<uint, Type>> registered)
    {
        var registry = new ResourceHandlerRegistry();
        errors = LegacyWrapperBridge.Register(registry, [typeof(SampleResourceHandler).Assembly], out registered);
        return registry;
    }

    [Fact]
    public void Register_AddsALegacyFactoryForEachHandlerType()
    {
        var registry = RegistryWithSample(out var errors, out var registered);

        errors.Should().BeEmpty();
        registered.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<uint, Type>(SampleResource.TypeId, typeof(SampleResource)));
        registry.GetFactory(SampleResource.TypeId).Should().BeOfType<LegacyResourceFactory>()
            .Which.WrapperType.Should().Be<SampleResource>();
    }

    [Fact]
    public void CreateResource_ParsesThroughTheLegacyWrapper_AndRoundTripsData()
    {
        var registry = RegistryWithSample(out _, out _);
        var bytes = SampleResource.Build(7, (1, 2, 3));

        using var resource = registry.CreateResource(SampleKey, bytes);

        var adapter = resource.Should().BeOfType<LegacyResourceAdapter>().Subject;
        var legacy = adapter.LegacyResource.Should().BeOfType<SampleResource>().Subject;
        legacy.Version.Should().Be(7);
        legacy.TGIBlocks.Should().ContainSingle();
        adapter.Data.ToArray().Should().Equal(bytes);
        adapter.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void LegacyEdit_RaisesChanged_MarksDirty_AndUpdatesData()
    {
        var registry = RegistryWithSample(out _, out _);
        using var adapter = (LegacyResourceAdapter)registry.CreateResource(SampleKey, SampleResource.Build(1));
        var changes = 0;
        adapter.Changed += (_, _) => changes++;

        ((SampleResource)adapter.LegacyResource).Version = 9;

        changes.Should().Be(1);
        adapter.IsDirty.Should().BeTrue();
        adapter.Data.ToArray().Should().Equal(SampleResource.Build(9));
    }

    [Fact]
    public void CreateNewResource_UsesTheLegacyNullStreamConstructor()
    {
        var registry = RegistryWithSample(out _, out _);

        using var resource = registry.CreateNewResource(SampleKey);

        resource.Should().BeOfType<LegacyResourceAdapter>();
        resource.Data.ToArray().Should().Equal(SampleResource.Build(1));
    }

    [Fact]
    public void Register_TypeAlreadyHandled_KeepsTheTS4ToolsWrapperAndReportsIt()
    {
        var registry = new ResourceHandlerRegistry();
        var native = new ReversingFactory();
        registry.Register(SampleResource.TypeId, native);

        var errors = LegacyWrapperBridge.Register(registry, [typeof(SampleResourceHandler).Assembly], out var registered);

        registered.Should().BeEmpty();
        registry.GetFactory(SampleResource.TypeId).Should().BeSameAs(native);
        errors.Should().ContainSingle()
            .Which.Message.Should().Contain("not used").And.Contain(typeof(ReversingFactory).FullName!);
    }

    [Fact]
    public void Adapter_Dispose_UnsubscribesFromTheLegacyResource()
    {
        var legacy = new SampleResource(0, new MemoryStream(SampleResource.Build(1)));
        var adapter = new LegacyResourceAdapter(legacy);
        var changes = 0;
        adapter.Changed += (_, _) => changes++;

        adapter.Dispose();
        legacy.Version = 5;

        changes.Should().Be(0);
    }

    [Fact]
    public void Factory_RejectsTypesThatAreNotLegacyWrappers()
    {
        var act = () => new LegacyResourceFactory(typeof(string));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetValueText_FallsBackToTheLegacyToString()
    {
        var registry = RegistryWithSample(out _, out _);
        using var adapter = (LegacyResourceAdapter)registry.CreateResource(SampleKey, SampleResource.Build(3));

        // SampleResource has no "Value" content field, so the ValueBuilder dump (ToString) is shown.
        adapter.GetValueText().Should().Contain("Version: 0x00000003");
    }

    [Fact]
    public void Register_HandlerThatThrowsWhileEnumerated_IsReported_AndOthersStillRegister()
    {
        // Source: WrapperDealer.cs line 112 (a failing plugin never stops the others from loading). The faulty
        // handler's type-string list is null, so enumerating it throws NullReferenceException.
        var registry = new ResourceHandlerRegistry();
        IReadOnlyList<TS4Tools.Resources.WrapperLoadError> errors;
        IReadOnlyList<KeyValuePair<uint, Type>> registered;
        NullTypeListHandler.Enabled = true;
        try
        {
            errors = LegacyWrapperBridge.Register(registry, [typeof(NullTypeListHandler).Assembly], out registered);
        }
        finally
        {
            NullTypeListHandler.Enabled = false;
        }

        errors.Should().ContainSingle(e => e.TypeName == typeof(NullTypeListHandler).FullName)
            .Which.Message.Should().Contain("Could not load legacy handler");
        registered.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<uint, Type>(SampleResource.TypeId, typeof(SampleResource)));
        registry.GetFactory(SampleResource.TypeId).Should().BeOfType<LegacyResourceFactory>();
    }
}
