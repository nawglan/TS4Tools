using System.Reflection;

namespace TS4Tools.Core.Plugins;

/// <summary>
/// Interface for modern plugin loading using AssemblyLoadContext.
/// This replaces the legacy Assembly.LoadFile() approach with modern .NET 9 compatible loading.
/// </summary>
public interface IPluginLoadContext : IDisposable
{
    /// <summary>
    /// Load an assembly from the specified path using modern AssemblyLoadContext.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file</param>
    /// <returns>Loaded assembly</returns>
    Assembly LoadAssembly(string assemblyPath);

    /// <summary>
    /// Unload a previously loaded assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly to unload</param>
    void UnloadAssembly(string assemblyPath);

    /// <summary>
    /// Discover resource handlers in the specified assembly.
    /// This replaces the legacy Assembly.LoadFile() calls that break in .NET 9.
    /// </summary>
    /// <param name="assembly">Assembly to scan for resource handlers</param>
    /// <returns>Collection of resource handler types</returns>
    IEnumerable<Type> DiscoverResourceHandlers(Assembly assembly);

    /// <summary>
    /// Check if an assembly is currently loaded in this context.
    /// </summary>
    /// <param name="assemblyPath">Path to check</param>
    /// <returns>True if loaded, false otherwise</returns>
    bool IsAssemblyLoaded(string assemblyPath);
}
