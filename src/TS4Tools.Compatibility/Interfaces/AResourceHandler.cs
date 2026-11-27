// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs lines 23-39

namespace s4pi.Interfaces;

/// <summary>
/// A resource handler: a map from wrapper <see cref="Type"/> (an <see cref="IResource"/> with an
/// <c>(int APIversion, Stream s)</c> constructor) to the list of resource type strings
/// (<c>"0xTTTTTTTT"</c>, or <c>"*"</c> for the default) it handles.
/// </summary>
/// <remarks>
/// <see cref="s4pi.WrapperDealer.WrapperDealer"/> discovers every concrete subclass with a public parameterless
/// constructor in the loaded assemblies (including assemblies loaded later, e.g. plugins loaded by
/// <c>ResourceHandlerRegistry.LoadWrappersFromFolder</c>).
/// </remarks>
public abstract class AResourceHandler : Dictionary<Type, List<string>>, IResourceHandler
{
    /// <summary>
    /// Create the content of the Dictionary (in the derived constructor).
    /// </summary>
    protected AResourceHandler() { }
}
