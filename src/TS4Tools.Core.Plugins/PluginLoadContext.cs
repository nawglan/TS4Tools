using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Plugins;

/// <summary>
/// Modern plugin loading context using AssemblyLoadContext.
/// This replaces the legacy Assembly.LoadFile() approach with modern .NET 9 compatible loading.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext, IPluginLoadContext
{
    private readonly ILogger<PluginLoadContext> _logger;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadContext"/> class.
    /// </summary>
    /// <param name="name">The name of the plugin load context.</param>
    /// <param name="logger">The logger instance for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public PluginLoadContext(string name, ILogger<PluginLoadContext> logger)
        : base(name, isCollectible: true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Load an assembly from the specified path using modern AssemblyLoadContext.
    /// This replaces the legacy Assembly.LoadFile() calls that break in .NET 9.
    /// </summary>
    public Assembly LoadAssembly(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}");

        if (_loadedAssemblies.TryGetValue(assemblyPath, out var existingAssembly))
        {
            _logger.LogDebug("Assembly already loaded: {AssemblyPath}", assemblyPath);
            return existingAssembly;
        }

        try
        {
            var assembly = LoadFromAssemblyPath(assemblyPath);
            _loadedAssemblies[assemblyPath] = assembly;

            _logger.LogInformation("Successfully loaded plugin assembly: {AssemblyPath}", assemblyPath);
            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin assembly: {AssemblyPath}", assemblyPath);
            throw new PluginLoadException($"Cannot load assembly: {assemblyPath}", ex);
        }
    }

    /// <summary>
    /// Unload a previously loaded assembly.
    /// </summary>
    public void UnloadAssembly(string assemblyPath)
    {
        if (_loadedAssemblies.Remove(assemblyPath))
        {
            _logger.LogInformation("Unloaded assembly: {AssemblyPath}", assemblyPath);
        }
    }

    /// <summary>
    /// Discover resource handlers in the specified assembly.
    /// This replaces the legacy pattern from WrapperDealer.cs that used Assembly.LoadFile().
    /// </summary>
    public IEnumerable<Type> DiscoverResourceHandlers(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        try
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => IsResourceHandler(t) && !t.IsAbstract)
                .ToList();

            _logger.LogDebug("Discovered {Count} resource handlers in {Assembly}",
                handlerTypes.Count, assembly.FullName);

            return handlerTypes;
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Some types failed to load from {Assembly}: {Errors}",
                assembly.FullName, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message)));

            // Return successfully loaded types that are resource handlers
            return ex.Types
                .Where(t => t != null && IsResourceHandler(t) && !t.IsAbstract)
                .Cast<Type>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover resource handlers in {Assembly}", assembly.FullName);
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Check if an assembly is currently loaded in this context.
    /// </summary>
    public bool IsAssemblyLoaded(string assemblyPath)
    {
        return _loadedAssemblies.ContainsKey(assemblyPath);
    }

    /// <summary>
    /// Check if a type is a resource handler (legacy AResourceHandler pattern).
    /// This maintains compatibility with the existing Sims4Tools plugin system.
    /// </summary>
    private static bool IsResourceHandler(Type type)
    {
        // Check for legacy AResourceHandler inheritance
        // Note: This assumes AResourceHandler type is available in the type hierarchy
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AResourceHandler")
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Releases the resources used by the plugin load context.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the plugin load context and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // AssemblyLoadContext already handles disposal properly
            // We just delegate to the base class implementation
            base.Unload();
        }
    }
}
