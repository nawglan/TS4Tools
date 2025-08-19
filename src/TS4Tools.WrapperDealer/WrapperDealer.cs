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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using TS4Tools.Core.Plugins;
using TS4Tools.WrapperDealer.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace TS4Tools.WrapperDealer;

/// <summary>
/// WrapperDealer Compatibility Layer - provides 100% backward compatibility with legacy s4pi WrapperDealer API.
/// 
/// BUSINESS LOGIC APPROACH: This implementation extracts the business requirements from the legacy
/// WrapperDealer and implements them using modern .NET 9 patterns (AssemblyLoadContext, etc.)
/// while maintaining exact API compatibility for community plugins and tools.
/// </summary>
public static class WrapperDealer
{
    #region Private Fields - Modern Implementation

    private static readonly object _lockObject = new object();
    private static readonly ConcurrentDictionary<string, Type> _typeMap = new();
    private static readonly List<KeyValuePair<string, Type>> _disabled = new();
    private static volatile bool _initialized = false;

    // Modern dependency injection bridge
    private static IServiceProvider? _serviceProvider;
    private static IResourceManager? _resourceManager;
    private static PluginRegistrationManager? _pluginManager;

    #endregion

    #region Legacy API Properties - EXACT COMPATIBILITY REQUIRED

    /// <summary>
    /// Retrieve the resource wrappers known to WrapperDealer.
    /// LEGACY API: Returns ICollection&lt;KeyValuePair&lt;string, Type&gt;&gt; for exact compatibility.
    /// </summary>
    public static ICollection<KeyValuePair<string, Type>> TypeMap
    {
        get
        {
            EnsureInitialized();
            lock (_lockObject)
            {
                return new List<KeyValuePair<string, Type>>(_typeMap);
            }
        }
    }

    /// <summary>
    /// Access the collection of wrappers on the "disabled" list.
    /// LEGACY API: Updates to entries in the collection will be used next time a wrapper is requested.
    /// </summary>
    /// <remarks>
    /// Existing instances of a disabled wrapper will not be invalidated and it will remain possible to
    /// bypass WrapperDealer and instantiate instances of the wrapper class directly.
    /// </remarks>
    public static ICollection<KeyValuePair<string, Type>> Disabled
    {
        get
        {
            lock (_lockObject)
            {
                return _disabled;
            }
        }
    }

    #endregion

    #region Legacy API Methods - EXACT SIGNATURES REQUIRED

    /// <summary>
    /// Create a new Resource of the requested type, allowing the wrapper to initialise it appropriately.
    /// LEGACY API: Exact signature compatibility with original WrapperDealer.
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="resourceType">Type of resource (currently a string like "0xDEADBEEF")</param>
    /// <returns>A new resource instance</returns>
    public static IResource CreateNewResource(int APIversion, string resourceType)
    {
        return WrapperForType(resourceType, APIversion, null);
    }

