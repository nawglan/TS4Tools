using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Deterministic precedence, disabled wrappers, default-handler protection and wrapper resolution.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 40-129
///   - TypeMap is sorted with an unstable sort (line 114) and the first non-disabled match wins (line 120), so
///     duplicate claims are scan/sort-order dependent in legacy; modern precedence is explicit and documented.
///   - Disabled pairs are skipped and the next match / "*" default is used (lines 120-123).
///   - GetResource(…, AlwaysDefault) uses "*" (line 66); CreateNewResource passes a null stream (line 42).
/// </summary>
public class ResourceHandlerPrecedenceTests
{
    // Test-only resource types (not used by any real wrapper)
    internal const uint ContestedType = 0xFEED0001;
    internal const uint PriorityType = 0xFEED0002;
    internal const uint ListOnlyType = 0xFEED0003;

    private static ResourceHandlerRegistry CreateTestRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(typeof(ResourceHandlerPrecedenceTests).Assembly);
        return registry;
    }

    [Fact]
    public void EqualPriorityAttributeClaims_BreakTiesByFullTypeName()
    {
        var registry = CreateTestRegistry();

        // AlphaFactory < ZuluFactory ordinal
        registry.GetFactory(ContestedType).Should().BeOfType<AlphaFactory>();
    }

    [Fact]
    public void FaultyProvider_DoesNotStopDiscovery()
    {
        // Source: WrapperDealer.cs line 112 (a failing plugin never stops the others from loading)
        var registry = CreateTestRegistry();

        registry.GetFactory(ListOnlyType).Should().BeOfType<ListFactory>();
        registry.TypeMap.Select(kv => kv.Value).Should().NotContain(typeof(ThrowingListFactory));
    }

    [Fact]
    public void HandlerTypeThatThrowsTypeInitializationException_IsSkipped_AndOthersStillRegister()
    {
        // Source: WrapperDealer.cs line 112 (a failing plugin never stops the others from loading). Reading this
        // type's attributes throws TypeInitializationException (as a plugin type with a broken static constructor
        // can), outside the narrower catches around factory creation and type lists; the per-type catch around
        // discovery must absorb it.
        var broken = new TypeInitFailingType();
        // Preconditions: discovery gets as far as reading the attributes of the broken type.
        typeof(IResourceFactory).IsAssignableFrom(broken).Should().BeTrue();
        broken.IsAbstract.Should().BeFalse();
        var readAttributes = () => broken.GetCustomAttributes<ResourceHandlerAttribute>(inherit: false);
        readAttributes.Should().Throw<TypeInitializationException>();

        var assembly = new FakeAssembly([broken, typeof(ListFactory), typeof(ZuluPriorityFactory)]);
        var registry = new ResourceHandlerRegistry();

        var discover = () => registry.DiscoverHandlers(assembly);

        discover.Should().NotThrow();
        registry.GetFactory(PriorityType).Should().BeOfType<ZuluPriorityFactory>();
        registry.GetFactory(ListOnlyType).Should().BeOfType<ListFactory>();
        (registry.GetFactory(ContestedType) is AlphaFactory).Should().BeFalse(
            "the failing type (wrapping AlphaFactory) must not have registered");
    }

    [Fact]
    public void HigherPriority_Wins()
    {
        var registry = CreateTestRegistry();

        registry.GetFactory(PriorityType).Should().BeOfType<ZuluPriorityFactory>();
    }

    [Fact]
    public void AttributeClaim_BeatsTypeListClaim_AtEqualPriority()
    {
        var registry = CreateTestRegistry();

        // ListFactory (name sorts first) lists ContestedType, but attribute claims rank above list claims.
        registry.GetFactory(ContestedType).Should().NotBeOfType<ListFactory>();
        registry.GetFactory(ListOnlyType).Should().BeOfType<ListFactory>();
    }

    [Fact]
    public void TypeMap_ListsAllClaimsInPrecedenceOrder()
    {
        var registry = CreateTestRegistry();

        registry.TypeMap.Where(kv => kv.Key == ContestedType).Select(kv => kv.Value)
            .Should().Equal(typeof(AlphaFactory), typeof(ZuluFactory), typeof(ListFactory));
    }

    [Fact]
    public void ExplicitRegister_DoesNotOverrideDiscovered_ButRegisterOrReplaceDoes()
    {
        var registry = CreateTestRegistry();
        var custom = new ZuluFactory();

        registry.Register(ContestedType, custom).Should().BeFalse();
        registry.GetFactory(ContestedType).Should().BeOfType<AlphaFactory>();

        registry.RegisterOrReplace(ContestedType, custom);
        registry.GetFactory(ContestedType).Should().BeSameAs(custom);
    }

    [Fact]
    public void ExplicitRegistration_OutranksLaterDiscovery()
    {
        var registry = new ResourceHandlerRegistry();
        var custom = new ZuluFactory();
        registry.Register(ContestedType, custom).Should().BeTrue();

        registry.DiscoverHandlers(typeof(ResourceHandlerPrecedenceTests).Assembly);

        registry.GetFactory(ContestedType).Should().BeSameAs(custom);
    }

    [Fact]
    public void DiscoveredTypeZeroHandler_WithoutReplacesDefault_IsIgnored()
    {
        var registry = CreateTestRegistry();

        registry.DefaultFactory.Should().NotBeOfType<WouldBeDefaultFactory>();
        registry.IgnoredDefaultHandlers.Should().Contain(typeof(WouldBeDefaultFactory));
    }

    [Fact]
    public void DiscoveredTypeZeroHandler_WithReplacesDefault_BecomesDefault()
    {
        var registry = CreateTestRegistry();

        registry.DefaultFactory.Should().BeOfType<IntendedDefaultFactory>();
    }

    [Fact]
    public void DisableWrapper_FallsBackToNextClaim_ThenDefault()
    {
        var registry = CreateTestRegistry();

        registry.DisableWrapper(ContestedType, typeof(AlphaFactory)).Should().BeTrue();
        registry.GetFactory(ContestedType).Should().BeOfType<ZuluFactory>();

        registry.DisableWrapper(ContestedType, typeof(ZuluFactory));
        registry.DisableWrapper(ContestedType, typeof(ListFactory));
        registry.GetFactory(ContestedType).Should().BeNull();
        registry.GetFactoryOrDefault(ContestedType).Should().BeSameAs(registry.DefaultFactory);

        registry.Disabled.Should().HaveCount(3);
        registry.EnableWrapper(ContestedType, typeof(AlphaFactory)).Should().BeTrue();
        registry.GetFactory(ContestedType).Should().BeOfType<AlphaFactory>();

        registry.ClearDisabled();
        registry.Disabled.Should().BeEmpty();
    }

    [Fact]
    public void DisabledWrapperStrings_RoundTrip()
    {
        var registry = CreateTestRegistry();
        registry.DisableWrapper(ContestedType, typeof(AlphaFactory));
        var saved = registry.GetDisabledWrappers();

        saved.Should().Equal($"0xFEED0001:{typeof(AlphaFactory).FullName}");

        var other = CreateTestRegistry();
        other.SetDisabledWrappers(saved).Should().BeEmpty();
        other.GetFactory(ContestedType).Should().BeOfType<ZuluFactory>();

        // Hex without "0x", lower case, and legacy trailing newline are accepted.
        other.SetDisabledWrappers([$"  feed0001:{typeof(AlphaFactory).FullName}\n"]).Should().BeEmpty();
        other.GetFactory(ContestedType).Should().BeOfType<ZuluFactory>();
    }

    [Fact]
    public void CreateResource_AlwaysDefault_UsesDefaultFactory()
    {
        var registry = new ResourceHandlerRegistry();
        registry.Register(ContestedType, new AlphaFactory());
        var key = new ResourceKey(ContestedType, 0, 1);

        registry.CreateResource(key, new byte[] { 1 }).Should().BeOfType<MarkerResource>();
        registry.CreateResource(key, new byte[] { 1 }, alwaysDefault: true).Should().BeOfType<DefaultResource>();
        registry.CreateNewResource(key).Should().BeOfType<MarkerResource>();
        registry.CreateNewResource(0x0BADF00Du).Should().BeOfType<DefaultResource>();
        registry.GetWrapperType(ContestedType).Should().Be<AlphaFactory>();
        registry.GetWrapperType(0x0BADF00D).Should().Be<DefaultResourceFactory>();
    }

    [Fact]
    public async Task Package_GetResourceAsync_UsesInjectedRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.Register(ContestedType, new AlphaFactory());
        using var package = DbpfPackage.CreateNew();
        package.ResourceHandlers = registry;
        var entry = package.AddResource(new ResourceKey(ContestedType, 0, 1), new byte[] { 9 })!;

        (await package.GetResourceAsync(entry)).Should().BeOfType<MarkerResource>();
        (await package.GetResourceAsync(entry, alwaysDefault: true)).Should().BeOfType<DefaultResource>();

        registry.DisableWrapper(ContestedType, typeof(AlphaFactory));
        (await package.GetResourceAsync(entry)).Should().BeOfType<DefaultResource>();
    }

    [Fact]
    public async Task Package_GetResourceAsync_DefaultsToSharedRegistry()
    {
        using var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(new ResourceKey(0x545AC67A, 0, 1), new byte[] { 1, 2, 3 })!;

        package.ResourceHandlers.Should().BeSameAs(ResourceHandlerRegistry.Shared);
        (await package.GetResourceAsync(entry, alwaysDefault: true)).Data.ToArray().Should().Equal(1, 2, 3);
    }
}

