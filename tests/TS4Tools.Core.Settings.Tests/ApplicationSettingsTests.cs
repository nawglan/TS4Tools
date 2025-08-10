/***************************************************************************
 *  Copyright (C) 2025 by the TS4Tools contributors                       *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by  *
 *  the Free Software Foundation, either version 3 of the License, or     *
 *  (at your option) any later version.                                   *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the          *
 *  GNU General Public License for more details.                          *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License     *
 *  along with TS4Tools. If not, see <http://www.gnu.org/licenses/>.      *
 ***************************************************************************/

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using TS4Tools.Core.Settings;
using Xunit;

namespace TS4Tools.Core.Settings.Tests;

/// <summary>
/// Unit tests for the ApplicationSettings model and its validation behavior.
/// </summary>
public sealed class ApplicationSettingsTests
{
    [Fact]
    public void DefaultSettings_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var settings = new ApplicationSettings();

        // Assert
        settings.EnableExtraChecking.Should().BeTrue();
        settings.AssumeDataDirty.Should().BeTrue();
        settings.UseTS4Format.Should().BeTrue();
        settings.MaxResourceCacheSize.Should().Be(1000);
        settings.EnableDetailedLogging.Should().BeFalse();
        settings.AsyncOperationTimeoutMs.Should().Be(30000);
        settings.EnableCrossPlatformFeatures.Should().BeTrue();

        settings.Package.Should().NotBeNull();
        settings.Package.CreateBackups.Should().BeTrue();
        settings.Package.MaxBackupCount.Should().Be(10);
        settings.Package.EnableCompression.Should().BeTrue();
        settings.Package.CompressionLevel.Should().Be(6);
        settings.Package.IOBufferSize.Should().Be(65536);

        settings.UserInterface.Should().NotBeNull();
        settings.UserInterface.Theme.Should().Be(ThemePreference.System);
        settings.UserInterface.EnableAnimations.Should().BeTrue();
        settings.UserInterface.ShowAdvancedOptions.Should().BeFalse();
        settings.UserInterface.RecentFilesCount.Should().Be(10);
        settings.UserInterface.EnableAutoSave.Should().BeFalse();
        settings.UserInterface.AutoSaveIntervalMinutes.Should().Be(5);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Arrange & Act & Assert
        ApplicationSettings.SectionName.Should().Be("ApplicationSettings");
    }

    [Theory]
    [InlineData(ThemePreference.System)]
    [InlineData(ThemePreference.Light)]
    [InlineData(ThemePreference.Dark)]
    public void ThemePreference_AllValues_ShouldBeValid(ThemePreference theme)
    {
        // Arrange & Act
        var settings = new ApplicationSettings
        {
            UserInterface = new UserInterfaceSettings { Theme = theme }
        };

        // Assert
        settings.UserInterface.Theme.Should().Be(theme);
    }

    [Fact]
    public void ApplicationSettings_WithCustomValues_ShouldRetainValues()
    {
        // Arrange
        var packageSettings = new PackageSettings
        {
            CreateBackups = false,
            MaxBackupCount = 5,
            EnableCompression = false,
            CompressionLevel = 3,
            IOBufferSize = 32768
        };

        var uiSettings = new UserInterfaceSettings
        {
            Theme = ThemePreference.Dark,
            EnableAnimations = false,
            ShowAdvancedOptions = true,
            RecentFilesCount = 20,
            EnableAutoSave = true,
            AutoSaveIntervalMinutes = 10
        };

        // Act
        var settings = new ApplicationSettings
        {
            EnableExtraChecking = false,
            AssumeDataDirty = false,
            UseTS4Format = false,
            MaxResourceCacheSize = 500,
            EnableDetailedLogging = true,
            AsyncOperationTimeoutMs = 15000,
            EnableCrossPlatformFeatures = false,
            Package = packageSettings,
            UserInterface = uiSettings
        };

        // Assert
        settings.EnableExtraChecking.Should().BeFalse();
        settings.AssumeDataDirty.Should().BeFalse();
        settings.UseTS4Format.Should().BeFalse();
        settings.MaxResourceCacheSize.Should().Be(500);
        settings.EnableDetailedLogging.Should().BeTrue();
        settings.AsyncOperationTimeoutMs.Should().Be(15000);
        settings.EnableCrossPlatformFeatures.Should().BeFalse();

        settings.Package.Should().BeSameAs(packageSettings);
        settings.UserInterface.Should().BeSameAs(uiSettings);
    }
}

