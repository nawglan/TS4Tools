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

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Modern AssemblyLoadContext implementation for WrapperDealer plugin loading.
/// Replaces legacy Assembly.LoadFile() patterns while maintaining exact compatibility.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the modern .NET 9 AssemblyLoadContext pattern to provide:
/// - Plugin isolation and cleanup
/// - Cross-platform assembly resolution
/// - Legacy Assembly.LoadFile() compatibility
/// - Proper dependency management
/// </para>
/// <para>
/// The implementation preserves exact legacy behavior while providing modern benefits:
/// - Better memory management
/// - Plugin unloading support
/// - Enhanced security
/// - Cross-platform compatibility
/// </para>
/// </remarks>
public sealed class WrapperDealerAssemblyContext : AssemblyLoadContext, IDisposable
{
    private readonly ILogger<WrapperDealerAssemblyContext> _logger;
    private readonly string _pluginDirectory;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the WrapperDealerAssemblyContext class.
    /// </summary>
    /// <param name="pluginDirectory">The directory to search for plugin assemblies.</param>
    /// <param name="logger">The logger for assembly loading operations.</param>
    /// <param name="isCollectible">Whether the context supports unloading (default: true).</param>
    public WrapperDealerAssemblyContext(
        string pluginDirectory,
        ILogger<WrapperDealerAssemblyContext> logger,
        bool isCollectible = true)
        : base($"WrapperDealer-{Guid.NewGuid():N}", isCollectible)
    {
        _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set up assembly resolution handlers
        Resolving += OnResolving;
        ResolvingUnmanagedDll += OnResolvingUnmanagedDll;

        _logger.LogDebug("Created WrapperDealerAssemblyContext for directory: {PluginDirectory}", _pluginDirectory);
    }

    /// <summary>
    /// Loads an assembly from the specified file path with legacy compatibility.
    /// This method provides Assembly.LoadFile() compatibility while using modern AssemblyLoadContext.
    /// </summary>
    /// <param name="assemblyPath">The full path to the assembly file.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblyPath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file is not found.</exception>
    /// <exception cref="BadImageFormatException">Thrown when the assembly is not valid.</exception>
    public Assembly LoadAssemblyFile(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            throw new ArgumentNullException(nameof(assemblyPath));

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}", assemblyPath);

