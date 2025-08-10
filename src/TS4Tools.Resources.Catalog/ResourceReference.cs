using System.ComponentModel;

using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Represents a reference to a resource (TGI - Type, Group, Instance) used in catalog objects.
/// Resource references typically point to meshes, textures, or other assets needed for object rendering.
/// </summary>
public readonly record struct ResourceReference : INotifyPropertyChanged
{
    #region Properties

    /// <summary>
    /// Gets the resource type identifier (what kind of resource this is).
    /// </summary>
    [ElementPriority(0)]
    public uint ResourceType { get; init; }

    /// <summary>
    /// Gets the resource group identifier (organizational grouping).
    /// </summary>
    [ElementPriority(1)]
    public uint ResourceGroup { get; init; }

    /// <summary>
    /// Gets the resource instance identifier (unique identifier within the type/group).
    /// </summary>
    [ElementPriority(2)]
    public ulong Instance { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceReference"/> struct.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <param name="resourceGroup">The resource group identifier.</param>
    /// <param name="instance">The resource instance identifier.</param>
    public ResourceReference(uint resourceType, uint resourceGroup, ulong instance)
    {
        ResourceType = resourceType;
        ResourceGroup = resourceGroup;
        Instance = instance;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a string representation of the resource reference in TGI format.
    /// </summary>
    /// <returns>A formatted TGI string (Type-Group-Instance).</returns>
    public override string ToString()
    {
        return $"TGI({ResourceType:X8}-{ResourceGroup:X8}-{Instance:X16})";
    }

    /// <summary>
    /// Deconstructs the resource reference into its TGI components.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <param name="resourceGroup">The resource group identifier.</param>
    /// <param name="instance">The resource instance identifier.</param>
    public void Deconstruct(out uint resourceType, out uint resourceGroup, out ulong instance)
    {
        resourceType = ResourceType;
        resourceGroup = ResourceGroup;
        instance = Instance;
    }

    /// <summary>
    /// Creates a resource reference from a TGI tuple.
    /// </summary>
    /// <param name="tgi">A tuple containing (Type, Group, Instance) values.</param>
    /// <returns>A new ResourceReference instance.</returns>
    public static ResourceReference FromTgi((uint Type, uint Group, ulong Instance) tgi)
    {
        return new ResourceReference(tgi.Type, tgi.Group, tgi.Instance);
    }

    /// <summary>
    /// Converts this resource reference to a TGI tuple.
    /// </summary>
    /// <returns>A tuple containing (Type, Group, Instance) values.</returns>
    public (uint Type, uint Group, ulong Instance) ToTgi()
    {
        return (ResourceType, ResourceGroup, Instance);
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    /// <inheritdoc />
    /// <remarks>
    /// This event is never raised for this record struct since it's immutable,
    /// but is implemented to satisfy interface requirements for UI binding.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

    #endregion
}
