using Microsoft.Extensions.Options;

namespace TS4Tools.Extensions.Tests;

/// <summary>
/// Tests for the <see cref="ServiceCollectionExtensions"/> class.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTS4ToolsExtensions_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTS4ToolsExtensions();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IResourceTypeRegistry>().Should().NotBeNull();
        serviceProvider.GetService<IFileNameService>().Should().NotBeNull();
    }

    [Fact]
    public void AddTS4ToolsExtensions_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => ((IServiceCollection)null!).AddTS4ToolsExtensions();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddTS4ToolsExtensions_WithConfiguration_RegistersServicesAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTS4ToolsExtensions(options =>
        {
            options.MaxFileNameLength = 200;
            options.UseSanitizedFilenames = false;
            options.EnableExtendedResourceTypes = false;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IResourceTypeRegistry>().Should().NotBeNull();
        serviceProvider.GetService<IFileNameService>().Should().NotBeNull();

        var options = serviceProvider.GetService<IOptions<ExtensionOptions>>();
        options.Should().NotBeNull();
        options!.Value.MaxFileNameLength.Should().Be(200);
        options.Value.UseSanitizedFilenames.Should().BeFalse();
        options.Value.EnableExtendedResourceTypes.Should().BeFalse();
    }

    [Fact]
    public void AddTS4ToolsExtensions_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddTS4ToolsExtensions((Action<ExtensionOptions>)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configureOptions");
    }

    [Fact]
    public void AddTS4ToolsExtensions_RegistersSingletonResourceTypeRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTS4ToolsExtensions();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry1 = serviceProvider.GetService<IResourceTypeRegistry>();
        var registry2 = serviceProvider.GetService<IResourceTypeRegistry>();

        registry1.Should().BeSameAs(registry2);
    }

    [Fact]
    public void AddTS4ToolsExtensions_RegistersScopedFileNameService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTS4ToolsExtensions();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IFileNameService>();
        var service2 = scope2.ServiceProvider.GetService<IFileNameService>();

        service1.Should().NotBeSameAs(service2);
    }

    [Fact]
    public void ExtensionOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new ExtensionOptions();

        // Assert
        options.EnableExtendedResourceTypes.Should().BeTrue();
        options.MaxFileNameLength.Should().Be(240);
        options.UseSanitizedFilenames.Should().BeTrue();
        options.CustomResourceTypes.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(49)]
    [InlineData(256)]
    public void ExtensionOptions_MaxFileNameLength_ValidatesRange(int invalidLength)
    {
        // Arrange
        var options = new ExtensionOptions { MaxFileNameLength = invalidLength };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(ExtensionOptions.MaxFileNameLength)));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(255)]
    [InlineData(150)]
    public void ExtensionOptions_MaxFileNameLength_AcceptsValidRange(int validLength)
    {
        // Arrange
        var options = new ExtensionOptions { MaxFileNameLength = validLength };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void ExtensionOptions_CustomResourceTypes_CanBeModified()
    {
        // Arrange
        var options = new ExtensionOptions();

        // Act
        options.AddCustomResourceType("12345678", "CUSTOM", ".custom");

        // Assert
        options.CustomResourceTypes.Should().ContainKey("12345678");
        options.CustomResourceTypes["12345678"].Should().Be(("CUSTOM", ".custom"));
    }
}
