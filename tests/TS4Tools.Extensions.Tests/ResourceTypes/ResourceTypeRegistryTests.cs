using Microsoft.Extensions.Logging.Abstractions;

namespace TS4Tools.Extensions.Tests.ResourceTypes;

/// <summary>
/// Tests for the <see cref="ResourceTypeRegistry"/> class.
/// </summary>
public sealed class ResourceTypeRegistryTests
{
    private readonly ResourceTypeRegistry _registry;

    public ResourceTypeRegistryTests()
    {
        _registry = new ResourceTypeRegistry(NullLogger<ResourceTypeRegistry>.Instance);
    }

    [Fact]
    public void Constructor_WithValidLogger_InitializesWithDefaultTypes()
    {
        // Act & Assert
        _registry.GetSupportedTypes().Should().NotBeEmpty();
        _registry.IsSupported(0x00B2D882).Should().BeTrue(); // DDS
        _registry.GetTag(0x00B2D882).Should().Be("DDS");
        _registry.GetExtension(0x00B2D882).Should().Be(".dds");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResourceTypeRegistry(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GetExtension_WithKnownType_ReturnsCorrectExtension()
    {
        // Arrange
        const uint resourceType = 0x220557DA; // STBL

        // Act
        var extension = _registry.GetExtension(resourceType);

        // Assert
        extension.Should().Be(".stbl");
    }

    [Fact]
    public void GetExtension_WithUnknownType_ReturnsNull()
    {
        // Arrange
        const uint unknownType = 0xDEADBEEF;

        // Act
        var extension = _registry.GetExtension(unknownType);

        // Assert
        extension.Should().BeNull();
    }

    [Fact]
    public void GetTag_WithKnownType_ReturnsCorrectTag()
    {
        // Arrange
        const uint resourceType = 0x220557DA; // STBL

        // Act
        var tag = _registry.GetTag(resourceType);

        // Assert
        tag.Should().Be("STBL");
    }

    [Fact]
    public void GetTag_WithUnknownType_ReturnsNull()
    {
        // Arrange
        const uint unknownType = 0xDEADBEEF;

        // Act
        var tag = _registry.GetTag(unknownType);

        // Assert
        tag.Should().BeNull();
    }

    [Fact]
    public void IsSupported_WithKnownType_ReturnsTrue()
    {
        // Arrange
        const uint resourceType = 0x220557DA; // STBL

        // Act
        var isSupported = _registry.IsSupported(resourceType);

        // Assert
        isSupported.Should().BeTrue();
    }

    [Fact]
    public void IsSupported_WithUnknownType_ReturnsFalse()
    {
        // Arrange
        const uint unknownType = 0xDEADBEEF;

        // Act
        var isSupported = _registry.IsSupported(unknownType);

        // Assert
        isSupported.Should().BeFalse();
    }

    [Fact]
    public void RegisterType_WithValidParameters_RegistersSuccessfully()
    {
        // Arrange
        const uint newType = 0x12345678;
        const string tag = "TEST";
        const string extension = ".test";

        // Act
        _registry.RegisterType(newType, tag, extension);

        // Assert
        _registry.IsSupported(newType).Should().BeTrue();
        _registry.GetTag(newType).Should().Be(tag);
        _registry.GetExtension(newType).Should().Be(extension);
    }

    [Fact]
    public void RegisterType_WithExistingType_UpdatesRegistration()
    {
        // Arrange
        const uint existingType = 0x00B2D882; // DDS
        const string newTag = "NEWTAG";
        const string newExtension = ".new";

        // Act
        _registry.RegisterType(existingType, newTag, newExtension);

        // Assert
        _registry.GetTag(existingType).Should().Be(newTag);
        _registry.GetExtension(existingType).Should().Be(newExtension);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterType_WithInvalidTag_ThrowsArgumentException(string? invalidTag)
    {
        // Act & Assert
        var act = () => _registry.RegisterType(0x12345678, invalidTag!, ".test");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterType_WithInvalidExtension_ThrowsArgumentException(string? invalidExtension)
    {
        // Act & Assert
        var act = () => _registry.RegisterType(0x12345678, "TEST", invalidExtension!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetSupportedTypes_ReturnsAllRegisteredTypes()
    {
        // Arrange
        const uint newType = 0x12345678;
        _registry.RegisterType(newType, "TEST", ".test");

        // Act
        var supportedTypes = _registry.GetSupportedTypes().ToList();

        // Assert
        supportedTypes.Should().Contain(newType);
        supportedTypes.Should().Contain(0x00B2D882); // Default DDS type
        supportedTypes.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentRegistrations_HandledCorrectly()
    {
        // Arrange
        const int threadCount = 10;
        const int registrationsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act
        for (var i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < registrationsPerThread; j++)
                {
                    var resourceType = (uint)((threadIndex * registrationsPerThread) + j + 0x10000000);
                    _registry.RegisterType(resourceType, $"T{threadIndex}_{j}", $".t{threadIndex}_{j}");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var supportedTypes = _registry.GetSupportedTypes().ToList();
        supportedTypes.Should().HaveCountGreaterOrEqualTo(threadCount * registrationsPerThread);

        // Verify some specific registrations
        _registry.IsSupported(0x10000000).Should().BeTrue();
        _registry.GetTag(0x10000000).Should().Be("T0_0");
        _registry.GetExtension(0x10000000).Should().Be(".t0_0");
    }
}
