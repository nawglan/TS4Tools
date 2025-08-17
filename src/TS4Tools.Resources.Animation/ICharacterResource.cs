using System.ComponentModel.DataAnnotations;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Interface for character resources in The Sims 4.
/// </summary>
public interface ICharacterResource : IResource
{
    /// <summary>
    /// Gets the character resource type.
    /// </summary>
    CharacterType CharacterType { get; }

    /// <summary>
    /// Gets the character name or identifier.
    /// </summary>
    string CharacterName { get; }

    /// <summary>
    /// Gets the age category for this character resource.
    /// </summary>
    AgeCategory AgeCategory { get; }

    /// <summary>
    /// Gets the gender for this character resource.
    /// </summary>
    Gender Gender { get; }

    /// <summary>
    /// Gets the species for this character resource.
    /// </summary>
    Species Species { get; }

    /// <summary>
    /// Gets the character parts associated with this resource.
    /// </summary>
    IReadOnlyList<CharacterPart> CharacterParts { get; }

    /// <summary>
    /// Gets a value indicating whether this resource supports morphing.
    /// </summary>
    bool SupportsMorphing { get; }

    /// <summary>
    /// Gets the priority for character resource loading.
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Interface for rig (skeleton) resources in The Sims 4.
/// </summary>
public interface IRigResource : IResource
{
    /// <summary>
    /// Gets the rig name or identifier.
    /// </summary>
    string RigName { get; }

    /// <summary>
    /// Gets the bones in this rig.
    /// </summary>
    IReadOnlyList<Bone> Bones { get; }

    /// <summary>
    /// Gets the bone count.
    /// </summary>
    int BoneCount { get; }

    /// <summary>
    /// Gets the version of the rig format.
    /// </summary>
    int RigVersion { get; }

    /// <summary>
    /// Gets a value indicating whether this rig supports inverse kinematics.
    /// </summary>
    bool SupportsIk { get; }
}
