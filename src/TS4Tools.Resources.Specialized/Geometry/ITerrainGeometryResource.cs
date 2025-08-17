using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Geometry
{
    /// <summary>
    /// Terrain geometry data structure representing height maps and terrain patches.
    /// </summary>
    public class TerrainPatch
    {
        /// <summary>
        /// Gets or sets the patch identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the X coordinate of the patch in world space.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the patch in world space.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets the width of the patch in terrain units.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the patch in terrain units.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the height values for this patch (2D array flattened).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<float> HeightData { get; set; } = new();

        /// <summary>
        /// Gets or sets the normal vectors for this patch.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<Vector3> Normals { get; set; } = new();

        /// <summary>
        /// Gets or sets the texture coordinate mappings.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<Vector2> TextureCoordinates { get; set; } = new();

        /// <summary>
        /// Gets or sets the material indices for texture blending.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<int> MaterialIndices { get; set; } = new();

        /// <summary>
        /// Gets or sets the blend weights for materials.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public Dictionary<int, float> MaterialWeights { get; set; } = new();

        /// <summary>
        /// Gets or sets additional patch metadata.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Terrain material definition for texture blending and rendering.
    /// </summary>
    public class TerrainMaterial
    {
        /// <summary>
        /// Gets or sets the material identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diffuse texture path.
        /// </summary>
        public string DiffuseTexture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the normal map texture path.
        /// </summary>
        public string NormalTexture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material properties.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public Dictionary<string, float> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets the texture scale.
        /// </summary>
        public Vector2 TextureScale { get; set; } = Vector2.One;

        /// <summary>
        /// Gets or sets the material color tint.
        /// </summary>
        public Vector4 Color { get; set; } = Vector4.One;
    }

    /// <summary>
    /// Result of terrain geometry validation operations.
    /// </summary>
    public class TerrainValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of validation warnings.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of validation information messages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class design pattern")]
        public List<string> Information { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful TerrainValidationResult.</returns>
        public static TerrainValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A failed TerrainValidationResult.</returns>
        public static TerrainValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };

        /// <summary>
        /// Creates a validation result with warnings.
        /// </summary>
        /// <param name="warnings">The validation warnings.</param>
        /// <returns>A TerrainValidationResult with warnings.</returns>
        public static TerrainValidationResult WithWarnings(params string[] warnings) => new()
        {
            IsValid = true,
            Warnings = new List<string>(warnings)
        };

        /// <summary>
        /// Creates a validation result with information.
        /// </summary>
        /// <param name="information">The validation information.</param>
        /// <returns>A TerrainValidationResult with information.</returns>
        public static TerrainValidationResult WithInformation(params string[] information) => new()
        {
            IsValid = true,
            Information = new List<string>(information)
        };
    }

    /// <summary>
    /// Terrain mesh data for rendering and collision detection.
    /// </summary>
    public class TerrainMesh
    {
        /// <summary>
        /// Gets or sets the mesh vertices.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public IReadOnlyList<Vector3> Vertices { get; set; } = Array.Empty<Vector3>();

        /// <summary>
        /// Gets or sets the mesh normals.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public IReadOnlyList<Vector3> Normals { get; set; } = Array.Empty<Vector3>();

        /// <summary>
        /// Gets or sets the texture coordinates.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public IReadOnlyList<Vector2> TextureCoordinates { get; set; } = Array.Empty<Vector2>();

        /// <summary>
        /// Gets or sets the triangle indices.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public IReadOnlyList<int> Indices { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Gets or sets the material data per vertex or face.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public Dictionary<int, float> MaterialData { get; set; } = new();

        /// <summary>
        /// Gets or sets the mesh metadata.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class requires settable collections")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Interface for resources that handle terrain geometry data including height maps, patches, and materials.
    /// TerrainGeometryResource supports world terrain mesh generation, height map processing, and material blending.
    /// </summary>
    public interface ITerrainGeometryResource : IResource, IApiVersion
    {
        /// <summary>
        /// Gets the total number of terrain patches.
        /// </summary>
        int PatchCount { get; }

        /// <summary>
        /// Gets the total number of terrain materials.
        /// </summary>
        int MaterialCount { get; }

        /// <summary>
        /// Gets the terrain world width.
        /// </summary>
        int WorldWidth { get; }

        /// <summary>
        /// Gets the terrain world height.
        /// </summary>
        int WorldHeight { get; }

        /// <summary>
        /// Gets the terrain scale factor.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Gets whether the terrain has been validated.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets the read-only collection of terrain patches.
        /// </summary>
        IReadOnlyDictionary<string, TerrainPatch> TerrainPatches { get; }

        /// <summary>
        /// Gets the read-only collection of terrain materials.
        /// </summary>
        IReadOnlyDictionary<string, TerrainMaterial> TerrainMaterials { get; }

        /// <summary>
        /// Gets the terrain metadata.
        /// </summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Validates the terrain geometry for consistency and correctness.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with any errors or warnings.</returns>
        Task<TerrainValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a mesh for the specified terrain patch.
        /// </summary>
        /// <param name="patchId">The patch identifier.</param>
        /// <param name="levelOfDetail">Level of detail (0 = highest, higher values = lower detail).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated terrain mesh.</returns>
        Task<TerrainMesh> GeneratePatchMeshAsync(string patchId, int levelOfDetail = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a combined mesh for multiple terrain patches.
        /// </summary>
        /// <param name="patchIds">The patch identifiers to combine.</param>
        /// <param name="levelOfDetail">Level of detail.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Combined terrain mesh.</returns>
        Task<TerrainMesh> GenerateCombinedMeshAsync(IEnumerable<string> patchIds, int levelOfDetail = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the height at a specific world coordinate using interpolation.
        /// </summary>
        /// <param name="worldX">World X coordinate.</param>
        /// <param name="worldY">World Y coordinate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Interpolated height value.</returns>
        Task<float> GetHeightAtWorldPositionAsync(float worldX, float worldY, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the normal vector at a specific world coordinate using interpolation.
        /// </summary>
        /// <param name="worldX">World X coordinate.</param>
        /// <param name="worldY">World Y coordinate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Interpolated normal vector.</returns>
        Task<Vector3> GetNormalAtWorldPositionAsync(float worldX, float worldY, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a terrain patch.
        /// </summary>
        /// <param name="patch">The terrain patch to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddTerrainPatchAsync(TerrainPatch patch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a terrain patch.
        /// </summary>
        /// <param name="patchId">The patch identifier to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the patch was removed, false if it didn't exist.</returns>
        Task<bool> RemoveTerrainPatchAsync(string patchId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing terrain patch.
        /// </summary>
        /// <param name="patchId">The patch identifier to update.</param>
        /// <param name="patch">The updated patch data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the patch was updated, false if it didn't exist.</returns>
        Task<bool> UpdateTerrainPatchAsync(string patchId, TerrainPatch patch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a terrain material.
        /// </summary>
        /// <param name="material">The terrain material to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddTerrainMaterialAsync(TerrainMaterial material, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a terrain material.
        /// </summary>
        /// <param name="materialId">The material identifier to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the material was removed, false if it didn't exist.</returns>
        Task<bool> RemoveTerrainMaterialAsync(string materialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates terrain metadata.
        /// </summary>
        /// <param name="metadata">The metadata to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the terrain world dimensions and scale.
        /// </summary>
        /// <param name="worldWidth">World width in terrain units.</param>
        /// <param name="worldHeight">World height in terrain units.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetWorldDimensionsAsync(int worldWidth, int worldHeight, float scale, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all terrain data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearTerrainDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets terrain patches within a specific world region.
        /// </summary>
        /// <param name="minX">Minimum X coordinate.</param>
        /// <param name="minY">Minimum Y coordinate.</param>
        /// <param name="maxX">Maximum X coordinate.</param>
        /// <param name="maxY">Maximum Y coordinate.</param>
        /// <returns>Terrain patches within the region.</returns>
        IEnumerable<TerrainPatch> GetPatchesInRegion(int minX, int minY, int maxX, int maxY);

        /// <summary>
        /// Checks if a terrain patch exists.
        /// </summary>
        /// <param name="patchId">The patch identifier to check.</param>
        /// <returns>True if the patch exists, false otherwise.</returns>
        bool HasTerrainPatch(string patchId);

        /// <summary>
        /// Checks if a terrain material exists.
        /// </summary>
        /// <param name="materialId">The material identifier to check.</param>
        /// <returns>True if the material exists, false otherwise.</returns>
        bool HasTerrainMaterial(string materialId);
    }
}
