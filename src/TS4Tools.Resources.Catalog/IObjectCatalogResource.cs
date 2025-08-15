using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Interface for object catalog resources that define Buy/Build mode objects.
/// Extends the base catalog resource with object-specific properties and functionality.
/// </summary>
public interface IObjectCatalogResource : ICatalogResource
{
    /// <summary>
    /// Gets the simoleon price of this object in the catalog.
    /// This is the base price before any modifiers or discounts.
    /// </summary>
    uint Price { get; }

    /// <summary>
    /// Gets the object categories this item belongs to.
    /// Used for catalog filtering and organization in Buy/Build mode.
    /// </summary>
    IReadOnlyList<uint> Categories { get; }

    /// <summary>
    /// Gets the environment impact scores for different room aspects.
    /// Values typically range from -10 to +10 representing negative to positive impact.
    /// </summary>
    IReadOnlyDictionary<EnvironmentType, float> EnvironmentScores { get; }

    /// <summary>
    /// Gets the placement rules that determine where this object can be placed.
    /// Includes surface requirements, wall mounting, and space constraints.
    /// </summary>
    ObjectPlacementRules PlacementRules { get; }

    /// <summary>
    /// Gets the TGI reference list for UI icons associated with this object.
    /// Multiple icons may be provided for different display contexts (catalog, inventory, etc.).
    /// </summary>
    IReadOnlyList<TgiReference> Icons { get; }

    /// <summary>
    /// Gets the rig reference information for object animation and interaction points.
    /// Defines where Sims can interact with the object and how it can be animated.
    /// </summary>
    ObjectRigInfo? RigInfo { get; }

    /// <summary>
    /// Gets the slot placement information for objects that can be placed on this object.
    /// Includes position and orientation data for containment slots.
    /// </summary>
    IReadOnlyList<ObjectSlotInfo> Slots { get; }

    /// <summary>
    /// Gets the catalog tags associated with this object for advanced filtering.
    /// Tags enable content creators to add custom categorization.
    /// </summary>
    IReadOnlyList<uint> Tags { get; }

    /// <summary>
    /// Gets a value indicating whether this object is available for purchase in the catalog.
    /// Hidden objects may exist in the game but not be available for player purchase.
    /// </summary>
    bool IsAvailableForPurchase { get; }

    /// <summary>
    /// Gets the depreciation value indicating how the object loses value over time.
    /// Used for calculating resale value and insurance calculations.
    /// </summary>
    float DepreciationValue { get; }

    /// <summary>
    /// Gets the catalog description key for localized object description text.
    /// References a string table entry for multi-language support.
    /// </summary>
    uint DescriptionKey { get; }

    /// <summary>
    /// Gets additional object-specific property data.
    /// Contains type-specific properties that vary by object category.
    /// </summary>
    IReadOnlyDictionary<uint, object> Properties { get; }
}

/// <summary>
/// Defines environment impact types for object catalog entries.
/// These values affect Sim mood and behavior in rooms containing the object.
/// </summary>
public enum EnvironmentType : int
{
    /// <summary>No environment impact</summary>
    None = 0x00,

    /// <summary>Overall room fun factor</summary>
    Fun = 0x01,

    /// <summary>Room comfort level</summary>
    Comfort = 0x02,

    /// <summary>Room hygiene cleanliness</summary>
    Hygiene = 0x03,

    /// <summary>Room lighting quality</summary>
    Lighting = 0x04,

    /// <summary>Room decorative appeal</summary>
    Decor = 0x05,

    /// <summary>Room organizational efficiency</summary>
    Organization = 0x06,

    /// <summary>Room social interaction support</summary>
    Social = 0x07,

    /// <summary>Room learning environment quality</summary>
    Learning = 0x08,

    /// <summary>Room entertainment value</summary>
    Entertainment = 0x09,

    /// <summary>Room energy/productivity boost</summary>
    Energy = 0x0A
}