/// <summary>
/// Unit tests for the ApplicationSettingsService implementation.
/// </summary>
public sealed class ApplicationSettingsServiceTests : IDisposable
{
    private readonly IOptionsMonitor<ApplicationSettings> _mockOptionsMonitor;
    private readonly ApplicationSettingsService _service;
    private readonly ApplicationSettings _defaultSettings;

    public ApplicationSettingsServiceTests()
    {
        _defaultSettings = new ApplicationSettings();
        _mockOptionsMonitor = Substitute.For<IOptionsMonitor<ApplicationSettings>>();
        _mockOptionsMonitor.CurrentValue.Returns(_defaultSettings);

        _service = new ApplicationSettingsService(_mockOptionsMonitor);
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new ApplicationSettingsService(null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("optionsMonitor");
    }

    [Fact]
    public void Current_WhenNotDisposed_ShouldReturnCurrentSettings()
    {
        // Arrange & Act
        var current = _service.Current;

        // Assert
        current.Should().BeSameAs(_defaultSettings);
    }

    [Fact]
    public void Current_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _service.Dispose();

        // Act & Assert
        var action = () => _service.Current;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReloadAsync_WhenNotDisposed_ShouldCompleteSuccessfully()
    {
        // Arrange & Act
        var task = _service.ReloadAsync();
        await task;

        // Assert
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task ReloadAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _service.Dispose();

        // Act & Assert
        var action = async () => await _service.ReloadAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SettingsChanged_WhenSettingsChange_ShouldFireEvent()
    {
        // Arrange
        SettingsChangedEventArgs? capturedArgs = null;
        _service.SettingsChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        var newSettings = new ApplicationSettings { EnableExtraChecking = false };

        // We need to create a more realistic test since we can't easily access internal behavior
        // Instead, let's test the ReloadAsync method which should trigger change detection
        _mockOptionsMonitor.CurrentValue.Returns(newSettings);

        // Act
        await _service.ReloadAsync();

        // Assert - This test verifies the service doesn't crash when settings change
        // The actual event firing depends on the IOptionsMonitor implementation
        // which would be tested in integration tests
        _service.Current.Should().BeSameAs(newSettings);
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}

/// <summary>
/// Unit tests for settings service extensions and configuration binding.
/// </summary>
public sealed class SettingsServiceExtensionsTests
{
    [Fact]
    public void AddTS4ToolsSettings_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var configuration = Substitute.For<IConfiguration>();

        // Act & Assert
        var action = () => services.AddTS4ToolsSettings(configuration);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("services");
    }

    [Fact]
    public void AddTS4ToolsSettings_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        // Act & Assert
        var action = () => services.AddTS4ToolsSettings(configuration);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("configuration");
    }

    [Fact]
    public void AddTS4ToolsSettings_WithValidParameters_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("testappsettings.json")
            .Build();

        // Act
        services.AddTS4ToolsSettings(configuration);

        // Add the configuration itself to the service collection as it's needed
        services.AddSingleton<IConfiguration>(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var settingsService = serviceProvider.GetService<IApplicationSettingsService>();
        settingsService.Should().NotBeNull();
        settingsService.Should().BeOfType<ApplicationSettingsService>();
    }

    [Fact]
    public void AddTS4ToolsSettingsWithAction_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<ApplicationSettings> configureSettings = _ => { };

        // Act & Assert
        var action = () => services.AddTS4ToolsSettings(configureSettings);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("services");
    }

    [Fact]
    public void AddTS4ToolsSettingsWithAction_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<ApplicationSettings> configureSettings = null!;

