using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for Object Key resources that manage object component definitions and their associated data.
/// Provides efficient access to component data, keys, and TGI block references for object identification.
/// </summary>
public interface IObjKeyResource : IResource
{
    /// <summary>
    /// Gets the version of the ObjKey format.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the unique object key identifier.
    /// </summary>
    ulong ObjectKey { get; set; }

    /// <summary>
    /// Gets the object type identifier.
    /// </summary>
    uint ObjectType { get; set; }

    /// <summary>
    /// Gets additional metadata for the object.
    /// </summary>
    byte[] AdditionalData { get; set; }

    /// <summary>
    /// Validates that the object key resource is in a valid state.
    /// </summary>
    /// <returns>True if the resource is valid; otherwise false.</returns>
    bool IsValid();

    /// <summary>
    /// Generates a new unique object key asynchronously.
    /// </summary>
    /// <param name="objectType">The object type to generate a key for.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task containing the new object key.</returns>
    Task GenerateNewKeyAsync(uint objectType, CancellationToken cancellationToken = default);
}
