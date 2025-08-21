namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for Object Definition Resources in The Sims 4.
/// Object Definition Resources (0xC0DB5AE7) contain the core definitions
/// for objects in the game, including their properties, components, and references.
/// </summary>
public interface IObjectDefinitionResource : IResource
{
    /// <summary>
    /// Gets the version of the object definition format.
    /// </summary>
    ushort Version { get; }

    /// <summary>
    /// Gets or sets the name of the object.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the tuning script associated with this object.
    /// </summary>
    string Tuning { get; set; }

    /// <summary>
    /// Gets or sets the tuning ID for this object.
    /// </summary>
    ulong TuningId { get; set; }

    /// <summary>
    /// Gets the list of property IDs associated with this object.
    /// </summary>
    IReadOnlyList<uint> PropertyIds { get; }

    /// <summary>
    /// Gets or sets the material variant for this object.
    /// </summary>
    string MaterialVariant { get; set; }

    /// <summary>
    /// Gets the icon resource reference for this object.
    /// </summary>
    IResourceReference? Icon { get; }

    /// <summary>
    /// Gets the rig resource reference for this object.
    /// </summary>
    IResourceReference? Rig { get; }

    /// <summary>
    /// Gets the slot resource reference for this object.
    /// </summary>
    IResourceReference? Slot { get; }

    /// <summary>
    /// Gets the model resource reference for this object.
    /// </summary>
    IResourceReference? Model { get; }

    /// <summary>
    /// Gets the footprint resource reference for this object.
    /// </summary>
    IResourceReference? Footprint { get; }

    /// <summary>
    /// Gets the list of component IDs for this object.
    /// </summary>
    IReadOnlyList<uint> Components { get; }

    /// <summary>
    /// Adds a property ID to the object definition.
    /// </summary>
    /// <param name="propertyId">The property ID to add</param>
    void AddPropertyId(uint propertyId);

    /// <summary>
    /// Removes a property ID from the object definition.
    /// </summary>
    /// <param name="propertyId">The property ID to remove</param>
    /// <returns>True if the property ID was removed, false if it was not found</returns>
    bool RemovePropertyId(uint propertyId);

    /// <summary>
    /// Adds a component ID to the object definition.
    /// </summary>
    /// <param name="componentId">The component ID to add</param>
    void AddComponent(uint componentId);

    /// <summary>
    /// Removes a component ID from the object definition.
    /// </summary>
    /// <param name="componentId">The component ID to remove</param>
    /// <returns>True if the component ID was removed, false if it was not found</returns>
    bool RemoveComponent(uint componentId);
}
