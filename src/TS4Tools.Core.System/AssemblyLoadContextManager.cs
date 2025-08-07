using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.System;

/// <summary>
/// Interface for managing AssemblyLoadContext instances for dynamic assembly loading.
/// This replaces the legacy Assembly.LoadFile() approach with modern .NET 9 compatible loading.
/// </summary>
public interface IAssemblyLoadContextManager : IDisposable
{
    /// <summary>
    /// Loads an assembly from the specified file path using an isolated AssemblyLoadContext.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the assembly file</param>
    /// <returns>The loaded assembly instance</returns>
    Assembly LoadFromPath(string assemblyPath);

    /// <summary>
    /// Loads an assembly from a stream using an isolated AssemblyLoadContext.
    /// </summary>
    /// <param name="assemblyStream">The stream containing the assembly data</param>
    /// <param name="contextName">Optional name for the context (defaults to generated name)</param>
    /// <returns>The loaded assembly instance</returns>
    Assembly LoadFromStream(Stream assemblyStream, string? contextName = null);

    /// <summary>
    /// Unloads a specific AssemblyLoadContext by name, allowing garbage collection.
    /// </summary>
    /// <param name="contextName">The name of the context to unload</param>
    /// <returns>True if the context was found and marked for unloading</returns>
    bool UnloadContext(string contextName);

    /// <summary>
    /// Gets the names of all currently loaded contexts.
    /// </summary>
    /// <returns>Collection of context names</returns>
    IEnumerable<string> GetLoadedContexts();

    /// <summary>
    /// Gets statistics about loaded assemblies and contexts.
    /// </summary>
    /// <returns>Dictionary with loading statistics</returns>
    Dictionary<string, object> GetLoadingStatistics();
}

/// <summary>
/// Modern implementation of assembly loading using AssemblyLoadContext.
/// This replaces the legacy Assembly.LoadFile() calls that break in .NET 9.
///
/// Key features:
/// - Isolated loading contexts prevent assembly conflicts
/// - Collectible contexts allow proper memory cleanup
/// - Thread-safe concurrent loading
/// - Comprehensive error handling and logging
/// </summary>
public sealed class AssemblyLoadContextManager : IAssemblyLoadContextManager
{
    private readonly ILogger<AssemblyLoadContextManager> _logger;
    private readonly ConcurrentDictionary<string, WeakReference<AssemblyLoadContext>> _contexts;
    private readonly object _lockObject = new();
    private volatile bool _disposed;

    public AssemblyLoadContextManager(ILogger<AssemblyLoadContextManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contexts = new ConcurrentDictionary<string, WeakReference<AssemblyLoadContext>>();

        _logger.LogInformation("AssemblyLoadContextManager initialized");
    }

    /// <inheritdoc />
    public Assembly LoadFromPath(string assemblyPath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssemblyLoadContextManager));

        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}");

        var absolutePath = Path.GetFullPath(assemblyPath);
        var contextName = GenerateContextName(absolutePath);

        _logger.LogDebug("Loading assembly from path: {AssemblyPath} (Context: {ContextName})",
            absolutePath, contextName);

        try
        {
            lock (_lockObject)
            {
                // Check if we already have this context
                if (_contexts.TryGetValue(contextName, out var weakRef) &&
                    weakRef.TryGetTarget(out var existingContext))
                {
                    _logger.LogDebug("Reusing existing context: {ContextName}", contextName);
                    return existingContext.Assemblies.First();
                }

                // Create new collectible context
                var context = new AssemblyLoadContext(contextName, isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(absolutePath);

                // Store weak reference to allow GC
                _contexts[contextName] = new WeakReference<AssemblyLoadContext>(context);

                _logger.LogInformation("Successfully loaded assembly: {AssemblyName} from {AssemblyPath}",
                    assembly.FullName, absolutePath);

                return assembly;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly from path: {AssemblyPath}", absolutePath);
            throw new InvalidOperationException($"Failed to load assembly from {absolutePath}", ex);
        }
    }

    /// <inheritdoc />
    public Assembly LoadFromStream(Stream assemblyStream, string? contextName = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssemblyLoadContextManager));

        if (assemblyStream is null)
            throw new ArgumentNullException(nameof(assemblyStream));

        if (!assemblyStream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(assemblyStream));

        contextName ??= $"StreamContext_{Guid.NewGuid():N}";

        _logger.LogDebug("Loading assembly from stream (Context: {ContextName})", contextName);

        try
        {
            lock (_lockObject)
            {
                // Create new collectible context
                var context = new AssemblyLoadContext(contextName, isCollectible: true);
                var assembly = context.LoadFromStream(assemblyStream);

                // Store weak reference
                _contexts[contextName] = new WeakReference<AssemblyLoadContext>(context);

                _logger.LogInformation("Successfully loaded assembly from stream: {AssemblyName} (Context: {ContextName})",
                    assembly.FullName, contextName);

                return assembly;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly from stream (Context: {ContextName})", contextName);
            throw new InvalidOperationException($"Failed to load assembly from stream", ex);
        }
    }

    /// <inheritdoc />
    public bool UnloadContext(string contextName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssemblyLoadContextManager));

        if (string.IsNullOrWhiteSpace(contextName))
            return false;

        lock (_lockObject)
        {
            if (!_contexts.TryRemove(contextName, out var weakRef))
                return false;

            if (weakRef.TryGetTarget(out var context))
            {
                try
                {
                    context.Unload();
                    _logger.LogInformation("Successfully unloaded context: {ContextName}", contextName);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unloading context: {ContextName}", contextName);
                    return false;
                }
            }

            return true; // Context was already collected
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetLoadedContexts()
    {
        if (_disposed)
            return Enumerable.Empty<string>();

        lock (_lockObject)
        {
            return _contexts.Keys.ToList(); // Return copy to avoid concurrent modification
        }
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetLoadingStatistics()
    {
        if (_disposed)
            return new Dictionary<string, object>();

        lock (_lockObject)
        {
            var activeContexts = 0;
            var totalContexts = _contexts.Count;

            foreach (var weakRef in _contexts.Values)
            {
                if (weakRef.TryGetTarget(out _))
                    activeContexts++;
            }

            return new Dictionary<string, object>
            {
                ["TotalContextsCreated"] = totalContexts,
                ["ActiveContexts"] = activeContexts,
                ["CollectedContexts"] = totalContexts - activeContexts,
                ["LastOperationTime"] = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generates a unique context name based on the assembly path.
    /// </summary>
    private static string GenerateContextName(string assemblyPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
        var hash = Path.GetFullPath(assemblyPath).GetHashCode();
        return $"{fileName}_{hash:X8}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lockObject)
        {
            if (_disposed)
                return;

            _logger.LogInformation("Disposing AssemblyLoadContextManager with {ContextCount} contexts",
                _contexts.Count);

            // Unload all contexts
            foreach (var kvp in _contexts)
            {
                if (kvp.Value.TryGetTarget(out var context))
                {
                    try
                    {
                        context.Unload();
                        _logger.LogDebug("Unloaded context during disposal: {ContextName}", kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error unloading context during disposal: {ContextName}", kvp.Key);
                    }
                }
            }

            _contexts.Clear();
            _disposed = true;

            _logger.LogInformation("AssemblyLoadContextManager disposed");
        }
    }
}
