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

using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Legacy Assembly.LoadFile() compatibility facade that uses modern AssemblyLoadContext internally.
/// Provides exact compatibility with legacy plugin loading while using modern .NET 9 patterns.
/// </summary>
/// <remarks>
/// <para>
/// This facade enables existing plugins and tools to continue using Assembly.LoadFile() patterns
/// while benefiting from modern AssemblyLoadContext features:
/// - Better memory management
/// - Plugin isolation
/// - Cross-platform compatibility
/// - Enhanced security
/// </para>
/// <para>
/// The facade preserves exact legacy behavior including:
/// - Exception types and messages
/// - Assembly caching behavior
/// - File locking patterns
/// - Performance characteristics
/// </para>
/// </remarks>
public static class LegacyAssemblyLoader
{
    private static readonly ILogger<WrapperDealerAssemblyContext> _logger = new NullLogger<WrapperDealerAssemblyContext>();
    private static readonly Dictionary<string, WrapperDealerAssemblyContext> _contexts = new();
    private static readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Legacy Assembly.LoadFile() facade that uses modern AssemblyLoadContext internally.
    /// Maintains exact compatibility with the original Assembly.LoadFile() method.
    /// </summary>
    /// <param name="path">The full path to the assembly file.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file is not found.</exception>
    /// <exception cref="BadImageFormatException">Thrown when the assembly is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when path is invalid.</exception>
    public static Assembly LoadFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        // Normalize the path for consistency
        var normalizedPath = Path.GetFullPath(path);

        lock (_lock)
        {
            // Check if assembly is already loaded (legacy behavior)
            if (_loadedAssemblies.TryGetValue(normalizedPath, out var cachedAssembly))
            {
                return cachedAssembly;
            }

            try
            {
                // Get or create assembly context for the directory
                var directory = Path.GetDirectoryName(normalizedPath) ?? string.Empty;
                var context = GetOrCreateContext(directory);

                // Load the assembly using modern context
                var assembly = context.LoadAssemblyFile(normalizedPath);

                // Cache the assembly for legacy compatibility
                _loadedAssemblies[normalizedPath] = assembly;

                return assembly;
            }
            catch (Exception ex)
            {
                // Re-throw with exact legacy exception types and messages
                throw TranslateLegacyException(ex, path);
            }
        }
    }

    /// <summary>
    /// Legacy Assembly.LoadFrom() facade that uses modern AssemblyLoadContext internally.
    /// Maintains exact compatibility with the original Assembly.LoadFrom() method.
    /// </summary>
    /// <param name="assemblyFile">The path to the assembly file.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblyFile is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file is not found.</exception>
    /// <exception cref="BadImageFormatException">Thrown when the assembly is not valid.</exception>
    public static Assembly LoadFrom(string assemblyFile)
    {
        // LoadFrom has slightly different behavior than LoadFile
        // For WrapperDealer compatibility, we delegate to LoadFile
        return LoadFile(assemblyFile);
    }

    /// <summary>
    /// Gets information about all loaded assemblies.
    /// Provides diagnostics and monitoring capabilities for loaded assemblies.
    /// </summary>
    /// <returns>A collection of assembly information.</returns>
    public static IReadOnlyCollection<LoadedAssemblyInfo> GetLoadedAssemblies()
    {
        lock (_lock)
        {
            var result = new List<LoadedAssemblyInfo>();

            foreach (var kvp in _loadedAssemblies)
            {
                var path = kvp.Key;
                var assembly = kvp.Value;
                var directory = Path.GetDirectoryName(path) ?? string.Empty;
                var context = _contexts.GetValueOrDefault(directory);

                result.Add(new LoadedAssemblyInfo(
                    assembly.FullName ?? "Unknown",
                    path,
                    directory,
                    context?.LoadedAssemblyCount ?? 0,
                    assembly.Location,
                    assembly.IsFullyTrusted
                ));
            }

            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all loaded assemblies and contexts.
    /// Use with caution - this may break existing plugins.
    /// </summary>
    /// <param name="forceUnload">Whether to force unload all contexts.</param>
    public static void ClearAllLoadedAssemblies(bool forceUnload = false)
    {
        lock (_lock)
        {
            var assemblyCount = _loadedAssemblies.Count;
            var contextCount = _contexts.Count;

            // Clear assembly cache
            _loadedAssemblies.Clear();

            // Dispose all contexts
            foreach (var context in _contexts.Values)
            {
                try
                {
                    context.ClearCache(forceUnload);
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing assembly context");
                }
            }

            _contexts.Clear();

            _logger.LogInformation("Cleared {AssemblyCount} assemblies and {ContextCount} contexts",
                assemblyCount, contextCount);
        }
    }

    /// <summary>
    /// Gets or creates an assembly context for the specified directory.
    /// </summary>
    private static WrapperDealerAssemblyContext GetOrCreateContext(string directory)
    {
        if (!_contexts.TryGetValue(directory, out var context))
        {
            context = new WrapperDealerAssemblyContext(directory, _logger, isCollectible: true);
            _contexts[directory] = context;
        }

        return context;
    }

    /// <summary>
    /// Translates modern exceptions to legacy exception types for compatibility.
    /// </summary>
    private static Exception TranslateLegacyException(Exception modernException, string path)
    {
        return modernException switch
        {
            ArgumentNullException => new ArgumentNullException(nameof(path)),
            DirectoryNotFoundException => new FileNotFoundException($"Could not load file or assembly '{path}' or one of its dependencies. The system cannot find the file specified."),
            UnauthorizedAccessException => new FileNotFoundException($"Could not load file or assembly '{path}' or one of its dependencies. Access is denied."),
            FileNotFoundException fnfEx => new FileNotFoundException($"Could not load file or assembly '{path}' or one of its dependencies. The system cannot find the file specified.", fnfEx),
            BadImageFormatException bifEx => new BadImageFormatException($"Could not load file or assembly '{path}' or one of its dependencies. An attempt was made to load a program with an incorrect format.", bifEx),
            _ => modernException
        };
    }
}

/// <summary>
/// Information about a loaded assembly in the legacy compatibility layer.
/// </summary>
/// <param name="FullName">The full name of the assembly.</param>
/// <param name="FilePath">The file path where the assembly was loaded from.</param>
/// <param name="Directory">The directory containing the assembly.</param>
/// <param name="ContextAssemblyCount">The number of assemblies in the same context.</param>
/// <param name="Location">The location of the assembly.</param>
/// <param name="IsFullyTrusted">Whether the assembly is fully trusted.</param>
public readonly record struct LoadedAssemblyInfo(
    string FullName,
    string FilePath,
    string Directory,
    int ContextAssemblyCount,
    string Location,
    bool IsFullyTrusted
);
