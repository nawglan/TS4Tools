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

namespace TS4Tools.Core.Resources;

/// <summary>
/// Provides automatic discovery and registration of resource wrapper factories.
/// This replaces the legacy s4pi.WrapperDealer reflection-based system with modern dependency injection.
/// </summary>
public interface IResourceWrapperRegistry
{
    /// <summary>
    /// Discovers and registers all available resource factories from loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the discovery and registration process</returns>
    Task<ResourceWrapperRegistryResult> DiscoverAndRegisterFactoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all registered factories.
    /// </summary>
    /// <returns>Dictionary of factory information keyed by registration key</returns>
    IReadOnlyDictionary<string, ResourceWrapperRegistryInfo> GetRegisteredFactories();

    /// <summary>
    /// Gets statistics about the registry and factory usage.
    /// </summary>
    /// <returns>Registry statistics</returns>
    ResourceWrapperRegistryStatistics GetStatistics();

    /// <summary>
    /// Checks if a specific factory type is registered.
    /// </summary>
    /// <param name="factoryType">Factory type to check</param>
    /// <returns>True if registered, false otherwise</returns>
    bool IsFactoryRegistered(Type factoryType);

    /// <summary>
    /// Checks if the registry has a factory that supports the specified resource type.
    /// </summary>
    /// <param name="resourceType">Resource type to check</param>
    /// <returns>True if supported, false otherwise</returns>
    bool SupportsResourceType(string resourceType);
}

/// <summary>
/// Result of factory discovery and registration process.
/// </summary>
public sealed class ResourceWrapperRegistryResult
{
    private readonly List<string> _successfulRegistrations = new();
    private readonly Dictionary<string, string> _failedRegistrations = new();

    /// <summary>
    /// Gets the names of successfully registered factories.
    /// </summary>
    public IReadOnlyCollection<string> SuccessfulRegistrations => _successfulRegistrations.AsReadOnly();

    /// <summary>
    /// Gets the names and error messages of failed registrations.
    /// </summary>
    public IReadOnlyDictionary<string, string> FailedRegistrations => _failedRegistrations.AsReadOnly();

    /// <summary>
    /// Gets or sets the total number of factories discovered.
    /// </summary>
    public int TotalFactoriesDiscovered { get; set; }

    /// <summary>
    /// Gets or sets the duration of the registration process.
    /// </summary>
    public TimeSpan RegistrationDuration { get; set; }

    /// <summary>
    /// Gets a value indicating whether all discoveries were successful.
    /// </summary>
    public bool IsSuccess => _failedRegistrations.Count == 0;

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalFactoriesDiscovered > 0
        ? (double)_successfulRegistrations.Count / TotalFactoriesDiscovered * 100.0
        : 100.0;

    /// <summary>
    /// Adds a successful registration.
    /// </summary>
    /// <param name="factoryName">Name of the successfully registered factory</param>
    internal void AddSuccessfulRegistration(string factoryName)
    {
        _successfulRegistrations.Add(factoryName);
    }

    /// <summary>
    /// Adds a failed registration.
    /// </summary>
    /// <param name="factoryName">Name of the factory that failed to register</param>
    /// <param name="errorMessage">Error message describing the failure</param>
    internal void AddFailedRegistration(string factoryName, string errorMessage)
    {
        _failedRegistrations[factoryName] = errorMessage;
    }
}

/// <summary>
/// Information about a registered factory.
/// </summary>
/// <param name="FactoryName">Name of the factory type</param>
/// <param name="ResourceName">Name of the resource type</param>
/// <param name="Priority">Factory priority</param>
/// <param name="SupportedResourceTypes">Set of supported resource type identifiers</param>
/// <param name="RegisteredAt">When the factory was registered</param>
/// <param name="UsageCount">Number of times this factory has been used</param>
public sealed record ResourceWrapperRegistryInfo(
    string FactoryName,
    string ResourceName,
    int Priority,
    IReadOnlySet<string> SupportedResourceTypes,
    DateTime RegisteredAt,
    long UsageCount);

/// <summary>
/// Statistics about the resource wrapper registry.
/// </summary>
public sealed class ResourceWrapperRegistryStatistics
{
    /// <summary>
    /// Gets or sets the total number of registered factories.
    /// </summary>
    public int TotalRegisteredFactories { get; set; }

    /// <summary>
    /// Gets or sets the total number of resource creations across all factories.
    /// </summary>
    public long TotalResourceCreations { get; set; }

    /// <summary>
    /// Gets or sets the ratio of factories that have been used at least once.
    /// </summary>
    public double FactoryUtilizationRatio { get; set; }

    /// <summary>
    /// Gets or sets the name of the most frequently used factory.
    /// </summary>
    public string? MostUsedFactory { get; set; }

    /// <summary>
    /// Gets or sets the distribution of registrations by priority level.
    /// </summary>
    public IReadOnlyDictionary<int, int> RegistrationsByPriority { get; set; } = new Dictionary<int, int>();
}
