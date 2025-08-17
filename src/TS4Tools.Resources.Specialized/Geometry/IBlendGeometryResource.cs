using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Geometry
{
    /// <summary>
    /// Interface for blend geometry resources that handle mesh blending and morphing data for animations.
    /// BlendGeometryResource provides morph target systems and blend shape functionality.
    /// </summary>
    public interface IBlendGeometryResource : IResource
    {
        /// <summary>
        /// Gets the number of vertices in the base mesh.
        /// </summary>
        int VertexCount { get; }

        /// <summary>
        /// Gets the number of blend shapes defined.
        /// </summary>
        int BlendShapeCount { get; }

        /// <summary>
        /// Gets the base mesh name/identifier.
        /// </summary>
        string MeshName { get; }

        /// <summary>
        /// Gets the blend geometry version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets whether this blend geometry has been validated.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets the base vertex positions.
        /// </summary>
        IReadOnlyList<Vector3> BaseVertices { get; }

        /// <summary>
        /// Gets the base vertex normals.
        /// </summary>
        IReadOnlyList<Vector3> BaseNormals { get; }

        /// <summary>
        /// Gets the base texture coordinates.
        /// </summary>
        IReadOnlyList<Vector2> BaseTextureCoordinates { get; }

        /// <summary>
        /// Gets the defined blend shapes.
        /// </summary>
        IReadOnlyDictionary<string, BlendShape> BlendShapes { get; }

        /// <summary>
        /// Gets blend shape groups for organization.
        /// </summary>
        IReadOnlyDictionary<string, BlendShapeGroup> BlendShapeGroups { get; }

        /// <summary>
        /// Gets blend geometry metadata.
        /// </summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Validates the blend geometry for consistency and errors.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with errors, warnings, and information.</returns>
        Task<BlendGeometryValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates blended vertex positions using blend weights.
        /// </summary>
        /// <param name="blendWeights">Blend weights for each blend shape.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Blended vertex positions.</returns>
        Task<IReadOnlyList<Vector3>> CalculateBlendedVerticesAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates blended vertex normals using blend weights.
        /// </summary>
        /// <param name="blendWeights">Blend weights for each blend shape.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Blended vertex normals.</returns>
        Task<IReadOnlyList<Vector3>> CalculateBlendedNormalsAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates a complete blended mesh with all components.
        /// </summary>
        /// <param name="blendWeights">Blend weights for each blend shape.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Complete blended mesh data.</returns>
        Task<BlendedMesh> CalculateBlendedMeshAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new blend shape.
        /// </summary>
        /// <param name="blendShape">Blend shape to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddBlendShapeAsync(BlendShape blendShape, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a blend shape by name.
        /// </summary>
        /// <param name="blendShapeName">Name of blend shape to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if removed, false if not found.</returns>
        Task<bool> RemoveBlendShapeAsync(string blendShapeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates blend shape data.
        /// </summary>
        /// <param name="blendShapeName">Name of blend shape to update.</param>
        /// <param name="blendShape">Updated blend shape data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if updated, false if not found.</returns>
        Task<bool> UpdateBlendShapeAsync(string blendShapeName, BlendShape blendShape, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a blend shape group.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <param name="group">Group definition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetBlendShapeGroupAsync(string groupName, BlendShapeGroup group, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates base mesh data.
        /// </summary>
        /// <param name="vertices">Base vertex positions.</param>
        /// <param name="normals">Base vertex normals.</param>
        /// <param name="textureCoordinates">Base texture coordinates.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateBaseMeshAsync(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<Vector3>? normals = null,
            IReadOnlyList<Vector2>? textureCoordinates = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates blend geometry metadata.
        /// </summary>
        /// <param name="metadata">Metadata to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all blend shapes but keeps base mesh.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearBlendShapesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets blend shapes by group.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <returns>Blend shapes in the group.</returns>
        IEnumerable<BlendShape> GetBlendShapesByGroup(string groupName);

        /// <summary>
        /// Checks if a blend shape exists.
        /// </summary>
        /// <param name="blendShapeName">Blend shape name.</param>
        /// <returns>True if exists, false otherwise.</returns>
        bool HasBlendShape(string blendShapeName);
    }

    /// <summary>
    /// Represents a blend shape with vertex deltas for morphing.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    public class BlendShape
    {
        /// <summary>
        /// Gets or sets the blend shape name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the blend shape description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the blend shape weight (default intensity).
        /// </summary>
        public float DefaultWeight { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the minimum allowed weight.
        /// </summary>
        public float MinWeight { get; set; } = 0.0f;

        /// <summary>
        /// Gets or sets the maximum allowed weight.
        /// </summary>
        public float MaxWeight { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the blend shape group.
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets vertex position deltas.
        /// Only vertices that change need entries in this collection.
        /// </summary>
        public Dictionary<int, Vector3> VertexDeltas { get; set; } = new();

        /// <summary>
        /// Gets or sets vertex normal deltas.
        /// Only vertices with changed normals need entries in this collection.
        /// </summary>
        public Dictionary<int, Vector3> NormalDeltas { get; set; } = new();

        /// <summary>
        /// Gets or sets vertex texture coordinate deltas.
        /// Only vertices with changed texture coordinates need entries in this collection.
        /// </summary>
        public Dictionary<int, Vector2> TextureCoordinateDeltas { get; set; } = new();

        /// <summary>
        /// Gets or sets blend shape metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a group of related blend shapes.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    public class BlendShapeGroup
    {
        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether blend shapes in this group are mutually exclusive.
        /// </summary>
        public bool IsMutuallyExclusive { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum total weight for this group.
        /// </summary>
        public float MaxTotalWeight { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets blend shape names in this group.
        /// </summary>
        public HashSet<string> BlendShapeNames { get; set; } = new();

        /// <summary>
        /// Gets or sets group metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a complete blended mesh with all components.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    public class BlendedMesh
    {
        /// <summary>
        /// Gets or sets the blended vertex positions.
        /// </summary>
        public required IReadOnlyList<Vector3> Vertices { get; set; }

        /// <summary>
        /// Gets or sets the blended vertex normals.
        /// </summary>
        public IReadOnlyList<Vector3>? Normals { get; set; }

        /// <summary>
        /// Gets or sets the blended texture coordinates.
        /// </summary>
        public IReadOnlyList<Vector2>? TextureCoordinates { get; set; }

        /// <summary>
        /// Gets or sets the blend weights that were applied.
        /// </summary>
        public IReadOnlyDictionary<string, float> AppliedWeights { get; set; } = new Dictionary<string, float>();

        /// <summary>
        /// Gets or sets metadata about the blending operation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of blend geometry validation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class BlendGeometryValidationResult
    {
        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets validation errors.
        /// </summary>
        public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Gets validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets validation information.
        /// </summary>
        public List<string> Information { get; set; } = new();

        /// <summary>
        /// Gets detected blend shape conflicts.
        /// </summary>
        public List<BlendShapeConflict> DetectedConflicts { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>Successful validation result.</returns>
        public static BlendGeometryValidationResult Success()
        {
            return new BlendGeometryValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">Validation errors.</param>
        /// <returns>Failed validation result.</returns>
        public static BlendGeometryValidationResult Failure(params string[] errors)
        {
            return new BlendGeometryValidationResult
            {
                IsValid = false,
                Errors = errors.ToArray()
            };
        }

        /// <summary>
        /// Creates a successful validation result with warnings.
        /// </summary>
        /// <param name="warnings">Validation warnings.</param>
        /// <returns>Successful validation result with warnings.</returns>
        public static BlendGeometryValidationResult WithWarnings(params string[] warnings)
        {
            return new BlendGeometryValidationResult
            {
                IsValid = true,
                Warnings = warnings.ToList()
            };
        }

        /// <summary>
        /// Creates a successful validation result with information.
        /// </summary>
        /// <param name="information">Validation information.</param>
        /// <returns>Successful validation result with information.</returns>
        public static BlendGeometryValidationResult WithInformation(params string[] information)
        {
            return new BlendGeometryValidationResult
            {
                IsValid = true,
                Information = information.ToList()
            };
        }
    }

    /// <summary>
    /// Represents a blend shape conflict.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    public class BlendShapeConflict
    {
        /// <summary>
        /// Gets or sets the conflict type.
        /// </summary>
        public BlendShapeConflictType ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the conflicting blend shape name.
        /// </summary>
        public string BlendShapeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the conflict description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional conflict details.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Types of blend shape conflicts.
    /// </summary>
    public enum BlendShapeConflictType
    {
        /// <summary>
        /// Vertex index out of range.
        /// </summary>
        VertexIndexOutOfRange,

        /// <summary>
        /// Duplicate blend shape name.
        /// </summary>
        DuplicateName,

        /// <summary>
        /// Invalid weight range.
        /// </summary>
        InvalidWeightRange,

        /// <summary>
        /// Group constraint violation.
        /// </summary>
        GroupConstraintViolation,

        /// <summary>
        /// Missing vertex data.
        /// </summary>
        MissingVertexData,

        /// <summary>
        /// Inconsistent data dimensions.
        /// </summary>
        InconsistentDataDimensions
    }
}
