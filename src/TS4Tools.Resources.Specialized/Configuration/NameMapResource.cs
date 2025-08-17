using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Implementation of name mapping resources that provide bidirectional string-to-ID mapping.
    /// </summary>
    public class NameMapResource : INameMapResource, IDisposable
    {
        private readonly Dictionary<string, uint> _nameToId = new();
        private readonly Dictionary<uint, string> _idToName = new();
        private readonly Dictionary<string, object> _metadata = new();
        private readonly List<NameMapConflict> _conflicts = new();
        private readonly StringComparer _nameComparer;
        private bool _isValidated;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the NameMapResource class.
        /// </summary>
        public NameMapResource() : this("default", "1.0", "general", false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the NameMapResource class with specific settings.
        /// </summary>
        /// <param name="nameMapId">Name map identifier.</param>
        /// <param name="version">Version.</param>
        /// <param name="category">Category.</param>
        /// <param name="isCaseSensitive">Whether names are case-sensitive.</param>
        public NameMapResource(string nameMapId, string version, string category, bool isCaseSensitive)
        {
            NameMapId = nameMapId ?? string.Empty;
            Version = version ?? "1.0";
            Category = category ?? "general";
            IsCaseSensitive = isCaseSensitive;
            _nameComparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            Stream = new MemoryStream();
        }

        #region INameMapResource Implementation

        /// <inheritdoc />
        public string NameMapId { get; private set; }

        /// <inheritdoc />
        public string Version { get; private set; }

        /// <inheritdoc />
        public string Category { get; private set; }

        /// <inheritdoc />
        public bool IsCaseSensitive { get; private set; }

        /// <inheritdoc />
        public bool IsValidated => _isValidated;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, uint> NameToIdMappings => _nameToId;

        /// <inheritdoc />
        public IReadOnlyDictionary<uint, string> IdToNameMappings => _idToName;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        /// <inheritdoc />
        public IReadOnlyCollection<NameMapConflict> Conflicts => _conflicts;

        /// <inheritdoc />
        public async Task<NameMapValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            var errors = new List<string>();
            var warnings = new List<string>();
            var information = new List<string>();
            var conflicts = new List<NameMapConflict>();

            // Clear existing conflicts
            _conflicts.Clear();

            // Validate name map ID
            if (string.IsNullOrWhiteSpace(NameMapId))
            {
                errors.Add("Name map ID cannot be empty");
            }

            // Check for duplicate names (case sensitivity)
            var nameGroups = _nameToId.Keys.GroupBy(name => name, _nameComparer);
            foreach (var group in nameGroups.Where(g => g.Count() > 1))
            {
                var duplicateNames = group.ToList();
                var conflict = new NameMapConflict
                {
                    ConflictType = NameMapConflictType.DuplicateName,
                    ConflictingName = duplicateNames.First(),
                    Description = $"Duplicate names found: {string.Join(", ", duplicateNames)}"
                };
                conflicts.Add(conflict);
                _conflicts.Add(conflict);
                errors.Add(conflict.Description);
            }

            // Check for duplicate IDs
            var idGroups = _idToName.Keys.GroupBy(id => id);
            foreach (var group in idGroups.Where(g => g.Count() > 1))
            {
                var duplicateIds = group.ToList();
                var conflict = new NameMapConflict
                {
                    ConflictType = NameMapConflictType.DuplicateId,
                    ConflictingId = duplicateIds.First(),
                    Description = $"Duplicate IDs found: {string.Join(", ", duplicateIds)}"
                };
                conflicts.Add(conflict);
                _conflicts.Add(conflict);
                errors.Add(conflict.Description);
            }

            // Check for bidirectional consistency
            foreach (var nameMapping in _nameToId)
            {
                if (_idToName.TryGetValue(nameMapping.Value, out var mappedName))
                {
                    if (!_nameComparer.Equals(nameMapping.Key, mappedName))
                    {
                        var conflict = new NameMapConflict
                        {
                            ConflictType = NameMapConflictType.DuplicateId,
                            ConflictingName = nameMapping.Key,
                            ConflictingId = nameMapping.Value,
                            ExistingName = mappedName,
                            Description = $"Bidirectional mapping inconsistency: Name '{nameMapping.Key}' maps to ID {nameMapping.Value}, but ID {nameMapping.Value} maps to name '{mappedName}'"
                        };
                        conflicts.Add(conflict);
                        _conflicts.Add(conflict);
                        errors.Add(conflict.Description);
                    }
                }
                else
                {
                    warnings.Add($"Name '{nameMapping.Key}' maps to ID {nameMapping.Value}, but reverse mapping is missing");
                }
            }

            // Validate name formats
            foreach (var name in _nameToId.Keys)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    var conflict = new NameMapConflict
                    {
                        ConflictType = NameMapConflictType.InvalidNameFormat,
                        ConflictingName = name,
                        Description = "Name cannot be null or whitespace"
                    };
                    conflicts.Add(conflict);
                    _conflicts.Add(conflict);
                    errors.Add(conflict.Description);
                }
            }

            // Generate statistics
            information.Add($"Total mappings: {_nameToId.Count}");
            information.Add($"Case sensitive: {IsCaseSensitive}");
            information.Add($"ID range: {(_idToName.Keys.Any() ? _idToName.Keys.Min() : 0)} - {(_idToName.Keys.Any() ? _idToName.Keys.Max() : 0)}");

            _isValidated = errors.Count == 0;

            await Task.CompletedTask;

            if (errors.Count > 0)
            {
                var result = NameMapValidationResult.Failure(errors.ToArray());
                result.Warnings = warnings;
                result.Information = information;
                result.DetectedConflicts = conflicts;
                return result;
            }

            if (warnings.Count > 0)
            {
                var result = NameMapValidationResult.WithWarnings(warnings.ToArray());
                result.Information = information;
                result.DetectedConflicts = conflicts;
                return result;
            }

            return information.Count > 0
                ? NameMapValidationResult.WithInformation(information.ToArray())
                : NameMapValidationResult.Success();
        }

        /// <inheritdoc />
        public uint? GetId(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _nameToId.TryGetValue(name, out var id) ? id : null;
        }

        /// <inheritdoc />
        public string? GetName(uint id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            return _idToName.TryGetValue(id, out var name) ? name : null;
        }

        /// <inheritdoc />
        public bool HasName(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return _nameToId.ContainsKey(name);
        }

        /// <inheritdoc />
        public bool HasId(uint id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            return _idToName.ContainsKey(id);
        }

        /// <inheritdoc />
        public async Task<bool> AddMappingAsync(string name, uint id, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // Check for conflicts
            if (_nameToId.ContainsKey(name) || _idToName.ContainsKey(id))
            {
                return false;
            }

            _nameToId[name] = id;
            _idToName[id] = name;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveMappingByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (_nameToId.TryGetValue(name, out var id))
            {
                _nameToId.Remove(name);
                _idToName.Remove(id);
                _isValidated = false; // Invalidate validation
                OnResourceChanged();

                await Task.CompletedTask;
                return true;
            }

            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveMappingByIdAsync(uint id, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            if (_idToName.TryGetValue(id, out var name))
            {
                _nameToId.Remove(name);
                _idToName.Remove(id);
                _isValidated = false; // Invalidate validation
                OnResourceChanged();

                await Task.CompletedTask;
                return true;
            }

            await Task.CompletedTask;
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateNameAsync(string oldName, string newName, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(oldName);
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);

            if (!_nameToId.TryGetValue(oldName, out var id))
            {
                return false;
            }

            if (_nameToId.ContainsKey(newName))
            {
                return false; // Conflict
            }

            _nameToId.Remove(oldName);
            _nameToId[newName] = id;
            _idToName[id] = newName;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateIdAsync(string name, uint newId, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!_nameToId.TryGetValue(name, out var oldId))
            {
                return false;
            }

            if (_idToName.ContainsKey(newId))
            {
                return false; // Conflict
            }

            _idToName.Remove(oldId);
            _nameToId[name] = newId;
            _idToName[newId] = name;
            _isValidated = false; // Invalidate validation
            OnResourceChanged();

            await Task.CompletedTask;
            return true;
        }

        /// <inheritdoc />
        public async Task<NameMapBatchResult> AddMappingsBatchAsync(
            IDictionary<string, uint> mappings,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentNullException.ThrowIfNull(mappings);

            var successful = new Dictionary<string, uint>();
            var failures = new List<NameMapBatchFailure>();

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Key))
                {
                    failures.Add(new NameMapBatchFailure
                    {
                        Name = mapping.Key,
                        Id = mapping.Value,
                        Reason = "Name cannot be null or whitespace",
                        FailureType = NameMapBatchFailureType.InvalidName
                    });
                    continue;
                }

                if (_nameToId.ContainsKey(mapping.Key))
                {
                    failures.Add(new NameMapBatchFailure
                    {
                        Name = mapping.Key,
                        Id = mapping.Value,
                        Reason = "Name already exists",
                        FailureType = NameMapBatchFailureType.NameExists
                    });
                    continue;
                }

                if (_idToName.ContainsKey(mapping.Value))
                {
                    failures.Add(new NameMapBatchFailure
                    {
                        Name = mapping.Key,
                        Id = mapping.Value,
                        Reason = "ID already exists",
                        FailureType = NameMapBatchFailureType.IdExists
                    });
                    continue;
                }

                _nameToId[mapping.Key] = mapping.Value;
                _idToName[mapping.Value] = mapping.Key;
                successful[mapping.Key] = mapping.Value;
            }

            if (successful.Count > 0)
            {
                _isValidated = false; // Invalidate validation
                OnResourceChanged();
            }

            await Task.CompletedTask;

            return failures.Count == 0
                ? NameMapBatchResult.Success(successful)
                : NameMapBatchResult.Partial(successful, failures);
        }

        /// <inheritdoc />
        public async Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
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
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            _nameToId.Clear();
            _idToName.Clear();
            _conflicts.Clear();
            _isValidated = false;
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public IEnumerable<string> FindNamesByPattern(string pattern)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            var regex = new Regex(regexPattern, IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

            return _nameToId.Keys.Where(name => regex.IsMatch(name));
        }

        /// <inheritdoc />
        public IEnumerable<uint> FindIdsInRange(uint minId, uint maxId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NameMapResource));

            return _idToName.Keys.Where(id => id >= minId && id <= maxId);
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
            "NameMapId", "Version", "Category", "IsCaseSensitive",
            "IsValidated", "MappingCount", "ConflictCount"
        };

        /// <inheritdoc />
        TypedValue IContentFields.this[int index]
        {
            get => index switch
            {
                0 => TypedValue.Create(NameMapId, "string"),
                1 => TypedValue.Create(Version, "string"),
                2 => TypedValue.Create(Category, "string"),
                3 => TypedValue.Create(IsCaseSensitive, "bool"),
                4 => TypedValue.Create(IsValidated, "bool"),
                5 => TypedValue.Create(_nameToId.Count, "int"),
                6 => TypedValue.Create(_conflicts.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (index)
                {
                    case 0:
                        NameMapId = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 1:
                        Version = value.Value?.ToString() ?? "1.0";
                        break;
                    case 2:
                        Category = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 3:
                        IsCaseSensitive = value.Value is bool caseSensitive && caseSensitive;
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
                "NameMapId" => TypedValue.Create(NameMapId, "string"),
                "Version" => TypedValue.Create(Version, "string"),
                "Category" => TypedValue.Create(Category, "string"),
                "IsCaseSensitive" => TypedValue.Create(IsCaseSensitive, "bool"),
                "IsValidated" => TypedValue.Create(IsValidated, "bool"),
                "MappingCount" => TypedValue.Create(_nameToId.Count, "int"),
                "ConflictCount" => TypedValue.Create(_conflicts.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (name)
                {
                    case "NameMapId":
                        NameMapId = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "Version":
                        Version = value.Value?.ToString() ?? "1.0";
                        break;
                    case "Category":
                        Category = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "IsCaseSensitive":
                        IsCaseSensitive = value.Value is bool caseSensitive && caseSensitive;
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

        private byte[] SerializeToBytes()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

            // Write header
            writer.Write(NameMapId);
            writer.Write(Version);
            writer.Write(Category);
            writer.Write(IsCaseSensitive);

            // Write mappings
            writer.Write(_nameToId.Count);
            foreach (var mapping in _nameToId)
            {
                writer.Write(mapping.Key);
                writer.Write(mapping.Value);
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
                case uint ui:
                    writer.Write((byte)3);
                    writer.Write(ui);
                    break;
                case bool b:
                    writer.Write((byte)4);
                    writer.Write(b);
                    break;
                case float f:
                    writer.Write((byte)5);
                    writer.Write(f);
                    break;
                case double d:
                    writer.Write((byte)6);
                    writer.Write(d);
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