    /// <summary>
    /// Create a new Resource of the requested type, allowing the wrapper to initialise it appropriately.
    /// LEGACY API: Overload for uint resource type.
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="resourceType">Type of resource as uint</param>
    /// <returns>A new resource instance</returns>
    public static IResource CreateNewResource(int APIversion, uint resourceType)
    {
        return CreateNewResource(APIversion, $"0x{resourceType:X8}");
    }

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper.
    /// LEGACY API: Exact signature compatibility with original WrapperDealer.
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="pkg">Package containing <paramref name="rie"/></param>
    /// <param name="rie">Identifies resource to be returned</param>
    /// <returns>A resource from the package</returns>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
    {
        return GetResource(APIversion, pkg, rie, false);
    }

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper or the default wrapper.
    /// LEGACY API: Exact signature compatibility with original WrapperDealer.
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="pkg">Package containing <paramref name="rie"/></param>
    /// <param name="rie">Identifies resource to be returned</param>
    /// <param name="AlwaysDefault">When true, indicates WrapperDealer should always use the DefaultResource wrapper</param>
    /// <returns>A resource from the package</returns>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie, bool AlwaysDefault)
    {
        if (_resourceManager == null)
        {
            throw new InvalidOperationException("WrapperDealer not initialized. Call WrapperDealer.Initialize(serviceProvider) first.");
        }

        // BUSINESS LOGIC: Use modern async resource manager for loading from packages
        try
        {
            var task = _resourceManager.LoadResourceAsync(pkg, rie, APIversion, AlwaysDefault);
            task.Wait(); // LEGACY COMPATIBILITY: Block on async call
            return task.Result;
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    #endregion

    #region Legacy Helper Methods - CRITICAL - OFTEN MISSED

    /// <summary>
    /// Refresh the wrapper registrations from available assemblies.
    /// LEGACY API: Forces re-discovery of wrapper types.
    /// </summary>
    public static void RefreshWrappers()
    {
        lock (_lockObject)
        {
            _initialized = false;
            // Only re-initialize if service provider is available
            if (_resourceManager != null)
            {
                EnsureInitialized();
            }
        }
    }

    /// <summary>
    /// Check if a resource type is supported by any registered wrapper.
    /// LEGACY API: Returns true if a wrapper exists for the resource type.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsResourceSupported(string resourceType)
    {
        // Try to initialize, but don't fail if not available
        try { EnsureInitialized(); } catch { }

        lock (_lockObject)
        {
            return _typeMap.ContainsKey(resourceType) || _typeMap.ContainsKey("*");
        }
    }

    /// <summary>
    /// Get the wrapper type for a specific resource type.
    /// LEGACY API: Returns the Type that handles the specified resource type.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <returns>Wrapper type, or null if not found</returns>
    public static Type? GetWrapperType(string resourceType)
    {
        // Try to initialize, but don't fail if not available
        try { EnsureInitialized(); } catch { }

        lock (_lockObject)
        {
            return _typeMap.TryGetValue(resourceType, out Type? type) ? type : _typeMap.GetValueOrDefault("*");
        }
    }

    /// <summary>
    /// Get all supported resource types.
    /// LEGACY API: Returns array of all resource type strings.
    /// </summary>
    /// <returns>Array of supported resource type strings</returns>
    public static string[] GetSupportedResourceTypes()
    {
        // Try to initialize, but don't fail if not available
        try { EnsureInitialized(); } catch { }

        lock (_lockObject)
        {
            return _typeMap.Keys.ToArray();
        }
    }

    /// <summary>
    /// Check if a wrapper is available (not disabled) for a resource type.
    /// LEGACY API: Returns true if wrapper exists and is not disabled.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <returns>True if available, false otherwise</returns>
    public static bool IsWrapperAvailable(string resourceType)
    {
        EnsureInitialized();
        lock (_lockObject)
        {
            if (!_typeMap.TryGetValue(resourceType, out Type? type))
            {
                type = _typeMap.GetValueOrDefault("*");
                resourceType = "*";
            }

            if (type == null) return false;

            var kvp = new KeyValuePair<string, Type>(resourceType, type);
            return !_disabled.Contains(kvp);
        }
    }

    /// <summary>
    /// Register a wrapper type for specific resource types.
    /// LEGACY API: Allows runtime registration of wrapper types.
    /// </summary>
    /// <param name="wrapperType">Type of wrapper to register</param>
    /// <param name="resourceTypes">Resource types this wrapper handles</param>
    public static void RegisterWrapper(Type wrapperType, params string[] resourceTypes)
    {
        if (wrapperType == null) throw new ArgumentNullException(nameof(wrapperType));
        if (resourceTypes == null) throw new ArgumentNullException(nameof(resourceTypes));

        lock (_lockObject)
        {
            foreach (string resourceType in resourceTypes)
            {
                _typeMap.AddOrUpdate(resourceType, wrapperType, (key, oldValue) => wrapperType);
            }
        }
    }

    /// <summary>
    /// Unregister a wrapper for a specific resource type.
    /// LEGACY API: Removes wrapper registration.
    /// </summary>
    /// <param name="resourceType">Resource type to unregister</param>
    public static void UnregisterWrapper(string resourceType)
    {
        if (resourceType == null) throw new ArgumentNullException(nameof(resourceType));

        lock (_lockObject)
        {
            _typeMap.TryRemove(resourceType, out _);
        }
    }

    /// <summary>
    /// Reload all wrapper registrations.
    /// LEGACY API: Alias for RefreshWrappers().
    /// </summary>
    public static void ReloadWrappers()
    {
        RefreshWrappers();
    }

    #endregion

    #region Modern Initialization

    /// <summary>
    /// Initializes the WrapperDealer with modern dependency injection.
    /// MODERN APPROACH: Bridge legacy static API with modern IoC container.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
        _pluginManager = serviceProvider.GetService<PluginRegistrationManager>();
        _initialized = false; // Force re-initialization with new services
        EnsureInitialized();
    }

    #endregion

    #region Modern Implementation - Business Logic Translation

    /// <summary>
    /// Ensures the WrapperDealer is initialized with modern resource factory discovery.
    /// BUSINESS LOGIC: Replaces legacy Assembly.LoadFile() with modern AssemblyLoadContext patterns.
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lockObject)
        {
            if (_initialized) return;

            if (_resourceManager == null)
            {
                throw new InvalidOperationException(
                    "WrapperDealer not initialized. Call WrapperDealer.Initialize(serviceProvider) first.");
            }

            // BUSINESS LOGIC: Populate type map from modern resource manager
            var typeMap = _resourceManager.GetResourceTypeMap();
            _typeMap.Clear();

            foreach (var kvp in typeMap)
            {
                _typeMap.TryAdd(kvp.Key, kvp.Value);
            }

            // PHASE 4.20.3: Initialize plugin system for legacy compatibility
            if (_pluginManager != null)
            {
                InitializePluginSystem();
            }

            _initialized = true;
        }
    }

    /// <summary>
    /// Initializes the plugin system for legacy compatibility.
    /// PHASE 4.20.4: Enhanced with auto-discovery capabilities.
    /// </summary>
    private static void InitializePluginSystem()
    {
        if (_pluginManager == null) return;

        // BUSINESS LOGIC: Scan for legacy plugins and register them
        // This enables community plugins that use the legacy AResourceHandler.Add() pattern
        try
        {
            // Initialize the AResourceHandler bridge for legacy plugins
            AResourceHandlerBridge.Initialize(_pluginManager);
            
            // PHASE 4.20.4: Auto-discovery of plugins from standard locations
            if (_serviceProvider != null)
            {
                var logger = _serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<PluginDiscoveryService>>();
                if (logger != null)
                {
                    using var discoveryService = new PluginDiscoveryService(logger, _pluginManager);
                    var discoveredCount = discoveryService.DiscoverPlugins();
                    
                    if (discoveredCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Auto-discovered {discoveredCount} plugins from standard locations");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Plugin auto-discovery skipped: Logger not available");
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - plugin system is optional for basic functionality
            System.Diagnostics.Debug.WriteLine($"Plugin system initialization warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the appropriate wrapper for the specified resource type.
    /// BUSINESS LOGIC: Extracted from legacy WrapperForType method.
    /// </summary>
    /// <param name="type">Resource type string</param>
    /// <param name="APIversion">API version</param>
    /// <param name="stream">Resource data stream</param>
    /// <returns>Wrapper instance</returns>
    private static IResource WrapperForType(string type, int APIversion, Stream? stream)
    {
        EnsureInitialized();

        if (_resourceManager == null)
        {
            throw new InvalidOperationException("WrapperDealer not initialized.");
        }

        // BUSINESS LOGIC: Use modern async resource manager, but provide sync API for compatibility
        // Note: This is a blocking call for legacy compatibility - not ideal but necessary
        try
        {
            var task = _resourceManager.CreateResourceAsync(type, APIversion);
            task.Wait(); // LEGACY COMPATIBILITY: Block on async call
            return task.Result;
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
        catch (Exception)
        {
            // BUSINESS LOGIC: Fall back to default wrapper (type "*")
            if (type != "*")
            {
                try
                {
                    var task = _resourceManager.CreateResourceAsync("*", APIversion);
                    task.Wait(); // LEGACY COMPATIBILITY: Block on async call
                    return task.Result;
                }
                catch (AggregateException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            // BUSINESS LOGIC: Throw exception if no wrapper found (legacy behavior)
            throw new InvalidOperationException("Could not find a resource handler");
        }
    }

    /// <summary>
    /// Gets resource type string from resource index entry.
    /// BUSINESS LOGIC: Extract resource type for wrapper lookup.
    /// </summary>
    /// <param name="rie">Resource index entry</param>
    /// <returns>Resource type string</returns>
    private static string GetResourceTypeString(IResourceIndexEntry rie)
    {
        // BUSINESS LOGIC: Convert uint resource type to hex string format expected by legacy API
        return $"0x{rie.ResourceType:X8}";
    }

    /// <summary>
    /// Gets resource stream from package.
    /// BUSINESS LOGIC: Extract resource data stream for wrapper initialization.
    /// </summary>
    /// <param name="pkg">Package</param>
    /// <param name="rie">Resource index entry</param>
    /// <returns>Resource data stream</returns>
    private static Stream? GetResourceStream(IPackage pkg, IResourceIndexEntry rie)
    {
        // BUSINESS LOGIC: Use modern async package API, but provide sync interface for legacy compatibility
        try
        {
            var task = pkg.GetResourceStreamAsync(rie);
            task.Wait(); // LEGACY COMPATIBILITY: Block on async call
            return task.Result;
        }
        catch (AggregateException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    #endregion
}