        lock (_lock)
        {
            // Check if already loaded
            var assemblyKey = Path.GetFullPath(assemblyPath);
            if (_loadedAssemblies.TryGetValue(assemblyKey, out var cachedAssembly))
            {
                _logger.LogDebug("Returning cached assembly: {AssemblyPath}", assemblyPath);
                return cachedAssembly;
            }

            try
            {
                _logger.LogInformation("Loading assembly: {AssemblyPath}", assemblyPath);

                // Load the assembly using modern AssemblyLoadContext
                var assembly = LoadFromAssemblyPath(assemblyPath);

                // Cache the loaded assembly
                _loadedAssemblies[assemblyKey] = assembly;

                _logger.LogInformation("Successfully loaded assembly: {AssemblyName} from {AssemblyPath}",
                    assembly.FullName, assemblyPath);

                return assembly;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly: {AssemblyPath}", assemblyPath);
                throw;
            }
        }
    }

    /// <summary>
    /// Gets all assemblies loaded in this context.
    /// </summary>
    /// <returns>A read-only collection of loaded assemblies.</returns>
    public IReadOnlyCollection<Assembly> GetLoadedAssemblies()
    {
        lock (_lock)
        {
            return _loadedAssemblies.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the number of assemblies loaded in this context.
    /// </summary>
    public int LoadedAssemblyCount
    {
        get
        {
            lock (_lock)
            {
                return _loadedAssemblies.Count;
            }
        }
    }

    /// <summary>
    /// Clears the assembly cache and optionally attempts to unload the context.
    /// </summary>
    /// <param name="forceUnload">Whether to attempt to unload the context immediately.</param>
    public void ClearCache(bool forceUnload = false)
    {
        lock (_lock)
        {
            var assemblyCount = _loadedAssemblies.Count;
            _loadedAssemblies.Clear();

            _logger.LogInformation("Cleared assembly cache with {AssemblyCount} assemblies", assemblyCount);

            if (forceUnload && IsCollectible)
            {
                try
                {
                    Unload();
                    _logger.LogInformation("Unloaded WrapperDealerAssemblyContext");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to unload WrapperDealerAssemblyContext");
                }
            }
        }
    }

    /// <summary>
    /// Handles assembly resolution for dependencies.
    /// </summary>
    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        _logger.LogDebug("Resolving assembly: {AssemblyName}", assemblyName.FullName);

        try
        {
            // Try to find the assembly in the plugin directory
            var assemblyFileName = $"{assemblyName.Name}.dll";
            var assemblyPath = Path.Combine(_pluginDirectory, assemblyFileName);

            if (File.Exists(assemblyPath))
            {
                _logger.LogDebug("Found dependency assembly: {AssemblyPath}", assemblyPath);
                return LoadAssemblyFile(assemblyPath);
            }

            // Try common alternative locations
            var alternativeLocations = new[]
            {
                Path.Combine(_pluginDirectory, "Dependencies", assemblyFileName),
                Path.Combine(_pluginDirectory, "Lib", assemblyFileName),
                Path.Combine(_pluginDirectory, "References", assemblyFileName)
            };

            foreach (var location in alternativeLocations)
            {
                if (File.Exists(location))
                {
                    _logger.LogDebug("Found dependency assembly in alternative location: {AssemblyPath}", location);
                    return LoadAssemblyFile(location);
                }
            }

            // Try to load from the default context if not found
            try
            {
                var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
                _logger.LogDebug("Loaded assembly from default context: {AssemblyName}", assemblyName.FullName);
                return defaultAssembly;
            }
            catch
            {
                // Ignore - assembly not available in default context
            }

            _logger.LogWarning("Could not resolve assembly: {AssemblyName}", assemblyName.FullName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving assembly: {AssemblyName}", assemblyName.FullName);
            return null;
        }
    }

    /// <summary>
    /// Handles unmanaged DLL resolution for native dependencies.
    /// </summary>
    private IntPtr OnResolvingUnmanagedDll(Assembly assembly, string unmanagedDllName)
    {
        _logger.LogDebug("Resolving unmanaged DLL: {DllName} for assembly: {AssemblyName}",
            unmanagedDllName, assembly.FullName);

        try
        {
            // Try to find the unmanaged DLL in the plugin directory
            var dllPath = Path.Combine(_pluginDirectory, unmanagedDllName);
            if (File.Exists(dllPath))
            {
                _logger.LogDebug("Found unmanaged DLL: {DllPath}", dllPath);
                return NativeLibrary.Load(dllPath);
            }

            // Try platform-specific names
            var platformSpecificNames = GetPlatformSpecificDllNames(unmanagedDllName);
            foreach (var name in platformSpecificNames)
            {
                var platformDllPath = Path.Combine(_pluginDirectory, name);
                if (File.Exists(platformDllPath))
                {
                    _logger.LogDebug("Found platform-specific unmanaged DLL: {DllPath}", platformDllPath);
                    return NativeLibrary.Load(platformDllPath);
                }
            }

            _logger.LogWarning("Could not resolve unmanaged DLL: {DllName}", unmanagedDllName);
            return IntPtr.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving unmanaged DLL: {DllName}", unmanagedDllName);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Gets platform-specific DLL names for cross-platform compatibility.
    /// </summary>
    private static string[] GetPlatformSpecificDllNames(string dllName)
    {
        var baseName = Path.GetFileNameWithoutExtension(dllName);

        if (OperatingSystem.IsWindows())
        {
            return new[] { $"{baseName}.dll", $"lib{baseName}.dll" };
        }
        else if (OperatingSystem.IsLinux())
        {
            return new[] { $"lib{baseName}.so", $"{baseName}.so", $"lib{baseName}.so.1" };
        }
        else if (OperatingSystem.IsMacOS())
        {
            return new[] { $"lib{baseName}.dylib", $"{baseName}.dylib" };
        }
        else
        {
            return new[] { dllName };
        }
    }

    /// <summary>
    /// Disposes the assembly context and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _logger.LogDebug("Disposing WrapperDealerAssemblyContext");

        // Clear cache and unload if collectible
        ClearCache(forceUnload: true);

        // Unload the context if it's collectible
        if (IsCollectible)
        {
            try
            {
                Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unloading assembly context");
            }
        }
    }
}