internal sealed class MarkerResource(ReadOnlyMemory<byte> data) : IResource
{
    public ReadOnlyMemory<byte> Data { get; } = data;
    public bool IsDirty => false;
#pragma warning disable CS0067 // never raised
    public event EventHandler? Changed;
#pragma warning restore CS0067
    public void Dispose() { }
}

internal abstract class MarkerFactoryBase : IResourceFactory
{
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data) => new MarkerResource(data);
    public IResource CreateEmpty(ResourceKey key) => new MarkerResource(ReadOnlyMemory<byte>.Empty);
}

[ResourceHandler(ResourceHandlerPrecedenceTests.ContestedType)]
internal sealed class ZuluFactory : MarkerFactoryBase;

[ResourceHandler(ResourceHandlerPrecedenceTests.ContestedType)]
[ResourceHandler(ResourceHandlerPrecedenceTests.PriorityType)]
internal sealed class AlphaFactory : MarkerFactoryBase;

// Zulu must win PriorityType despite sorting after Alpha.
[ResourceHandler(ResourceHandlerPrecedenceTests.PriorityType, Priority = 5)]
internal sealed class ZuluPriorityFactory : MarkerFactoryBase;

/// <summary>A faulty plugin factory: its type list throws an exception type the host does not expect.</summary>
internal sealed class ThrowingListFactory : MarkerFactoryBase, IResourceTypeProvider
{
    public IReadOnlyCollection<uint> ResourceTypes => throw new KeyNotFoundException("plugin bug");
}

