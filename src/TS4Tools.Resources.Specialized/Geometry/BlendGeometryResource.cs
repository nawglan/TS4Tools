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
    /// Implementation of blend geometry resources that handle mesh blending and morphing data for animations.
    /// BlendGeometryResource provides morph target systems and blend shape functionality.
    /// </summary>
    public class BlendGeometryResource : IBlendGeometryResource, IDisposable
    {
        private readonly List<Vector3> _baseVertices = new();
        private readonly List<Vector3> _baseNormals = new();
        private readonly List<Vector2> _baseTextureCoordinates = new();
        private readonly Dictionary<string, BlendShape> _blendShapes = new();
        private readonly Dictionary<string, BlendShapeGroup> _blendShapeGroups = new();
        private readonly Dictionary<string, object> _metadata = new();
        private readonly List<BlendShapeConflict> _conflicts = new();
        private bool _isValidated;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the BlendGeometryResource class.
        /// </summary>
        public BlendGeometryResource() : this("default_mesh", "1.0")
        {
        }

        /// <summary>
        /// Initializes a new instance of the BlendGeometryResource class with specified settings.
        /// </summary>
        /// <param name="meshName">Mesh name/identifier.</param>
        /// <param name="version">Version.</param>
        public BlendGeometryResource(string meshName, string version)
        {
            MeshName = meshName ?? string.Empty;
            Version = version ?? "1.0";
            Stream = new MemoryStream();
        }

        #region IBlendGeometryResource Implementation

        /// <inheritdoc />
        public int VertexCount => _baseVertices.Count;

        /// <inheritdoc />
        public int BlendShapeCount => _blendShapes.Count;

        /// <inheritdoc />
        public string MeshName { get; private set; }

        /// <inheritdoc />
        public string Version { get; private set; }

        /// <inheritdoc />
        public bool IsValidated => _isValidated;

        /// <inheritdoc />
        public IReadOnlyList<Vector3> BaseVertices => _baseVertices;

        /// <inheritdoc />
        public IReadOnlyList<Vector3> BaseNormals => _baseNormals;

        /// <inheritdoc />
        public IReadOnlyList<Vector2> BaseTextureCoordinates => _baseTextureCoordinates;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, BlendShape> BlendShapes => _blendShapes;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, BlendShapeGroup> BlendShapeGroups => _blendShapeGroups;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        /// <inheritdoc />
        public async Task<BlendGeometryValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));

            var errors = new List<string>();
            var warnings = new List<string>();
            var information = new List<string>();
            var conflicts = new List<BlendShapeConflict>();

            // Clear existing conflicts
            _conflicts.Clear();

            // Validate mesh name
            if (string.IsNullOrWhiteSpace(MeshName))
            {
                errors.Add("Mesh name cannot be empty");
            }

            // Validate base mesh data consistency
            if (_baseVertices.Count == 0)
            {
                warnings.Add("No base vertices defined");
            }
            else
            {
                if (_baseNormals.Count > 0 && _baseNormals.Count != _baseVertices.Count)
                {
                    var conflict = new BlendShapeConflict
                    {
                        ConflictType = BlendShapeConflictType.InconsistentDataDimensions,
                        Description = $"Base normals count ({_baseNormals.Count}) does not match vertex count ({_baseVertices.Count})"
                    };
                    conflicts.Add(conflict);
                    _conflicts.Add(conflict);
                    errors.Add(conflict.Description);
                }

                if (_baseTextureCoordinates.Count > 0 && _baseTextureCoordinates.Count != _baseVertices.Count)
                {
                    var conflict = new BlendShapeConflict
                    {
                        ConflictType = BlendShapeConflictType.InconsistentDataDimensions,
                        Description = $"Base texture coordinates count ({_baseTextureCoordinates.Count}) does not match vertex count ({_baseVertices.Count})"
                    };
                    conflicts.Add(conflict);
                    _conflicts.Add(conflict);
                    errors.Add(conflict.Description);
                }
            }

            // Validate blend shapes
            foreach (var blendShape in _blendShapes.Values)
            {
                await ValidateBlendShapeAsync(blendShape, errors, warnings, conflicts, cancellationToken);
            }

            // Validate blend shape groups
            foreach (var group in _blendShapeGroups.Values)
            {
                ValidateBlendShapeGroup(group, errors, warnings, conflicts);
            }

            // Check for duplicate blend shape names
            var duplicateNames = _blendShapes.Keys.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);
            foreach (var group in duplicateNames)
            {
                var conflict = new BlendShapeConflict
                {
                    ConflictType = BlendShapeConflictType.DuplicateName,
                    BlendShapeName = group.Key,
                    Description = $"Duplicate blend shape names found: {string.Join(", ", group)}"
                };
                conflicts.Add(conflict);
                _conflicts.Add(conflict);
                errors.Add(conflict.Description);
            }

            // Generate statistics
            information.Add($"Total vertices: {VertexCount}");
            information.Add($"Total blend shapes: {BlendShapeCount}");
            information.Add($"Total blend shape groups: {_blendShapeGroups.Count}");
            if (VertexCount > 0)
            {
                var totalDeltas = _blendShapes.Values.Sum(bs => bs.VertexDeltas.Count);
                var averageDeltas = totalDeltas / (float)BlendShapeCount;
                information.Add($"Average vertex deltas per blend shape: {averageDeltas:F1}");
            }

            _isValidated = errors.Count == 0;

            await Task.CompletedTask;

            if (errors.Count > 0)
            {
                var result = BlendGeometryValidationResult.Failure(errors.ToArray());
                result.Warnings = warnings;
                result.Information = information;
                result.DetectedConflicts = conflicts;
                return result;
            }

            if (warnings.Count > 0)
            {
                var result = BlendGeometryValidationResult.WithWarnings(warnings.ToArray());
                result.Information = information;
                result.DetectedConflicts = conflicts;
                return result;
            }

            return information.Count > 0
                ? BlendGeometryValidationResult.WithInformation(information.ToArray())
                : BlendGeometryValidationResult.Success();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Vector3>> CalculateBlendedVerticesAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentNullException.ThrowIfNull(blendWeights);

            var blendedVertices = new Vector3[_baseVertices.Count];

            // Start with base vertices
            for (int i = 0; i < _baseVertices.Count; i++)
            {
                blendedVertices[i] = _baseVertices[i];
            }

            // Apply blend shapes
            foreach (var weightEntry in blendWeights)
            {
                if (_blendShapes.TryGetValue(weightEntry.Key, out var blendShape))
                {
                    var weight = Math.Clamp(weightEntry.Value, blendShape.MinWeight, blendShape.MaxWeight);

                    foreach (var delta in blendShape.VertexDeltas)
                    {
                        if (delta.Key < blendedVertices.Length)
                        {
                            blendedVertices[delta.Key] += delta.Value * weight;
                        }
                    }
                }
            }

            await Task.CompletedTask;
            return blendedVertices;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Vector3>> CalculateBlendedNormalsAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentNullException.ThrowIfNull(blendWeights);

            if (_baseNormals.Count == 0)
            {
                return Array.Empty<Vector3>();
            }

            var blendedNormals = new Vector3[_baseNormals.Count];

            // Start with base normals
            for (int i = 0; i < _baseNormals.Count; i++)
            {
                blendedNormals[i] = _baseNormals[i];
            }

            // Apply blend shapes
            foreach (var weightEntry in blendWeights)
            {
                if (_blendShapes.TryGetValue(weightEntry.Key, out var blendShape))
                {
                    var weight = Math.Clamp(weightEntry.Value, blendShape.MinWeight, blendShape.MaxWeight);

                    foreach (var delta in blendShape.NormalDeltas)
                    {
                        if (delta.Key < blendedNormals.Length)
                        {
                            blendedNormals[delta.Key] += delta.Value * weight;
                        }
                    }
                }
            }

            // Normalize the blended normals
            for (int i = 0; i < blendedNormals.Length; i++)
            {
                blendedNormals[i] = Vector3.Normalize(blendedNormals[i]);
            }

            await Task.CompletedTask;
            return blendedNormals;
        }

        /// <inheritdoc />
        public async Task<BlendedMesh> CalculateBlendedMeshAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentNullException.ThrowIfNull(blendWeights);

            var blendedVertices = await CalculateBlendedVerticesAsync(blendWeights, cancellationToken);
            var blendedNormals = await CalculateBlendedNormalsAsync(blendWeights, cancellationToken);

            // Calculate blended texture coordinates if available
            IReadOnlyList<Vector2>? blendedTextureCoordinates = null;
            if (_baseTextureCoordinates.Count > 0)
            {
                blendedTextureCoordinates = await CalculateBlendedTextureCoordinatesAsync(blendWeights, cancellationToken);
            }

            return new BlendedMesh
            {
                Vertices = blendedVertices,
                Normals = blendedNormals.Count > 0 ? blendedNormals : null,
                TextureCoordinates = blendedTextureCoordinates,
                AppliedWeights = new Dictionary<string, float>(blendWeights),
                Metadata = new Dictionary<string, object>
                {
                    ["BlendTime"] = DateTime.UtcNow,
                    ["BlendShapeCount"] = blendWeights.Count,
                    ["VertexCount"] = blendedVertices.Count
                }
            };
        }

        /// <inheritdoc />
        public async Task AddBlendShapeAsync(BlendShape blendShape, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentNullException.ThrowIfNull(blendShape);
            ArgumentException.ThrowIfNullOrWhiteSpace(blendShape.Name);

            if (_blendShapes.ContainsKey(blendShape.Name))
            {
                throw new InvalidOperationException($"Blend shape '{blendShape.Name}' already exists");
            }

            _blendShapes[blendShape.Name] = blendShape;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBlendShapeAsync(string blendShapeName, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(blendShapeName);

            if (_blendShapes.Remove(blendShapeName))
            {
                // Remove from all groups
                foreach (var group in _blendShapeGroups.Values)
                {
                    group.BlendShapeNames.Remove(blendShapeName);
                }

                _isValidated = false; // Invalidate validation
                OnResourceChanged();

                await Task.CompletedTask;
                return true;
            }

            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateBlendShapeAsync(string blendShapeName, BlendShape blendShape, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(blendShapeName);
            ArgumentNullException.ThrowIfNull(blendShape);

            if (!_blendShapes.ContainsKey(blendShapeName))
            {
                return false;
            }

            _blendShapes[blendShapeName] = blendShape;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
            return true;
        }

        /// <inheritdoc />
        public async Task SetBlendShapeGroupAsync(string groupName, BlendShapeGroup group, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
            ArgumentNullException.ThrowIfNull(group);

            _blendShapeGroups[groupName] = group;
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task UpdateBaseMeshAsync(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<Vector3>? normals = null,
            IReadOnlyList<Vector2>? textureCoordinates = null,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentNullException.ThrowIfNull(vertices);

            _baseVertices.Clear();
            _baseVertices.AddRange(vertices);

            _baseNormals.Clear();
            if (normals != null)
            {
                _baseNormals.AddRange(normals);
            }

            _baseTextureCoordinates.Clear();
            if (textureCoordinates != null)
            {
                _baseTextureCoordinates.AddRange(textureCoordinates);
            }

            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
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
        public async Task ClearBlendShapesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));

            _blendShapes.Clear();
            _blendShapeGroups.Clear();
            _conflicts.Clear();
            _isValidated = false;
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public IEnumerable<BlendShape> GetBlendShapesByGroup(string groupName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

            if (_blendShapeGroups.TryGetValue(groupName, out var group))
            {
                return group.BlendShapeNames
                    .Where(name => _blendShapes.ContainsKey(name))
                    .Select(name => _blendShapes[name]);
            }

            return Enumerable.Empty<BlendShape>();
        }

        /// <inheritdoc />
        public bool HasBlendShape(string blendShapeName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BlendGeometryResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(blendShapeName);

            return _blendShapes.ContainsKey(blendShapeName);
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
            "MeshName", "Version", "VertexCount", "BlendShapeCount",
            "IsValidated", "GroupCount", "ConflictCount"
        };

        /// <inheritdoc />
        TypedValue IContentFields.this[int index]
        {
            get => index switch
            {
                0 => TypedValue.Create(MeshName, "string"),
                1 => TypedValue.Create(Version, "string"),
                2 => TypedValue.Create(VertexCount, "int"),
                3 => TypedValue.Create(BlendShapeCount, "int"),
                4 => TypedValue.Create(IsValidated, "bool"),
                5 => TypedValue.Create(_blendShapeGroups.Count, "int"),
                6 => TypedValue.Create(_conflicts.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (index)
                {
                    case 0:
                        MeshName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 1:
                        Version = value.Value?.ToString() ?? "1.0";
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
                "MeshName" => TypedValue.Create(MeshName, "string"),
                "Version" => TypedValue.Create(Version, "string"),
                "VertexCount" => TypedValue.Create(VertexCount, "int"),
                "BlendShapeCount" => TypedValue.Create(BlendShapeCount, "int"),
                "IsValidated" => TypedValue.Create(IsValidated, "bool"),
                "GroupCount" => TypedValue.Create(_blendShapeGroups.Count, "int"),
                "ConflictCount" => TypedValue.Create(_conflicts.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (name)
                {
                    case "MeshName":
                        MeshName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "Version":
                        Version = value.Value?.ToString() ?? "1.0";
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

        private async Task ValidateBlendShapeAsync(
            BlendShape blendShape,
            List<string> errors,
            List<string> warnings,
            List<BlendShapeConflict> conflicts,
            CancellationToken cancellationToken)
        {
            // Validate blend shape name
            if (string.IsNullOrWhiteSpace(blendShape.Name))
            {
                var conflict = new BlendShapeConflict
                {
                    ConflictType = BlendShapeConflictType.MissingVertexData,
                    Description = "Blend shape name cannot be empty"
                };
                conflicts.Add(conflict);
                errors.Add(conflict.Description);
            }

            // Validate weight range
            if (blendShape.MinWeight > blendShape.MaxWeight)
            {
                var conflict = new BlendShapeConflict
                {
                    ConflictType = BlendShapeConflictType.InvalidWeightRange,
                    BlendShapeName = blendShape.Name,
                    Description = $"Blend shape '{blendShape.Name}' has invalid weight range: min ({blendShape.MinWeight}) > max ({blendShape.MaxWeight})"
                };
                conflicts.Add(conflict);
                errors.Add(conflict.Description);
            }

            // Validate vertex indices
            foreach (var vertexIndex in blendShape.VertexDeltas.Keys)
            {
                if (vertexIndex < 0 || vertexIndex >= VertexCount)
                {
                    var conflict = new BlendShapeConflict
                    {
                        ConflictType = BlendShapeConflictType.VertexIndexOutOfRange,
                        BlendShapeName = blendShape.Name,
                        Description = $"Blend shape '{blendShape.Name}' has vertex index {vertexIndex} out of range (0-{VertexCount - 1})"
                    };
                    conflicts.Add(conflict);
                    errors.Add(conflict.Description);
                }
            }

            // Check for empty vertex deltas
            if (blendShape.VertexDeltas.Count == 0)
            {
                warnings.Add($"Blend shape '{blendShape.Name}' has no vertex deltas");
            }

            await Task.CompletedTask;
        }

        private void ValidateBlendShapeGroup(
            BlendShapeGroup group,
            List<string> errors,
            List<string> warnings,
            List<BlendShapeConflict> conflicts)
        {
            // Validate group name
            if (string.IsNullOrWhiteSpace(group.Name))
            {
                errors.Add("Blend shape group name cannot be empty");
            }

            // Validate blend shape references
            foreach (var blendShapeName in group.BlendShapeNames)
            {
                if (!_blendShapes.ContainsKey(blendShapeName))
                {
                    warnings.Add($"Blend shape group '{group.Name}' references non-existent blend shape '{blendShapeName}'");
                }
            }

            // Validate mutual exclusivity constraints
            if (group.IsMutuallyExclusive && group.BlendShapeNames.Count > 1)
            {
                var conflict = new BlendShapeConflict
                {
                    ConflictType = BlendShapeConflictType.GroupConstraintViolation,
                    Description = $"Mutually exclusive group '{group.Name}' contains multiple blend shapes but should only allow one active at a time"
                };
                conflicts.Add(conflict);
                warnings.Add(conflict.Description);
            }
        }

        private async Task<IReadOnlyList<Vector2>> CalculateBlendedTextureCoordinatesAsync(
            IDictionary<string, float> blendWeights,
            CancellationToken cancellationToken)
        {
            var blendedTexCoords = new Vector2[_baseTextureCoordinates.Count];

            // Start with base texture coordinates
            for (int i = 0; i < _baseTextureCoordinates.Count; i++)
            {
                blendedTexCoords[i] = _baseTextureCoordinates[i];
            }

            // Apply blend shapes
            foreach (var weightEntry in blendWeights)
            {
                if (_blendShapes.TryGetValue(weightEntry.Key, out var blendShape))
                {
                    var weight = Math.Clamp(weightEntry.Value, blendShape.MinWeight, blendShape.MaxWeight);

                    foreach (var delta in blendShape.TextureCoordinateDeltas)
                    {
                        if (delta.Key < blendedTexCoords.Length)
                        {
                            blendedTexCoords[delta.Key] += delta.Value * weight;
                        }
                    }
                }
            }

            await Task.CompletedTask;
            return blendedTexCoords;
        }

        private byte[] SerializeToBytes()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

            // Write header
            writer.Write(MeshName);
            writer.Write(Version);

            // Write base vertices
            writer.Write(_baseVertices.Count);
            foreach (var vertex in _baseVertices)
            {
                writer.Write(vertex.X);
                writer.Write(vertex.Y);
                writer.Write(vertex.Z);
            }

            // Write base normals
            writer.Write(_baseNormals.Count);
            foreach (var normal in _baseNormals)
            {
                writer.Write(normal.X);
                writer.Write(normal.Y);
                writer.Write(normal.Z);
            }

            // Write base texture coordinates
            writer.Write(_baseTextureCoordinates.Count);
            foreach (var texCoord in _baseTextureCoordinates)
            {
                writer.Write(texCoord.X);
                writer.Write(texCoord.Y);
            }

            // Write blend shapes
            writer.Write(_blendShapes.Count);
            foreach (var blendShape in _blendShapes.Values)
            {
                WriteBlendShape(writer, blendShape);
            }

            // Write blend shape groups
            writer.Write(_blendShapeGroups.Count);
            foreach (var group in _blendShapeGroups.Values)
            {
                WriteBlendShapeGroup(writer, group);
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

        private static void WriteBlendShape(BinaryWriter writer, BlendShape blendShape)
        {
            writer.Write(blendShape.Name);
            writer.Write(blendShape.Description);
            writer.Write(blendShape.DefaultWeight);
            writer.Write(blendShape.MinWeight);
            writer.Write(blendShape.MaxWeight);
            writer.Write(blendShape.Group);

            // Write vertex deltas
            writer.Write(blendShape.VertexDeltas.Count);
            foreach (var delta in blendShape.VertexDeltas)
            {
                writer.Write(delta.Key);
                writer.Write(delta.Value.X);
                writer.Write(delta.Value.Y);
                writer.Write(delta.Value.Z);
            }

            // Write normal deltas
            writer.Write(blendShape.NormalDeltas.Count);
            foreach (var delta in blendShape.NormalDeltas)
            {
                writer.Write(delta.Key);
                writer.Write(delta.Value.X);
                writer.Write(delta.Value.Y);
                writer.Write(delta.Value.Z);
            }

            // Write texture coordinate deltas
            writer.Write(blendShape.TextureCoordinateDeltas.Count);
            foreach (var delta in blendShape.TextureCoordinateDeltas)
            {
                writer.Write(delta.Key);
                writer.Write(delta.Value.X);
                writer.Write(delta.Value.Y);
            }
        }

        private static void WriteBlendShapeGroup(BinaryWriter writer, BlendShapeGroup group)
        {
            writer.Write(group.Name);
            writer.Write(group.Description);
            writer.Write(group.IsMutuallyExclusive);
            writer.Write(group.MaxTotalWeight);

            writer.Write(group.BlendShapeNames.Count);
            foreach (var name in group.BlendShapeNames)
            {
                writer.Write(name);
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
