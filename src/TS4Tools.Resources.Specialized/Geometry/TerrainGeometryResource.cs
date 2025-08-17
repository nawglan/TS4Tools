using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Geometry
{
    /// <summary>
    /// Implementation of terrain geometry resources that handle world terrain mesh data.
    /// TerrainGeometryResource provides height map processing and terrain patch management.
    /// </summary>
    public class TerrainGeometryResource : ITerrainGeometryResource, IDisposable
    {
        private readonly Dictionary<string, TerrainPatch> _terrainPatches = new();
        private readonly Dictionary<string, TerrainMaterial> _terrainMaterials = new();
        private readonly Dictionary<string, object> _metadata = new();
        private int _worldWidth = 256;
        private int _worldHeight = 256;
        private float _scale = 1.0f;
        private bool _isValidated;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TerrainGeometryResource class.
        /// </summary>
        public TerrainGeometryResource() : this(256, 256, 1.0f)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TerrainGeometryResource class with specified dimensions.
        /// </summary>
        /// <param name="worldWidth">World width in terrain units.</param>
        /// <param name="worldHeight">World height in terrain units.</param>
        /// <param name="scale">Scale factor.</param>
        public TerrainGeometryResource(int worldWidth, int worldHeight, float scale)
        {
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _scale = scale;
            Stream = new MemoryStream();
        }

        #region ITerrainGeometryResource Implementation

        /// <inheritdoc />
        public int PatchCount => _terrainPatches.Count;

        /// <inheritdoc />
        public int MaterialCount => _terrainMaterials.Count;

        /// <inheritdoc />
        public int WorldWidth => _worldWidth;

        /// <inheritdoc />
        public int WorldHeight => _worldHeight;

        /// <inheritdoc />
        public float Scale => _scale;

        /// <inheritdoc />
        public bool IsValidated => _isValidated;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, TerrainPatch> TerrainPatches => _terrainPatches;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, TerrainMaterial> TerrainMaterials => _terrainMaterials;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        /// <inheritdoc />
        public async Task<TerrainValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            var errors = new List<string>();
            var warnings = new List<string>();
            var information = new List<string>();

            // Validate world dimensions
            if (_worldWidth <= 0 || _worldHeight <= 0)
            {
                errors.Add($"Invalid world dimensions: {_worldWidth}x{_worldHeight}");
            }

            if (_scale <= 0)
            {
                errors.Add($"Invalid scale factor: {_scale}");
            }

            // Validate terrain patches
            foreach (var patch in _terrainPatches.Values)
            {
                ValidateTerrainPatch(patch, errors, warnings);
            }

            // Validate terrain materials
            foreach (var material in _terrainMaterials.Values)
            {
                ValidateTerrainMaterial(material, errors, warnings);
            }

            // Check for patch overlap
            var overlaps = FindPatchOverlaps();
            if (overlaps.Any())
            {
                warnings.AddRange(overlaps.Select(overlap => $"Patches {overlap.Item1} and {overlap.Item2} overlap"));
            }

            // Check for missing materials referenced by patches
            var missingMaterials = FindMissingMaterialReferences();
            if (missingMaterials.Any())
            {
                errors.AddRange(missingMaterials.Select(material => $"Patch references missing material: {material}"));
            }

            // Generate statistics
            information.Add($"Total patches: {PatchCount}");
            information.Add($"Total materials: {MaterialCount}");
            information.Add($"World dimensions: {WorldWidth}x{WorldHeight}");
            information.Add($"Scale factor: {Scale}");

            if (PatchCount > 0)
            {
                var totalHeightData = _terrainPatches.Values.Sum(p => p.HeightData.Count);
                var averageHeightData = totalHeightData / (float)PatchCount;
                information.Add($"Average height data points per patch: {averageHeightData:F1}");
            }

            _isValidated = errors.Count == 0;

            await Task.CompletedTask;

            if (errors.Count > 0)
            {
                var result = TerrainValidationResult.Failure(errors.ToArray());
                result.Warnings = warnings;
                result.Information = information;
                return result;
            }

            if (warnings.Count > 0)
            {
                var result = TerrainValidationResult.WithWarnings(warnings.ToArray());
                result.Information = information;
                return result;
            }

            return information.Count > 0
                ? TerrainValidationResult.WithInformation(information.ToArray())
                : TerrainValidationResult.Success();
        }

        /// <inheritdoc />
        public async Task<TerrainMesh> GeneratePatchMeshAsync(string patchId, int levelOfDetail = 0, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(patchId);

            if (!_terrainPatches.TryGetValue(patchId, out var patch))
            {
                throw new ArgumentException($"Terrain patch '{patchId}' not found", nameof(patchId));
            }

            var lod = Math.Max(0, levelOfDetail);
            var skipFactor = (int)Math.Pow(2, lod);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var textureCoordinates = new List<Vector2>();
            var indices = new List<int>();
            var materialData = new Dictionary<int, float>();

            // Generate vertices from height data
            for (int y = 0; y < patch.Height; y += skipFactor)
            {
                for (int x = 0; x < patch.Width; x += skipFactor)
                {
                    var heightIndex = y * patch.Width + x;
                    if (heightIndex < patch.HeightData.Count)
                    {
                        var height = patch.HeightData[heightIndex];
                        var worldX = (patch.X + x) * _scale;
                        var worldY = (patch.Y + y) * _scale;

                        vertices.Add(new Vector3(worldX, height, worldY));

                        // Add normal if available
                        if (heightIndex < patch.Normals.Count)
                        {
                            normals.Add(patch.Normals[heightIndex]);
                        }
                        else
                        {
                            // Calculate normal from height data
                            var normal = CalculateNormalFromHeightData(patch, x, y);
                            normals.Add(normal);
                        }

                        // Add texture coordinate
                        var u = x / (float)(patch.Width - 1);
                        var v = y / (float)(patch.Height - 1);
                        textureCoordinates.Add(new Vector2(u, v));

                        // Add material data if available
                        if (heightIndex < patch.MaterialIndices.Count)
                        {
                            var materialIndex = patch.MaterialIndices[heightIndex];
                            materialData[vertices.Count - 1] = materialIndex;
                        }
                    }
                }
            }

            // Generate triangle indices
            var widthInVertices = (patch.Width + skipFactor - 1) / skipFactor;
            var heightInVertices = (patch.Height + skipFactor - 1) / skipFactor;

            for (int y = 0; y < heightInVertices - 1; y++)
            {
                for (int x = 0; x < widthInVertices - 1; x++)
                {
                    var topLeft = y * widthInVertices + x;
                    var topRight = topLeft + 1;
                    var bottomLeft = (y + 1) * widthInVertices + x;
                    var bottomRight = bottomLeft + 1;

                    // First triangle
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    // Second triangle
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            await Task.CompletedTask;

            return new TerrainMesh
            {
                Vertices = vertices,
                Normals = normals,
                TextureCoordinates = textureCoordinates,
                Indices = indices,
                MaterialData = materialData,
                Metadata = new Dictionary<string, object>
                {
                    ["PatchId"] = patchId,
                    ["LevelOfDetail"] = levelOfDetail,
                    ["GenerationTime"] = DateTime.UtcNow,
                    ["VertexCount"] = vertices.Count,
                    ["TriangleCount"] = indices.Count / 3
                }
            };
        }

        /// <inheritdoc />
        public async Task<TerrainMesh> GenerateCombinedMeshAsync(IEnumerable<string> patchIds, int levelOfDetail = 0, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentNullException.ThrowIfNull(patchIds);

            var patchIdList = patchIds.ToList();
            if (patchIdList.Count == 0)
            {
                return new TerrainMesh();
            }

            var combinedVertices = new List<Vector3>();
            var combinedNormals = new List<Vector3>();
            var combinedTextureCoordinates = new List<Vector2>();
            var combinedIndices = new List<int>();
            var combinedMaterialData = new Dictionary<int, float>();

            var vertexOffset = 0;

            foreach (var patchId in patchIdList)
            {
                var patchMesh = await GeneratePatchMeshAsync(patchId, levelOfDetail, cancellationToken);

                // Add vertices with offset
                combinedVertices.AddRange(patchMesh.Vertices);
                combinedNormals.AddRange(patchMesh.Normals);
                combinedTextureCoordinates.AddRange(patchMesh.TextureCoordinates);

                // Add indices with vertex offset
                foreach (var index in patchMesh.Indices)
                {
                    combinedIndices.Add(index + vertexOffset);
                }

                // Add material data with vertex offset
                foreach (var kvp in patchMesh.MaterialData)
                {
                    combinedMaterialData[kvp.Key + vertexOffset] = kvp.Value;
                }

                vertexOffset += patchMesh.Vertices.Count;
            }

            await Task.CompletedTask;

            return new TerrainMesh
            {
                Vertices = combinedVertices,
                Normals = combinedNormals,
                TextureCoordinates = combinedTextureCoordinates,
                Indices = combinedIndices,
                MaterialData = combinedMaterialData,
                Metadata = new Dictionary<string, object>
                {
                    ["CombinedPatches"] = patchIdList,
                    ["LevelOfDetail"] = levelOfDetail,
                    ["GenerationTime"] = DateTime.UtcNow,
                    ["VertexCount"] = combinedVertices.Count,
                    ["TriangleCount"] = combinedIndices.Count / 3
                }
            };
        }

        /// <inheritdoc />
        public async Task<float> GetHeightAtWorldPositionAsync(float worldX, float worldY, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            // Convert world coordinates to terrain coordinates
            var terrainX = worldX / _scale;
            var terrainY = worldY / _scale;

            // Find the patch containing this position
            var patch = FindPatchContainingPosition((int)terrainX, (int)terrainY);
            if (patch == null)
            {
                return 0.0f; // Default height for areas outside patches
            }

            // Calculate local coordinates within the patch
            var localX = terrainX - patch.X;
            var localY = terrainY - patch.Y;

            // Bilinear interpolation of height values
            var height = InterpolateHeight(patch, localX, localY);

            await Task.CompletedTask;
            return height;
        }

        /// <inheritdoc />
        public async Task<Vector3> GetNormalAtWorldPositionAsync(float worldX, float worldY, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            // Convert world coordinates to terrain coordinates
            var terrainX = worldX / _scale;
            var terrainY = worldY / _scale;

            // Find the patch containing this position
            var patch = FindPatchContainingPosition((int)terrainX, (int)terrainY);
            if (patch == null)
            {
                return Vector3.UnitY; // Default up normal for areas outside patches
            }

            // Calculate local coordinates within the patch
            var localX = terrainX - patch.X;
            var localY = terrainY - patch.Y;

            // Interpolate normal vector
            var normal = InterpolateNormal(patch, localX, localY);

            await Task.CompletedTask;
            return normal;
        }

        /// <inheritdoc />
        public async Task AddTerrainPatchAsync(TerrainPatch patch, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentNullException.ThrowIfNull(patch);
            ArgumentException.ThrowIfNullOrWhiteSpace(patch.Id);

            _terrainPatches[patch.Id] = patch;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveTerrainPatchAsync(string patchId, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(patchId);

            if (_terrainPatches.Remove(patchId))
            {
                _isValidated = false; // Invalidate validation
                OnResourceChanged();

                await Task.CompletedTask;
                return true;
            }

            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateTerrainPatchAsync(string patchId, TerrainPatch patch, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
            ArgumentNullException.ThrowIfNull(patch);

            if (!_terrainPatches.ContainsKey(patchId))
            {
                return false;
            }

            _terrainPatches[patchId] = patch;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
            return true;
        }

        /// <inheritdoc />
        public async Task AddTerrainMaterialAsync(TerrainMaterial material, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentNullException.ThrowIfNull(material);
            ArgumentException.ThrowIfNullOrWhiteSpace(material.Id);

            _terrainMaterials[material.Id] = material;
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveTerrainMaterialAsync(string materialId, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(materialId);

            if (_terrainMaterials.Remove(materialId))
            {
                OnResourceChanged();

                await Task.CompletedTask;
                return true;
            }

            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc />
        public async Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentNullException.ThrowIfNull(metadata);

            _metadata.Clear();
            foreach (var kvp in metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }

            OnResourceChanged();
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task SetWorldDimensionsAsync(int worldWidth, int worldHeight, float scale, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            if (worldWidth <= 0 || worldHeight <= 0)
            {
                throw new ArgumentException("World dimensions must be positive");
            }

            if (scale <= 0)
            {
                throw new ArgumentException("Scale must be positive");
            }

            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _scale = scale;

            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ClearTerrainDataAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            _terrainPatches.Clear();
            _terrainMaterials.Clear();
            _metadata.Clear();
            _isValidated = false;
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public IEnumerable<TerrainPatch> GetPatchesInRegion(int minX, int minY, int maxX, int maxY)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));

            return _terrainPatches.Values.Where(patch =>
                patch.X < maxX && patch.X + patch.Width > minX &&
                patch.Y < maxY && patch.Y + patch.Height > minY);
        }

        /// <inheritdoc />
        public bool HasTerrainPatch(string patchId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(patchId);

            return _terrainPatches.ContainsKey(patchId);
        }

        /// <inheritdoc />
        public bool HasTerrainMaterial(string materialId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TerrainGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(materialId);

            return _terrainMaterials.ContainsKey(materialId);
        }

        #endregion

        #region IResource Implementation

        /// <inheritdoc />
        public Stream Stream { get; private set; }

        /// <inheritdoc />
        public byte[] AsBytes => SerializeToBytes();

        /// <inheritdoc />
        public event EventHandler? ResourceChanged;

        /// <inheritdoc />
        public IReadOnlyList<string> ContentFields => new[]
        {
            "WorldWidth", "WorldHeight", "Scale", "PatchCount",
            "MaterialCount", "IsValidated"
        };

        /// <inheritdoc />
        TypedValue IContentFields.this[int index]
        {
            get => index switch
            {
                0 => TypedValue.Create(WorldWidth, "int"),
                1 => TypedValue.Create(WorldHeight, "int"),
                2 => TypedValue.Create(Scale, "float"),
                3 => TypedValue.Create(PatchCount, "int"),
                4 => TypedValue.Create(MaterialCount, "int"),
                5 => TypedValue.Create(IsValidated, "bool"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (index)
                {
                    case 0:
                        if (value.Value is int width && width > 0)
                            _worldWidth = width;
                        break;
                    case 1:
                        if (value.Value is int height && height > 0)
                            _worldHeight = height;
                        break;
                    case 2:
                        if (value.Value is float scale && scale > 0)
                            _scale = scale;
                        break;
                }
                OnResourceChanged();
            }
        }

        /// <inheritdoc />
        TypedValue IContentFields.this[string name]
        {
            get => name switch
            {
                "WorldWidth" => TypedValue.Create(WorldWidth, "int"),
                "WorldHeight" => TypedValue.Create(WorldHeight, "int"),
                "Scale" => TypedValue.Create(Scale, "float"),
                "PatchCount" => TypedValue.Create(PatchCount, "int"),
                "MaterialCount" => TypedValue.Create(MaterialCount, "int"),
                "IsValidated" => TypedValue.Create(IsValidated, "bool"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (name)
                {
                    case "WorldWidth":
                        if (value.Value is int width && width > 0)
                            _worldWidth = width;
                        break;
                    case "WorldHeight":
                        if (value.Value is int height && height > 0)
                            _worldHeight = height;
                        break;
                    case "Scale":
                        if (value.Value is float scale && scale > 0)
                            _scale = scale;
                        break;
                }
                OnResourceChanged();
            }
        }

        /// <inheritdoc />
        public object? this[string index]
        {
            get => ((IContentFields)this)[index].Value;
            set => ((IContentFields)this)[index] = TypedValue.Create(value);
        }

        #endregion

        #region IApiVersion Implementation

        /// <inheritdoc />
        public int RecommendedApiVersion => 1;

        /// <inheritdoc />
        public int RequestedApiVersion { get; set; } = 1;

        #endregion

        #region Private Methods

        private static void ValidateTerrainPatch(TerrainPatch patch, List<string> errors, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(patch.Id))
            {
                errors.Add("Terrain patch ID cannot be empty");
            }

            if (patch.Width <= 0 || patch.Height <= 0)
            {
                errors.Add($"Patch '{patch.Id}' has invalid dimensions: {patch.Width}x{patch.Height}");
            }

            var expectedHeightCount = patch.Width * patch.Height;
            if (patch.HeightData.Count != expectedHeightCount)
            {
                errors.Add($"Patch '{patch.Id}' height data count ({patch.HeightData.Count}) does not match dimensions ({expectedHeightCount})");
            }

            if (patch.Normals.Count > 0 && patch.Normals.Count != expectedHeightCount)
            {
                warnings.Add($"Patch '{patch.Id}' normal count ({patch.Normals.Count}) does not match height data count ({patch.HeightData.Count})");
            }

            if (patch.TextureCoordinates.Count > 0 && patch.TextureCoordinates.Count != expectedHeightCount)
            {
                warnings.Add($"Patch '{patch.Id}' texture coordinate count ({patch.TextureCoordinates.Count}) does not match height data count ({patch.HeightData.Count})");
            }
        }

        private static void ValidateTerrainMaterial(TerrainMaterial material, List<string> errors, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(material.Id))
            {
                errors.Add("Terrain material ID cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(material.Name))
            {
                warnings.Add($"Material '{material.Id}' has no name");
            }

            if (material.TextureScale.X <= 0 || material.TextureScale.Y <= 0)
            {
                warnings.Add($"Material '{material.Id}' has invalid texture scale: {material.TextureScale}");
            }

            if (string.IsNullOrWhiteSpace(material.DiffuseTexture))
            {
                warnings.Add($"Material '{material.Id}' has no diffuse texture");
            }
        }

        private List<(string, string)> FindPatchOverlaps()
        {
            var overlaps = new List<(string, string)>();
            var patches = _terrainPatches.Values.ToList();

            for (int i = 0; i < patches.Count; i++)
            {
                for (int j = i + 1; j < patches.Count; j++)
                {
                    var patch1 = patches[i];
                    var patch2 = patches[j];

                    if (PatchesOverlap(patch1, patch2))
                    {
                        overlaps.Add((patch1.Id, patch2.Id));
                    }
                }
            }

            return overlaps;
        }

        private static bool PatchesOverlap(TerrainPatch patch1, TerrainPatch patch2)
        {
            return patch1.X < patch2.X + patch2.Width &&
                   patch1.X + patch1.Width > patch2.X &&
                   patch1.Y < patch2.Y + patch2.Height &&
                   patch1.Y + patch1.Height > patch2.Y;
        }

        private List<string> FindMissingMaterialReferences()
        {
            var missingMaterials = new HashSet<string>();

            foreach (var patch in _terrainPatches.Values)
            {
                foreach (var materialIndex in patch.MaterialIndices)
                {
                    var materialId = materialIndex.ToString();
                    if (!_terrainMaterials.ContainsKey(materialId))
                    {
                        missingMaterials.Add(materialId);
                    }
                }

                foreach (var materialId in patch.MaterialWeights.Keys.Select(k => k.ToString()))
                {
                    if (!_terrainMaterials.ContainsKey(materialId))
                    {
                        missingMaterials.Add(materialId);
                    }
                }
            }

            return missingMaterials.ToList();
        }

        private TerrainPatch? FindPatchContainingPosition(int terrainX, int terrainY)
        {
            return _terrainPatches.Values.FirstOrDefault(patch =>
                terrainX >= patch.X && terrainX < patch.X + patch.Width &&
                terrainY >= patch.Y && terrainY < patch.Y + patch.Height);
        }

        private static float InterpolateHeight(TerrainPatch patch, float localX, float localY)
        {
            var x = Math.Clamp(localX, 0, patch.Width - 1);
            var y = Math.Clamp(localY, 0, patch.Height - 1);

            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var x1 = Math.Min(x0 + 1, patch.Width - 1);
            var y1 = Math.Min(y0 + 1, patch.Height - 1);

            var fx = x - x0;
            var fy = y - y0;

            var h00 = GetHeightAtIndex(patch, x0, y0);
            var h10 = GetHeightAtIndex(patch, x1, y0);
            var h01 = GetHeightAtIndex(patch, x0, y1);
            var h11 = GetHeightAtIndex(patch, x1, y1);

            // Bilinear interpolation
            var h0 = h00 * (1 - fx) + h10 * fx;
            var h1 = h01 * (1 - fx) + h11 * fx;
            return h0 * (1 - fy) + h1 * fy;
        }

        private static Vector3 InterpolateNormal(TerrainPatch patch, float localX, float localY)
        {
            if (patch.Normals.Count == 0)
            {
                return Vector3.UnitY;
            }

            var x = Math.Clamp(localX, 0, patch.Width - 1);
            var y = Math.Clamp(localY, 0, patch.Height - 1);

            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var x1 = Math.Min(x0 + 1, patch.Width - 1);
            var y1 = Math.Min(y0 + 1, patch.Height - 1);

            var fx = x - x0;
            var fy = y - y0;

            var n00 = GetNormalAtIndex(patch, x0, y0);
            var n10 = GetNormalAtIndex(patch, x1, y0);
            var n01 = GetNormalAtIndex(patch, x0, y1);
            var n11 = GetNormalAtIndex(patch, x1, y1);

            // Bilinear interpolation
            var n0 = Vector3.Lerp(n00, n10, fx);
            var n1 = Vector3.Lerp(n01, n11, fx);
            var result = Vector3.Lerp(n0, n1, fy);

            return Vector3.Normalize(result);
        }

        private static float GetHeightAtIndex(TerrainPatch patch, int x, int y)
        {
            var index = y * patch.Width + x;
            return index < patch.HeightData.Count ? patch.HeightData[index] : 0.0f;
        }

        private static Vector3 GetNormalAtIndex(TerrainPatch patch, int x, int y)
        {
            var index = y * patch.Width + x;
            return index < patch.Normals.Count ? patch.Normals[index] : Vector3.UnitY;
        }

        private static Vector3 CalculateNormalFromHeightData(TerrainPatch patch, int x, int y)
        {
            // Calculate normal using finite differences
            var h = GetHeightAtIndex(patch, x, y);
            var hLeft = GetHeightAtIndex(patch, Math.Max(0, x - 1), y);
            var hRight = GetHeightAtIndex(patch, Math.Min(patch.Width - 1, x + 1), y);
            var hUp = GetHeightAtIndex(patch, x, Math.Max(0, y - 1));
            var hDown = GetHeightAtIndex(patch, x, Math.Min(patch.Height - 1, y + 1));

            var dx = hRight - hLeft;
            var dy = hDown - hUp;

            var normal = Vector3.Normalize(new Vector3(-dx, 2.0f, -dy));
            return normal;
        }

        private byte[] SerializeToBytes()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

            // Write header
            writer.Write(_worldWidth);
            writer.Write(_worldHeight);
            writer.Write(_scale);

            // Write terrain patches
            writer.Write(_terrainPatches.Count);
            foreach (var patch in _terrainPatches.Values)
            {
                WriteTerrainPatch(writer, patch);
            }

            // Write terrain materials
            writer.Write(_terrainMaterials.Count);
            foreach (var material in _terrainMaterials.Values)
            {
                WriteTerrainMaterial(writer, material);
            }

            // Write metadata
            writer.Write(_metadata.Count);
            foreach (var kvp in _metadata)
            {
                writer.Write(kvp.Key);
                WriteValue(writer, kvp.Value);
            }

            return memoryStream.ToArray();
        }

        private static void WriteTerrainPatch(BinaryWriter writer, TerrainPatch patch)
        {
            writer.Write(patch.Id);
            writer.Write(patch.X);
            writer.Write(patch.Y);
            writer.Write(patch.Width);
            writer.Write(patch.Height);

            // Write height data
            writer.Write(patch.HeightData.Count);
            foreach (var height in patch.HeightData)
            {
                writer.Write(height);
            }

            // Write normals
            writer.Write(patch.Normals.Count);
            foreach (var normal in patch.Normals)
            {
                writer.Write(normal.X);
                writer.Write(normal.Y);
                writer.Write(normal.Z);
            }

            // Write texture coordinates
            writer.Write(patch.TextureCoordinates.Count);
            foreach (var texCoord in patch.TextureCoordinates)
            {
                writer.Write(texCoord.X);
                writer.Write(texCoord.Y);
            }

            // Write material indices
            writer.Write(patch.MaterialIndices.Count);
            foreach (var materialIndex in patch.MaterialIndices)
            {
                writer.Write(materialIndex);
            }

            // Write material weights
            writer.Write(patch.MaterialWeights.Count);
            foreach (var kvp in patch.MaterialWeights)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            // Write metadata
            writer.Write(patch.Metadata.Count);
            foreach (var kvp in patch.Metadata)
            {
                writer.Write(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
        }

        private static void WriteTerrainMaterial(BinaryWriter writer, TerrainMaterial material)
        {
            writer.Write(material.Id);
            writer.Write(material.Name);
            writer.Write(material.DiffuseTexture);
            writer.Write(material.NormalTexture);

            writer.Write(material.TextureScale.X);
            writer.Write(material.TextureScale.Y);

            writer.Write(material.Color.X);
            writer.Write(material.Color.Y);
            writer.Write(material.Color.Z);
            writer.Write(material.Color.W);

            // Write properties
            writer.Write(material.Properties.Count);
            foreach (var kvp in material.Properties)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        private static void WriteValue(BinaryWriter writer, object value)
        {
            switch (value)
            {
                case string s:
                    writer.Write((byte)1);
                    writer.Write(s);
                    break;
                case int i:
                    writer.Write((byte)2);
                    writer.Write(i);
                    break;
                case float f:
                    writer.Write((byte)3);
                    writer.Write(f);
                    break;
                case bool b:
                    writer.Write((byte)4);
                    writer.Write(b);
                    break;
                default:
                    writer.Write((byte)0);
                    writer.Write(value.ToString() ?? string.Empty);
                    break;
            }
        }

        private void OnResourceChanged()
        {
            ResourceChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resource.
        /// </summary>
        /// <param name="disposing">Whether disposing from Dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Stream?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
