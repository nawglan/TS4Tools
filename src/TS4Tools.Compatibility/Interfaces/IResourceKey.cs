// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IResourceKey.cs lines 23-43

namespace s4pi.Interfaces;

/// <summary>
/// An interface for accessing a resource index key.
/// </summary>
public interface IResourceKey : IEqualityComparer<IResourceKey>, IEquatable<IResourceKey>, IComparable<IResourceKey>
{
    /// <summary>
    /// The "type" of the resource.
    /// </summary>
    uint ResourceType { get; set; }

    /// <summary>
    /// The "group" the resource is part of.
    /// </summary>
    uint ResourceGroup { get; set; }

    /// <summary>
    /// The "instance" number of the resource.
    /// </summary>
    ulong Instance { get; set; }
}
