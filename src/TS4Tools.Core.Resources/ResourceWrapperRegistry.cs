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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Provides automatic discovery and registration of resource wrapper factories.
/// This replaces the legacy s4pi.WrapperDealer reflection-based system with modern dependency injection.
/// </summary>
public sealed class ResourceWrapperRegistry : IResourceWrapperRegistry, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<ResourceWrapperRegistry> _logger;
    private readonly ConcurrentDictionary<string, FactoryRegistration> _registrations = new();
    private readonly ConcurrentDictionary<string, long> _creationMetrics = new();
    private readonly object _metricsLock = new();
    private readonly Timer? _metricsTimer;
    private bool _disposed;

    /// <summary>
    /// Represents a factory registration with metadata.
    /// </summary>
    private sealed record FactoryRegistration(
        Type FactoryType,
        Type ResourceType,
        int Priority,
        IReadOnlySet<string> SupportedResourceTypes,
        DateTime RegisteredAt);

    public ResourceWrapperRegistry(
        IServiceProvider serviceProvider,
        IResourceManager resourceManager,
        ILogger<ResourceWrapperRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Start metrics collection timer
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public async Task<ResourceWrapperRegistryResult> DiscoverAndRegisterFactoriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var stopwatch = Stopwatch.StartNew();
        var result = new ResourceWrapperRegistryResult();

        try
        {
            _logger.LogInformation("Starting resource factory discovery and registration");

            // Discover factories from all loaded assemblies
            var factoryTypes = await DiscoverFactoryTypesAsync(cancellationToken);
            _logger.LogDebug("Discovered {FactoryCount} factory types", factoryTypes.Count);

            // Group by priority for registration order
            var prioritizedFactories = factoryTypes
                .GroupBy(f => f.Priority)
                .OrderByDescending(g => g.Key);

            foreach (var priorityGroup in prioritizedFactories)
            {
                _logger.LogDebug("Registering {FactoryCount} factories with priority {Priority}",
                    priorityGroup.Count(), priorityGroup.Key);

                // Register factories in parallel within the same priority level
                var registrationTasks = priorityGroup.Select(async factory =>
                {
                    try
                    {
                        await RegisterFactoryAsync(factory, cancellationToken);
                        result.AddSuccessfulRegistration(factory.FactoryType.Name);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to register factory {FactoryType}", factory.FactoryType.Name);
                        result.AddFailedRegistration(factory.FactoryType.Name, ex.Message);
                        return false;
                    }
                });

                await Task.WhenAll(registrationTasks);
            }

            stopwatch.Stop();
            result.RegistrationDuration = stopwatch.Elapsed;
            result.TotalFactoriesDiscovered = factoryTypes.Count;

            _logger.LogInformation(
                "Factory registration completed in {Duration:F2}ms. " +
                "Successful: {Successful}, Failed: {Failed}",
                stopwatch.Elapsed.TotalMilliseconds,
                result.SuccessfulRegistrations.Count,
                result.FailedRegistrations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during factory discovery and registration");
            result.AddFailedRegistration("DISCOVERY_ERROR", ex.Message);
            return result;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ResourceWrapperRegistryInfo> GetRegisteredFactories()
    {
        return _registrations.ToDictionary(
            kvp => kvp.Key,
            kvp => new ResourceWrapperRegistryInfo(
                kvp.Value.FactoryType.Name,
                kvp.Value.ResourceType.Name,
                kvp.Value.Priority,
                kvp.Value.SupportedResourceTypes,
                kvp.Value.RegisteredAt,
                _creationMetrics.GetValueOrDefault(kvp.Key, 0)));
    }

    /// <inheritdoc />
    public ResourceWrapperRegistryStatistics GetStatistics()
    {
        lock (_metricsLock)
        {
            var totalCreations = _creationMetrics.Values.Sum();
            var factoryUtilization = _registrations.Count > 0
                ? _creationMetrics.Values.Count(v => v > 0) / (double)_registrations.Count
                : 0.0;

            return new ResourceWrapperRegistryStatistics
            {
                TotalRegisteredFactories = _registrations.Count,
                TotalResourceCreations = totalCreations,
                FactoryUtilizationRatio = factoryUtilization,
                MostUsedFactory = GetMostUsedFactory(),
                RegistrationsByPriority = GetRegistrationsByPriority()
            };
        }
    }

    /// <inheritdoc />
    public bool IsFactoryRegistered(Type factoryType)
    {
        ArgumentNullException.ThrowIfNull(factoryType);

        return _registrations.Values.Any(r => r.FactoryType == factoryType);
    }

    /// <inheritdoc />
    public bool SupportsResourceType(string resourceType)
    {
        ArgumentException.ThrowIfNullOrEmpty(resourceType);

        // First check direct string match (for string-based types like "DDS", "PNG")
        if (_registrations.Values.Any(r => r.SupportedResourceTypes.Contains(resourceType)))
            return true;

        // Try to parse as hex value and check if any factory supports it
        if (TryParseHexResourceType(resourceType, out uint resourceTypeId))
        {
            foreach (var registration in _registrations.Values)
            {
                try
                {
                    // Create factory instance to check if it can handle the resource type
                    var factory = ActivatorUtilities.CreateInstance(_serviceProvider, registration.FactoryType);
                    
                    // Use reflection to call CanCreateResource if available
                    var canCreateMethod = registration.FactoryType.GetMethod("CanCreateResource", new[] { typeof(uint) });
                    if (canCreateMethod != null)
                    {
                        var canCreate = (bool)canCreateMethod.Invoke(factory, new object[] { resourceTypeId })!;
                        if (canCreate)
                            return true;
                    }
                }
                catch
                {
                    // Ignore factory creation errors during support checking
                    continue;
                }
            }
        }

        return false;
    }

    private static bool TryParseHexResourceType(string resourceType, out uint id)
    {
        id = 0;
        if (string.IsNullOrEmpty(resourceType))
            return false;

        // Handle hex format like "0x00B2D882"
        if (resourceType.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(resourceType.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
        }

        // Handle plain hex like "00B2D882" 
        return uint.TryParse(resourceType, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id);
    }

    private async Task<List<FactoryInfo>> DiscoverFactoryTypesAsync(CancellationToken cancellationToken)
    {
        var factoryTypes = new List<FactoryInfo>();

        // Get all loaded assemblies that might contain resource factories
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("TS4Tools.Resources.", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        _logger.LogDebug("Scanning {AssemblyCount} assemblies for resource factories", assemblies.Count);

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Task.Run(() =>
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && IsResourceFactory(t))
                        .ToList();

                    foreach (var type in types)
                    {
                        var factoryInfo = CreateFactoryInfo(type);
                        if (factoryInfo != null)
                        {
                            factoryTypes.Add(factoryInfo);
                        }
                    }
                }, cancellationToken);
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning("Type loading errors in assembly {Assembly}: {Errors}",
                    assembly.GetName().Name,
                    string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message ?? "Unknown error")));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan assembly {Assembly} for factories",
                    assembly.GetName().Name);
            }
        }

        return factoryTypes;
    }

    private static bool IsResourceFactory(Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceFactory<>));
    }

    private FactoryInfo? CreateFactoryInfo(Type factoryType)
    {
        try
        {
            // Find the IResourceFactory<T> interface to determine the resource type
            var factoryInterface = factoryType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceFactory<>));

            if (factoryInterface == null)
                return null;

            var resourceType = factoryInterface.GetGenericArguments()[0];

            // Create a temporary instance to get priority and supported types
            var tempInstance = ActivatorUtilities.CreateInstance(_serviceProvider, factoryType);
            
            if (tempInstance == null)
                return null;

            // Get SupportedResourceTypes and Priority using reflection since we don't have a common non-generic interface
            var supportedTypesProperty = factoryType.GetProperty("SupportedResourceTypes");
            var priorityProperty = factoryType.GetProperty("Priority");
            
            if (supportedTypesProperty == null || priorityProperty == null)
                return null;

            var supportedTypesValue = supportedTypesProperty.GetValue(tempInstance);
            var priorityValue = priorityProperty.GetValue(tempInstance);
            
            if (supportedTypesValue is not IEnumerable<string> supportedTypesEnumerable || priorityValue is not int priority)
                return null;

            var supportedTypes = new HashSet<string>(supportedTypesEnumerable);

            return new FactoryInfo(factoryType, resourceType, priority, supportedTypes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create factory info for type {FactoryType}", factoryType.Name);
            return null;
        }
    }

    private async Task RegisterFactoryAsync(FactoryInfo factoryInfo, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Use reflection to call the generic RegisterFactory method
            var method = typeof(IResourceManager).GetMethod(nameof(IResourceManager.RegisterFactory));
            if (method == null)
            {
                throw new InvalidOperationException("RegisterFactory method not found on IResourceManager");
            }

            var genericMethod = method.MakeGenericMethod(factoryInfo.ResourceType, factoryInfo.FactoryType);
            await Task.Run(() => genericMethod.Invoke(_resourceManager, Array.Empty<object>()), cancellationToken);

            // Record registration
            var registration = new FactoryRegistration(
                factoryInfo.FactoryType,
                factoryInfo.ResourceType,
                factoryInfo.Priority,
                factoryInfo.SupportedResourceTypes,
                DateTime.UtcNow);

            var registrationKey = $"{factoryInfo.ResourceType.Name}:{factoryInfo.FactoryType.Name}";
            _registrations[registrationKey] = registration;

            // Initialize metrics
            _creationMetrics[registrationKey] = 0;

            stopwatch.Stop();
            _logger.LogDebug("Registered factory {FactoryType} for resource {ResourceType} in {Duration:F2}ms",
                factoryInfo.FactoryType.Name,
                factoryInfo.ResourceType.Name,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to register factory {FactoryType} for resource {ResourceType}",
                factoryInfo.FactoryType.Name,
                factoryInfo.ResourceType.Name);
            throw;
        }
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            // Get current resource manager statistics to update usage metrics
            var resourceStats = _resourceManager.GetStatistics();
            
            // This is a simplified metric collection - in a real implementation,
            // you would track individual factory usage through events or callbacks
            lock (_metricsLock)
            {
                // Update metrics based on resource manager statistics
                // For now, we'll distribute the total creations evenly across factories
                if (_registrations.Count > 0)
                {
                    var avgCreationsPerFactory = resourceStats.TotalResourcesCreated / _registrations.Count;
                    foreach (var key in _registrations.Keys)
                    {
                        _creationMetrics[key] = avgCreationsPerFactory;
                    }
                }
            }

            _logger.LogDebug("Metrics collection completed. Tracked {FactoryCount} factories",
                _registrations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during metrics collection");
        }
    }

    private string? GetMostUsedFactory()
    {
        if (_creationMetrics.IsEmpty)
            return null;

        var mostUsed = _creationMetrics.MaxBy(kvp => kvp.Value);
        return mostUsed.Key;
    }

    private IReadOnlyDictionary<int, int> GetRegistrationsByPriority()
    {
        return _registrations.Values
            .GroupBy(r => r.Priority)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _metricsTimer?.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Internal factory information used during discovery.
    /// </summary>
    private sealed record FactoryInfo(
        Type FactoryType,
        Type ResourceType,
        int Priority,
        IReadOnlySet<string> SupportedResourceTypes);
}