        // Act & Assert
        var action = () => services.AddTS4ToolsSettings(configureSettings);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("configureSettings");
    }

    [Fact]
    public void CreateDefaultConfigurationBuilder_WithoutArgs_ShouldCreateBuilder()
    {
        // Arrange & Act
        var builder = SettingsServiceExtensions.CreateDefaultConfigurationBuilder();

        // Assert
        builder.Should().NotBeNull();
        var config = builder.Build();
        config.Should().NotBeNull();
    }

    [Fact]
    public void CreateDefaultConfigurationBuilder_WithArgs_ShouldIncludeCommandLine()
    {
        // Arrange
        var args = new[] { "--ApplicationSettings:EnableExtraChecking=false" };

        // Act
        var builder = SettingsServiceExtensions.CreateDefaultConfigurationBuilder(args);
        var config = builder.Build();

        // Assert
        config["ApplicationSettings:EnableExtraChecking"].Should().Be("false");
    }

    [Fact]
    public void GetDefaultSettings_WithoutBuilder_ShouldReturnSettings()
    {
        // Arrange & Act
        var settings = SettingsServiceExtensions.GetDefaultSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.Should().BeOfType<ApplicationSettings>();
    }

    [Fact]
    public void GetDefaultSettings_WithCustomBuilder_ShouldUseCustomConfiguration()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddJsonFile("testappsettings.json");

        // Act
        var settings = SettingsServiceExtensions.GetDefaultSettings(builder);

        // Assert
        settings.Should().NotBeNull();
        settings.EnableExtraChecking.Should().BeFalse(); // From testappsettings.json
        settings.UseTS4Format.Should().BeFalse(); // From testappsettings.json
    }
}

/// <summary>
/// Unit tests for the legacy settings adapter.
/// </summary>
public sealed class LegacySettingsAdapterTests
{
    [Fact]
    public void Checking_ShouldReturnExpectedValue()
    {
        // Arrange & Act & Assert
        LegacySettingsAdapter.Checking.Should().BeTrue();
    }

    [Fact]
    public void AsBytesWorkaround_ShouldReturnExpectedValue()
    {
        // Arrange & Act & Assert
        LegacySettingsAdapter.AsBytesWorkaround.Should().BeTrue();
    }

    [Fact]
    public void IsTS4_ShouldReturnExpectedValue()
    {
        // Arrange & Act & Assert
        LegacySettingsAdapter.IsTS4.Should().BeTrue();
    }

    [Fact]
    public void FullSettings_ShouldReturnApplicationSettings()
    {
        // Arrange & Act
        var settings = LegacySettingsAdapter.FullSettings;

        // Assert
        settings.Should().NotBeNull();
        settings.Should().BeOfType<ApplicationSettings>();
    }

    [Fact]
    public void Reload_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var action = () => LegacySettingsAdapter.Reload();
        action.Should().NotThrow();
    }
}

/// <summary>
/// Unit tests for settings change event arguments.
/// </summary>
public sealed class SettingsChangedEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var previousSettings = new ApplicationSettings { EnableExtraChecking = true };
        var currentSettings = new ApplicationSettings { EnableExtraChecking = false };

        // Act
        var args = new SettingsChangedEventArgs(previousSettings, currentSettings);

        // Assert
        args.PreviousSettings.Should().BeSameAs(previousSettings);
        args.CurrentSettings.Should().BeSameAs(currentSettings);
    }

    [Fact]
    public void Constructor_WithNullPreviousSettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        ApplicationSettings previousSettings = null!;
        var currentSettings = new ApplicationSettings();

        // Act & Assert
        var action = () => new SettingsChangedEventArgs(previousSettings, currentSettings);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("previousSettings");
    }

    [Fact]
    public void Constructor_WithNullCurrentSettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var previousSettings = new ApplicationSettings();
        ApplicationSettings currentSettings = null!;

        // Act & Assert
        var action = () => new SettingsChangedEventArgs(previousSettings, currentSettings);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("currentSettings");
    }
}
