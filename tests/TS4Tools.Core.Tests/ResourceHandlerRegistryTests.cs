using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="ResourceHandlerRegistry"/>.
///
/// The registry maps resource type IDs to factories that can parse/create resources
/// of that type. It supports auto-discovery via reflection and the ResourceHandlerAttribute.
/// </summary>
public class ResourceHandlerRegistryTests
{
    private const uint TestTypeId1 = 0x12345678;
    private const uint TestTypeId2 = 0x87654321;

    [Fact]
    public void Constructor_CreatesWithDefaultFactory()
    {
        var registry = new ResourceHandlerRegistry();

        registry.DefaultFactory.Should().NotBeNull();
        registry.DefaultFactory.Should().BeOfType<DefaultResourceFactory>();
    }

    [Fact]
    public void Register_NewType_ReturnsTrue()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new MockResourceFactory();

        var result = registry.Register(TestTypeId1, factory);

        result.Should().BeTrue();
        registry.RegisteredTypes.Should().Contain(TestTypeId1);
    }

    [Fact]
    public void Register_DuplicateType_ReturnsFalse()
    {
        var registry = new ResourceHandlerRegistry();
        var factory1 = new MockResourceFactory();
        var factory2 = new MockResourceFactory();

        registry.Register(TestTypeId1, factory1);
        var result = registry.Register(TestTypeId1, factory2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Register_NullFactory_ThrowsArgumentNullException()
    {
        var registry = new ResourceHandlerRegistry();

        var act = () => registry.Register(TestTypeId1, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFactory_RegisteredType_ReturnsFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new MockResourceFactory();
        registry.Register(TestTypeId1, factory);

        var result = registry.GetFactory(TestTypeId1);

        result.Should().BeSameAs(factory);
    }

    [Fact]
    public void GetFactory_UnregisteredType_ReturnsNull()
    {
        var registry = new ResourceHandlerRegistry();

        var result = registry.GetFactory(TestTypeId1);

        result.Should().BeNull();
    }

    [Fact]
    public void GetFactoryOrDefault_RegisteredType_ReturnsFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new MockResourceFactory();
        registry.Register(TestTypeId1, factory);

        var result = registry.GetFactoryOrDefault(TestTypeId1);

        result.Should().BeSameAs(factory);
    }

    [Fact]
    public void GetFactoryOrDefault_UnregisteredType_ReturnsDefaultFactory()
    {
        var registry = new ResourceHandlerRegistry();

        var result = registry.GetFactoryOrDefault(TestTypeId1);

        result.Should().BeSameAs(registry.DefaultFactory);
    }

    [Fact]
    public void RegisterOrReplace_NewType_RegistersFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new MockResourceFactory();

        registry.RegisterOrReplace(TestTypeId1, factory);

        registry.GetFactory(TestTypeId1).Should().BeSameAs(factory);
    }

    [Fact]
    public void RegisterOrReplace_ExistingType_ReplacesFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory1 = new MockResourceFactory();
        var factory2 = new MockResourceFactory();
        registry.Register(TestTypeId1, factory1);

        registry.RegisterOrReplace(TestTypeId1, factory2);

        registry.GetFactory(TestTypeId1).Should().BeSameAs(factory2);
    }

    [Fact]
    public void RegisterOrReplace_NullFactory_ThrowsArgumentNullException()
    {
        var registry = new ResourceHandlerRegistry();

        var act = () => registry.RegisterOrReplace(TestTypeId1, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unregister_ExistingType_RemovesFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new MockResourceFactory();
        registry.Register(TestTypeId1, factory);

        var result = registry.Unregister(TestTypeId1);

        result.Should().BeTrue();
        registry.GetFactory(TestTypeId1).Should().BeNull();
        registry.RegisteredTypes.Should().NotContain(TestTypeId1);
    }

    [Fact]
    public void Unregister_NonExistingType_ReturnsFalse()
    {
        var registry = new ResourceHandlerRegistry();

        var result = registry.Unregister(TestTypeId1);

        result.Should().BeFalse();
    }

    [Fact]
    public void DefaultFactory_Set_UpdatesValue()
    {
        var registry = new ResourceHandlerRegistry();
        var customDefault = new MockResourceFactory();

        registry.DefaultFactory = customDefault;

        registry.DefaultFactory.Should().BeSameAs(customDefault);
    }

    [Fact]
    public void DefaultFactory_SetNull_ThrowsArgumentNullException()
    {
        var registry = new ResourceHandlerRegistry();

        var act = () => registry.DefaultFactory = null!;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteredTypes_ReturnsAllKeys()
    {
        var registry = new ResourceHandlerRegistry();
        registry.Register(TestTypeId1, new MockResourceFactory());
        registry.Register(TestTypeId2, new MockResourceFactory());

        var types = registry.RegisteredTypes;

        types.Should().HaveCount(2);
        types.Should().Contain(TestTypeId1);
        types.Should().Contain(TestTypeId2);
    }

    [Fact]
    public void RegisteredTypes_EmptyRegistry_ReturnsEmptyCollection()
    {
        var registry = new ResourceHandlerRegistry();

        var types = registry.RegisteredTypes;

        types.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverHandlers_NullAssembly_ThrowsArgumentNullException()
    {
        var registry = new ResourceHandlerRegistry();

        var act = () => registry.DiscoverHandlers(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Mock factory for testing the registry without needing real resource implementations.
    /// </summary>
    private sealed class MockResourceFactory : IResourceFactory
    {
        public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
        {
            throw new NotImplementedException("Mock factory - not meant to be called");
        }

        public IResource CreateEmpty(ResourceKey key)
        {
            throw new NotImplementedException("Mock factory - not meant to be called");
        }
    }
}
