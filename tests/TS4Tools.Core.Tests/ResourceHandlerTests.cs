using FluentAssertions;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Core.Tests;

public class ResourceHandlerTests
{
    [Fact]
    public void DefaultResourceFactory_Create_ReturnsDefaultResource()
    {
        var factory = new DefaultResourceFactory();
        var key = new ResourceKey(0x220557DA, 0, 123);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var resource = factory.Create(key, data);

        resource.Should().NotBeNull();
        resource.Should().BeOfType<DefaultResource>();
        resource.Data.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public void DefaultResourceFactory_CreateEmpty_ReturnsEmptyResource()
    {
        var factory = new DefaultResourceFactory();
        var key = new ResourceKey(0x220557DA, 0, 123);

        var resource = factory.CreateEmpty(key);

        resource.Should().NotBeNull();
        resource.Data.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void DefaultResource_SetData_RaisesChangedEvent()
    {
        var key = new ResourceKey(1, 2, 3);
        var resource = new DefaultResource(key, new byte[] { 1, 2, 3 });
        bool eventRaised = false;
        resource.Changed += (_, _) => eventRaised = true;

        resource.SetData(new byte[] { 4, 5, 6 });

        eventRaised.Should().BeTrue();
        resource.IsDirty.Should().BeTrue();
        resource.Data.ToArray().Should().BeEquivalentTo(new byte[] { 4, 5, 6 });
    }

    [Fact]
    public void ResourceHandlerRegistry_DefaultFactory_IsSet()
    {
        var registry = new ResourceHandlerRegistry();

        registry.DefaultFactory.Should().NotBeNull();
        registry.DefaultFactory.Should().BeOfType<DefaultResourceFactory>();
    }

    [Fact]
    public void ResourceHandlerRegistry_Register_AddsFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new DefaultResourceFactory();
        const uint resourceType = 0x220557DA;

        bool registered = registry.Register(resourceType, factory);

        registered.Should().BeTrue();
        registry.GetFactory(resourceType).Should().Be(factory);
        registry.RegisteredTypes.Should().Contain(resourceType);
    }

    [Fact]
    public void ResourceHandlerRegistry_Register_RejectsDuplicate()
    {
        var registry = new ResourceHandlerRegistry();
        var factory1 = new DefaultResourceFactory();
        var factory2 = new DefaultResourceFactory();
        const uint resourceType = 0x220557DA;

        registry.Register(resourceType, factory1);
        bool registered = registry.Register(resourceType, factory2);

        registered.Should().BeFalse();
        registry.GetFactory(resourceType).Should().Be(factory1);
    }

    [Fact]
    public void ResourceHandlerRegistry_RegisterOrReplace_ReplacesExisting()
    {
        var registry = new ResourceHandlerRegistry();
        var factory1 = new DefaultResourceFactory();
        var factory2 = new DefaultResourceFactory();
        const uint resourceType = 0x220557DA;

        registry.Register(resourceType, factory1);
        registry.RegisterOrReplace(resourceType, factory2);

        registry.GetFactory(resourceType).Should().Be(factory2);
    }

    [Fact]
    public void ResourceHandlerRegistry_Unregister_RemovesFactory()
    {
        var registry = new ResourceHandlerRegistry();
        var factory = new DefaultResourceFactory();
        const uint resourceType = 0x220557DA;

        registry.Register(resourceType, factory);
        bool unregistered = registry.Unregister(resourceType);

        unregistered.Should().BeTrue();
        registry.GetFactory(resourceType).Should().BeNull();
        registry.RegisteredTypes.Should().NotContain(resourceType);
    }

    [Fact]
    public void ResourceHandlerRegistry_GetFactoryOrDefault_ReturnsFallback()
    {
        var registry = new ResourceHandlerRegistry();
        const uint unknownType = 0x12345678;

        var factory = registry.GetFactoryOrDefault(unknownType);

        factory.Should().Be(registry.DefaultFactory);
    }

    [Fact]
    public void ResourceHandlerRegistry_GetFactory_ReturnsNullForUnknown()
    {
        var registry = new ResourceHandlerRegistry();
        const uint unknownType = 0x12345678;

        var factory = registry.GetFactory(unknownType);

        factory.Should().BeNull();
    }
}
