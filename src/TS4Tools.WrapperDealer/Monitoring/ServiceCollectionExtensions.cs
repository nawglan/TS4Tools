/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TS4Tools.WrapperDealer.Monitoring;

/// <summary>
/// Extension methods for configuring WrapperDealer monitoring services.
/// Phase 4.20.4 - Dependency injection configuration for performance monitoring.
/// </summary>
public static class MonitoringServiceCollectionExtensions
{
    /// <summary>
    /// Adds WrapperDealer performance monitoring services to the service collection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWrapperDealerMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure telemetry options from configuration
        var telemetrySection = configuration.GetSection("WrapperDealer:Telemetry");
        services.Configure<TelemetryOptions>(telemetrySection);

        // Register core monitoring services
        services.AddSingleton<IWrapperDealerMetrics, WrapperDealerMetrics>();
        services.AddSingleton<ITelemetryService, DefaultTelemetryService>();
        services.AddSingleton<WrapperDealerTelemetryService>();

        return services;
    }

    /// <summary>
    /// Adds WrapperDealer performance monitoring services with custom telemetry service.
    /// </summary>
    /// <typeparam name="TTelemetryService">Custom telemetry service implementation</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWrapperDealerMonitoring<TTelemetryService>(this IServiceCollection services, IConfiguration configuration)
        where TTelemetryService : class, ITelemetryService
    {
        // Configure telemetry options from configuration
        var telemetrySection = configuration.GetSection("WrapperDealer:Telemetry");
        services.Configure<TelemetryOptions>(telemetrySection);

        // Register core monitoring services with custom telemetry
        services.AddSingleton<IWrapperDealerMetrics, WrapperDealerMetrics>();
        services.AddSingleton<ITelemetryService, TTelemetryService>();
        services.AddSingleton<WrapperDealerTelemetryService>();

        return services;
    }

    /// <summary>
    /// Adds WrapperDealer performance monitoring services with manual configuration.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure telemetry options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWrapperDealerMonitoring(this IServiceCollection services, Action<TelemetryOptions> configureOptions)
    {
        // Configure telemetry options
        services.Configure(configureOptions);

        // Register core monitoring services
        services.AddSingleton<IWrapperDealerMetrics, WrapperDealerMetrics>();
        services.AddSingleton<ITelemetryService, DefaultTelemetryService>();
        services.AddSingleton<WrapperDealerTelemetryService>();

        return services;
    }
}

/// <summary>
/// Configuration section for example appsettings.json integration.
/// Phase 4.20.4 - Configuration documentation and examples.
/// </summary>
public static class ConfigurationExample
{
    /// <summary>
    /// Example configuration section for appsettings.json:
    /// 
    /// {
    ///   "WrapperDealer": {
    ///     "Telemetry": {
    ///       "Enabled": true,
    ///       "ReportingInterval": "00:05:00",
    ///       "ImmediateWarningReporting": true,
    ///       "ApplicationInsightsConnectionString": null,
    ///       "OpenTelemetryEndpoint": null,
    ///       "CustomTelemetryEndpoint": null,
    ///       "BatchSize": 100,
    ///       "IncludeResourceTypeNames": false
    ///     }
    ///   }
    /// }
    /// </summary>
    public const string ExampleConfiguration = """
        {
          "WrapperDealer": {
            "Telemetry": {
              "Enabled": true,
              "ReportingInterval": "00:05:00",
              "ImmediateWarningReporting": true,
              "ApplicationInsightsConnectionString": null,
              "OpenTelemetryEndpoint": null,
              "CustomTelemetryEndpoint": null,
              "BatchSize": 100,
              "IncludeResourceTypeNames": false
            }
          }
        }
        """;
}
