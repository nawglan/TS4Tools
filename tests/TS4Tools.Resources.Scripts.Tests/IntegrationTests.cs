using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Integration tests for the script resources package.
/// </summary>
public sealed class IntegrationTests
{
    [Fact]
    public void ServiceRegistration_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetRequiredService<IResourceFactory<IScriptResource>>();
        factory.Should().NotBeNull();
        factory.Should().BeOfType<ScriptResourceFactory>();
    }

    [Fact]
    public async Task EndToEndScriptResourceHandling_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IResourceFactory<IScriptResource>>();
        var resourceKey = new ResourceKey(0x12345678, ScriptResourceFactory.ScriptResourceType, 0x9ABCDEF0);

        // Create test script data
        var testData = CreateCompleteTestScriptData();
        using var dataStream = new MemoryStream(testData);

        // Act
        var resource = await factory.CreateResourceAsync(1, dataStream);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Version.Should().Be(2);
            resource.GameVersion.Should().Be("TestGameVersion");
            resource.Unknown2.Should().Be(0x2BC4F79F);

            // Test round-trip serialization
            var serializedData = resource.AsBytes;
            serializedData.Length.Should().BeGreaterThan(0);

            // Create new resource from serialized data
            using var serializedStream = new MemoryStream(serializedData);
            var deserializedResource = await factory.CreateResourceAsync(1, serializedStream);

            using (deserializedResource)
            {
                deserializedResource.Version.Should().Be(resource.Version);
                deserializedResource.GameVersion.Should().Be(resource.GameVersion);
                deserializedResource.Unknown2.Should().Be(resource.Unknown2);
            }
        }
    }

    [Fact]
    public void ScriptResourceFactory_ShouldIntegrateWithResourceManager()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IResourceFactory<IScriptResource>>();

        // Act
        var canCreateScript = factory.CanCreateResource(ScriptResourceFactory.ScriptResourceType);
        var canCreateOther = factory.CanCreateResource(0x12345678);

        // Assert
        canCreateScript.Should().BeTrue();
        canCreateOther.Should().BeFalse();
        factory.Priority.Should().Be(100);
        factory.SupportedResourceTypes.Should().Contain($"0x{ScriptResourceFactory.ScriptResourceType:X8}");
    }

    [Fact]
    public async Task ScriptResource_WithAssemblyInfoExtraction_ShouldHandleEmptyData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IResourceFactory<IScriptResource>>();
        var resourceKey = new ResourceKey(1, ScriptResourceFactory.ScriptResourceType, 1);

        using var emptyStream = new MemoryStream();

        // Act
        var resource = await factory.CreateResourceAsync(1, emptyStream);
        var assemblyInfo = await resource.GetAssemblyInfoAsync();

        // Assert
        using (resource)
        {
            assemblyInfo.FullName.Should().Be("No assembly data");
            assemblyInfo.Location.Should().BeEmpty();
            assemblyInfo.ExportedTypes.Should().BeEmpty();
            assemblyInfo.ReferencedAssemblies.Should().BeEmpty();
            assemblyInfo.Properties.Should().BeEmpty();
        }
    }

    [Fact]
    public void ScriptResource_PropertyValidation_ShouldEnforceConstraints()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<ScriptResource>>();
        using var resource = new ScriptResource(logger);

        // Act & Assert
        resource.Invoking(r => r.GameVersion = null!)
            .Should().Throw<ArgumentNullException>();

        resource.Invoking(r => r.MD5Sum = new byte[32])
            .Should().Throw<ArgumentException>();

        // Valid operations should work
        resource.GameVersion = "ValidVersion";
        resource.MD5Sum = new byte[64];
        resource.SetAssemblyData(new byte[] { 1, 2, 3 });

        resource.GameVersion.Should().Be("ValidVersion");
        resource.MD5Sum.Length.Should().Be(64);
        resource.AssemblyData.Length.Should().Be(3);
    }

    [Fact]
    public void ScriptResource_Disposal_ShouldCleanUpResources()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<ScriptResource>>();
        var resource = new ScriptResource(logger);

        // Set some data
        resource.MD5Sum = new byte[64];
        Array.Fill<byte>(resource.MD5Sum.ToArray(), 0x42);
        resource.SetAssemblyData(new byte[] { 1, 2, 3, 4 });

        // Act
        resource.Dispose();

        // Assert
        // AssemblyData should be all zeros but keep original length
        resource.AssemblyData.ToArray().Should().OnlyContain(b => b == 0);
        resource.MD5Sum.ToArray().Should().OnlyContain(b => b == 0);

        // Operations after disposal should throw
        resource.Invoking(r => r.GetAssemblyInfoAsync())
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Creates comprehensive test script resource data.
    /// </summary>
    private static byte[] CreateCompleteTestScriptData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write version (2 to include game version)
        writer.Write((byte)2);

        // Write game version
        var gameVersionBytes = System.Text.Encoding.Unicode.GetBytes("TestGameVersion");
        writer.Write(gameVersionBytes.Length / 2);
        writer.Write(gameVersionBytes);

        // Write unknown2
        writer.Write(0x2BC4F79Fu);

        // Write MD5 sum (64 bytes)
        var md5Sum = new byte[64];
        Array.Fill<byte>(md5Sum, 0x42);
        writer.Write(md5Sum);

        // Write entry count (1 entry for test)
        writer.Write((ushort)1);

        // Write MD5 table (8 bytes per entry)
        var md5Table = new byte[8];
        Array.Fill<byte>(md5Table, 0x33);
        writer.Write(md5Table);

        // Write encrypted data (512 bytes per entry)
        var encryptedData = new byte[512];
        Array.Fill<byte>(encryptedData, 0x55);
        writer.Write(encryptedData);

        return stream.ToArray();
    }
}