/// <summary>
/// Defines placement rules and constraints for catalog objects.
/// Controls where and how objects can be placed in the game world.
/// </summary>
public class ObjectPlacementRules : IEquatable<ObjectPlacementRules>
{
    /// <summary>
    /// Gets or sets a value indicating whether the object can be placed on floors.
    /// </summary>
    public bool CanPlaceOnFloor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object can be wall-mounted.
    /// </summary>
    public bool CanPlaceOnWall { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object can be placed on surfaces/tables.
    /// </summary>
    public bool CanPlaceOnSurface { get; set; }

    /// <summary>
    /// Gets or sets the minimum space requirements around the object in tiles.
    /// </summary>
    public float RequiredClearance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object blocks Sim movement.
    /// </summary>
    public bool BlocksMovement { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object can be placed outside.
    /// </summary>
    public bool CanPlaceOutside { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the object requires a specific room type.
    /// </summary>
    public bool RequiresSpecificRoom { get; set; }

    /// <summary>
    /// Gets or sets the required room type flags if RequiresSpecificRoom is true.
    /// </summary>
    public uint RoomTypeFlags { get; set; }

    /// <inheritdoc />
    public bool Equals(ObjectPlacementRules? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return CanPlaceOnFloor == other.CanPlaceOnFloor &&
               CanPlaceOnWall == other.CanPlaceOnWall &&
               CanPlaceOnSurface == other.CanPlaceOnSurface &&
               RequiredClearance.Equals(other.RequiredClearance) &&
               BlocksMovement == other.BlocksMovement &&
               CanPlaceOutside == other.CanPlaceOutside &&
               RequiresSpecificRoom == other.RequiresSpecificRoom &&
               RoomTypeFlags == other.RoomTypeFlags;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ObjectPlacementRules);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        CanPlaceOnFloor, CanPlaceOnWall, CanPlaceOnSurface, RequiredClearance,
        BlocksMovement, CanPlaceOutside, RequiresSpecificRoom, RoomTypeFlags);
}

/// <summary>
/// Contains rig reference information for object animation and interaction.
/// Defines how Sims can interact with the object and animation capabilities.
/// </summary>
public class ObjectRigInfo : IEquatable<ObjectRigInfo>
{
    /// <summary>
    /// Gets or sets the TGI reference to the object's rig resource.
    /// </summary>
    public TgiReference RigReference { get; set; } = TgiReference.Null;

    /// <summary>
    /// Gets or sets the number of interaction points available on this object.
    /// </summary>
    public uint InteractionPointCount { get; set; }

    /// <summary>
    /// Gets or sets flags indicating supported animation types.
    /// </summary>
    public uint AnimationFlags { get; set; }

    /// <summary>
    /// Gets or sets the rig complexity level affecting performance.
    /// </summary>
    public uint RigComplexity { get; set; }

    /// <inheritdoc />
    public bool Equals(ObjectRigInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return RigReference.Equals(other.RigReference) &&
               InteractionPointCount == other.InteractionPointCount &&
               AnimationFlags == other.AnimationFlags &&
               RigComplexity == other.RigComplexity;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ObjectRigInfo);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(RigReference, InteractionPointCount, AnimationFlags, RigComplexity);
}

/// <summary>
/// Information about object slots where other objects can be placed.
/// Used for containers, surfaces, and multi-part object systems.
/// </summary>
public class ObjectSlotInfo : IEquatable<ObjectSlotInfo>
{
    /// <summary>
    /// Gets or sets the slot identifier.
    /// </summary>
    public uint SlotId { get; set; }

    /// <summary>
    /// Gets or sets the slot position relative to the object origin.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the slot rotation in degrees.
    /// </summary>
    public Vector3 Rotation { get; set; }

    /// <summary>
    /// Gets or sets the types of objects that can be placed in this slot.
    /// </summary>
    public uint AllowedObjectTypes { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of objects that can fit in this slot.
    /// </summary>
    public Vector3 MaxObjectSize { get; set; }

    /// <inheritdoc />
    public bool Equals(ObjectSlotInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return SlotId == other.SlotId &&
               Position.Equals(other.Position) &&
               Rotation.Equals(other.Rotation) &&
               AllowedObjectTypes == other.AllowedObjectTypes &&
               MaxObjectSize.Equals(other.MaxObjectSize);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ObjectSlotInfo);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(SlotId, Position, Rotation, AllowedObjectTypes, MaxObjectSize);
}

/// <summary>
/// Simple 3D vector for position and size information.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector3"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <inheritdoc />
    public bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>
    /// Checks equality between two Vector3 instances.
    /// </summary>
    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);

    /// <summary>
    /// Checks inequality between two Vector3 instances.
    /// </summary>
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);
}
