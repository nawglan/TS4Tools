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

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Extension methods for configuring character resource services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds character resource services to the service collection.
    /// This includes CAS Part resources and related factories.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public static IServiceCollection AddCharacterResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the CAS Part resource factory
        services.AddTransient<IResourceFactory, CasPartResourceFactory>();

        return services;
    }

    /// <summary>
    /// Configures character resource services with advanced options.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configure">Configuration action for character resource options</param>
    /// <returns>The service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configure is null</exception>
    public static IServiceCollection AddCharacterResources(
        this IServiceCollection services,
        Action<CharacterResourceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CharacterResourceOptions();
        configure(options);

        services.AddSingleton(options);

        return services.AddCharacterResources();
    }
}

/// <summary>
/// Configuration options for character resource services.
/// </summary>
public sealed class CharacterResourceOptions
{
    /// <summary>
    /// Gets or sets whether to enable detailed logging for character resources.
    /// Default is false.
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets whether to perform strict validation of CAS part data.
    /// Default is true.
    /// </summary>
    public bool EnableStrictValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed CAS part file size in bytes.
    /// Default is 50 MB.
    /// </summary>
    public long MaxCasPartSize { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Gets or sets whether to cache parsed CAS part metadata.
    /// Default is true.
    /// </summary>
    public bool EnableMetadataCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets custom validation rules for CAS parts.
    /// </summary>
    public List<Func<CasPartResource, ValidationResult>> CustomValidationRules { get; set; } = [];
}

/// <summary>
/// Validation result for character resource validation.
/// </summary>
public readonly record struct ValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    string? WarningMessage = null);