internal sealed class ListFactory : MarkerFactoryBase, IResourceTypeProvider
{
    public IReadOnlyCollection<uint> ResourceTypes =>
        [ResourceHandlerPrecedenceTests.ContestedType, ResourceHandlerPrecedenceTests.ListOnlyType];
}

/// <summary>An in-memory assembly listing the given types, for discovery tests.</summary>
internal sealed class FakeAssembly(Type[] types) : Assembly
{
    public override Type[] GetTypes() => types;
    public override string FullName => "FakePluginAssembly";
    public override string Location => string.Empty;
}

/// <summary>
/// A plugin handler type (looks like <see cref="AlphaFactory"/>) whose metadata cannot be read: every attribute
/// query throws TypeInitializationException.
/// </summary>
internal sealed class TypeInitFailingType() : TypeDelegator(typeof(AlphaFactory))
{
    public override string FullName => "FakePlugin.BrokenFactory";

    public override object[] GetCustomAttributes(bool inherit) => throw Failure();

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw Failure();

    private static TypeInitializationException Failure() =>
        new("FakePlugin.BrokenFactory", new InvalidOperationException("plugin static constructor bug"));
}

[ResourceHandler(0)]
internal sealed class WouldBeDefaultFactory : MarkerFactoryBase;

/// <summary>
/// Intended default replacement. It delegates to <see cref="DefaultResourceFactory"/> so that any registry built
/// by scanning all loaded assemblies (e.g. <see cref="ResourceHandlerRegistry.Shared"/>) still yields DefaultResource.
/// </summary>
[ResourceHandler(0, ReplacesDefault = true)]
internal sealed class IntendedDefaultFactory : IResourceFactory
{
    private readonly DefaultResourceFactory _inner = new();
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data) => _inner.Create(key, data);
    public IResource CreateEmpty(ResourceKey key) => _inner.CreateEmpty(key);
}
