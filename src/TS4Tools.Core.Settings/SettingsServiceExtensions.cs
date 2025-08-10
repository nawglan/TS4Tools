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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TS4Tools.Core.Settings;

/// <summary>
/// Extension methods for configuring TS4Tools settings in dependency injection containers.
/// </summary>
public static class SettingsServiceExtensions
{
    /// <summary>
    /// Adds TS4Tools settings services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration root to bind settings from.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
    public static IServiceCollection AddTS4ToolsSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure strongly-typed settings with validation
        services.Configure<ApplicationSettings>(
            configuration.GetSection(ApplicationSettings.SectionName));

        // Enable data annotation validation for settings
        services.AddOptions<ApplicationSettings>()
            .BindConfiguration(ApplicationSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the settings service
        services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();

        return services;
    }

    /// <summary>
    /// Adds TS4Tools settings services with custom configuration action.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureSettings">Action to configure settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureSettings is null.</exception>
    public static IServiceCollection AddTS4ToolsSettings(
        this IServiceCollection services,
        Action<ApplicationSettings> configureSettings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureSettings);

        // Configure settings with custom action
        services.Configure(configureSettings);

        // Enable validation
        services.AddOptions<ApplicationSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the settings service
        services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();

        return services;
    }

    /// <summary>
    /// Creates a default configuration builder for TS4Tools applications.
    /// </summary>
    /// <param name="args">Command line arguments (optional).</param>
    /// <returns>A configured configuration builder.</returns>
    public static IConfigurationBuilder CreateDefaultConfigurationBuilder(string[]? args = null)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
                optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("TS4TOOLS_");

        if (args?.Length > 0)
        {
            builder.AddCommandLine(args);
        }

        return builder;
    }

    /// <summary>
    /// Gets the default application settings configuration.
    /// Used for scenarios where dependency injection is not available.
    /// </summary>
    /// <param name="configurationBuilder">Optional configuration builder. If null, creates default.</param>
    /// <returns>The default application settings.</returns>
    public static ApplicationSettings GetDefaultSettings(IConfigurationBuilder? configurationBuilder = null)
    {
        configurationBuilder ??= CreateDefaultConfigurationBuilder();
        var configuration = configurationBuilder.Build();

        var settings = new ApplicationSettings();
        configuration.GetSection(ApplicationSettings.SectionName).Bind(settings);

        return settings;
    }
}
