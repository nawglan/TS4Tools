using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Interface for name mapping resources that provide bidirectional string-to-ID mapping.
    /// NameMapResource enables translation between human-readable names and numeric identifiers.
    /// </summary>
    public interface INameMapResource : IResource
    {
        /// <summary>
        /// Gets the name map identifier.
        /// </summary>
        string NameMapId { get; }

        /// <summary>
        /// Gets the name map version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the name map category/namespace.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets whether the name map is case-sensitive.
        /// </summary>
        bool IsCaseSensitive { get; }

        /// <summary>
        /// Gets whether this name map has been validated.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets the name-to-ID mappings.
        /// </summary>
        IReadOnlyDictionary<string, uint> NameToIdMappings { get; }

        /// <summary>
        /// Gets the ID-to-name mappings.
        /// </summary>
        IReadOnlyDictionary<uint, string> IdToNameMappings { get; }

        /// <summary>
        /// Gets name map metadata.
        /// </summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets name mapping conflicts (multiple names for same ID or multiple IDs for same name).
        /// </summary>
        IReadOnlyCollection<NameMapConflict> Conflicts { get; }

        /// <summary>
        /// Validates the name map for consistency and conflicts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with errors, warnings, and information.</returns>
        Task<NameMapValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the ID for a given name.
        /// </summary>
        /// <param name="name">Name to lookup.</param>
        /// <returns>ID if found, null otherwise.</returns>
        uint? GetId(string name);

        /// <summary>
        /// Gets the name for a given ID.
        /// </summary>
        /// <param name="id">ID to lookup.</param>
        /// <returns>Name if found, null otherwise.</returns>
        string? GetName(uint id);

        /// <summary>
        /// Checks if a name exists in the mapping.
        /// </summary>
        /// <param name="name">Name to check.</param>
        /// <returns>True if name exists, false otherwise.</returns>
        bool HasName(string name);

        /// <summary>
        /// Checks if an ID exists in the mapping.
        /// </summary>
        /// <param name="id">ID to check.</param>
        /// <returns>True if ID exists, false otherwise.</returns>
        bool HasId(uint id);

        /// <summary>
        /// Adds a name-to-ID mapping.
        /// </summary>
        /// <param name="name">Name to map.</param>
        /// <param name="id">ID to map to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if mapping was added successfully, false if conflict exists.</returns>
        Task<bool> AddMappingAsync(string name, uint id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a name-to-ID mapping.
        /// </summary>
        /// <param name="name">Name to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if mapping was removed, false if not found.</returns>
        Task<bool> RemoveMappingByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a name-to-ID mapping by ID.
        /// </summary>
        /// <param name="id">ID to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if mapping was removed, false if not found.</returns>
        Task<bool> RemoveMappingByIdAsync(uint id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a name mapping.
        /// </summary>
        /// <param name="oldName">Current name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if update was successful, false if conflict or name not found.</returns>
        Task<bool> UpdateNameAsync(string oldName, string newName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an ID mapping.
        /// </summary>
        /// <param name="name">Name to update.</param>
        /// <param name="newId">New ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if update was successful, false if conflict or name not found.</returns>
        Task<bool> UpdateIdAsync(string name, uint newId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple mappings in a batch operation.
        /// </summary>
        /// <param name="mappings">Mappings to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Batch operation result with success/failure details.</returns>
        Task<NameMapBatchResult> AddMappingsBatchAsync(
            IDictionary<string, uint> mappings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates name map metadata.
        /// </summary>
        /// <param name="metadata">Metadata to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateMetadataAsync(IDictionary<string, object> metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all mappings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets names matching a pattern.
        /// </summary>
        /// <param name="pattern">Pattern to match (supports wildcards).</param>
        /// <returns>Collection of matching names.</returns>
        IEnumerable<string> FindNamesByPattern(string pattern);

        /// <summary>
        /// Gets IDs in a specified range.
        /// </summary>
        /// <param name="minId">Minimum ID (inclusive).</param>
        /// <param name="maxId">Maximum ID (inclusive).</param>
        /// <returns>Collection of IDs in range.</returns>
        IEnumerable<uint> FindIdsInRange(uint minId, uint maxId);
    }

    /// <summary>
    /// Represents a name mapping conflict.
    /// </summary>
    public class NameMapConflict
    {
        /// <summary>
        /// Gets or sets the conflict type.
        /// </summary>
        public NameMapConflictType ConflictType { get; set; }

        /// <summary>
        /// Gets or sets the conflicting name.
        /// </summary>
        public string ConflictingName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the conflicting ID.
        /// </summary>
        public uint ConflictingId { get; set; }

        /// <summary>
        /// Gets or sets the existing name.
        /// </summary>
        public string? ExistingName { get; set; }

        /// <summary>
        /// Gets or sets the existing ID.
        /// </summary>
        public uint? ExistingId { get; set; }

        /// <summary>
        /// Gets or sets the conflict description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of name mapping conflicts.
    /// </summary>
    public enum NameMapConflictType
    {
        /// <summary>
        /// Multiple names map to the same ID.
        /// </summary>
        DuplicateId,

        /// <summary>
        /// Same name maps to multiple IDs.
        /// </summary>
        DuplicateName,

        /// <summary>
        /// Case sensitivity conflict.
        /// </summary>
        CaseSensitivityConflict,

        /// <summary>
        /// Invalid name format.
        /// </summary>
        InvalidNameFormat,

        /// <summary>
        /// Invalid ID range.
        /// </summary>
        InvalidIdRange
    }

    /// <summary>
    /// Represents the result of name map validation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class NameMapValidationResult
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
        /// Gets detected conflicts.
        /// </summary>
        public List<NameMapConflict> DetectedConflicts { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>Successful validation result.</returns>
        public static NameMapValidationResult Success()
        {
            return new NameMapValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">Validation errors.</param>
        /// <returns>Failed validation result.</returns>
        public static NameMapValidationResult Failure(params string[] errors)
        {
            return new NameMapValidationResult
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
        public static NameMapValidationResult WithWarnings(params string[] warnings)
        {
            return new NameMapValidationResult
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
        public static NameMapValidationResult WithInformation(params string[] information)
        {
            return new NameMapValidationResult
            {
                IsValid = true,
                Information = information.ToList()
            };
        }

        /// <summary>
        /// Creates a validation result with conflicts detected.
        /// </summary>
        /// <param name="conflicts">Detected conflicts.</param>
        /// <returns>Validation result with conflicts.</returns>
        public static NameMapValidationResult WithConflicts(params NameMapConflict[] conflicts)
        {
            var hasErrors = conflicts.Any(c => c.ConflictType == NameMapConflictType.DuplicateId ||
                                              c.ConflictType == NameMapConflictType.DuplicateName);

            return new NameMapValidationResult
            {
                IsValid = !hasErrors,
                DetectedConflicts = conflicts.ToList()
            };
        }
    }

    /// <summary>
    /// Represents the result of a batch name mapping operation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class NameMapBatchResult
    {
        /// <summary>
        /// Gets whether the batch operation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the number of mappings successfully added.
        /// </summary>
        public int SuccessCount { get; private set; }

        /// <summary>
        /// Gets the number of mappings that failed to be added.
        /// </summary>
        public int FailureCount { get; private set; }

        /// <summary>
        /// Gets the mappings that failed to be added.
        /// </summary>
        public List<NameMapBatchFailure> Failures { get; set; } = new();

        /// <summary>
        /// Gets the mappings that were successfully added.
        /// </summary>
        public Dictionary<string, uint> SuccessfulMappings { get; set; } = new();

        /// <summary>
        /// Creates a successful batch result.
        /// </summary>
        /// <param name="successfulMappings">Successfully added mappings.</param>
        /// <returns>Successful batch result.</returns>
        public static NameMapBatchResult Success(IDictionary<string, uint> successfulMappings)
        {
            return new NameMapBatchResult
            {
                IsSuccess = true,
                SuccessCount = successfulMappings.Count,
                FailureCount = 0,
                SuccessfulMappings = new Dictionary<string, uint>(successfulMappings)
            };
        }

        /// <summary>
        /// Creates a partial batch result with some failures.
        /// </summary>
        /// <param name="successfulMappings">Successfully added mappings.</param>
        /// <param name="failures">Failed mappings.</param>
        /// <returns>Partial batch result.</returns>
        public static NameMapBatchResult Partial(
            IDictionary<string, uint> successfulMappings,
            IEnumerable<NameMapBatchFailure> failures)
        {
            var failureList = failures.ToList();
            return new NameMapBatchResult
            {
                IsSuccess = failureList.Count == 0,
                SuccessCount = successfulMappings.Count,
                FailureCount = failureList.Count,
                SuccessfulMappings = new Dictionary<string, uint>(successfulMappings),
                Failures = failureList
            };
        }

        /// <summary>
        /// Creates a failed batch result.
        /// </summary>
        /// <param name="failures">Failed mappings.</param>
        /// <returns>Failed batch result.</returns>
        public static NameMapBatchResult Failure(params NameMapBatchFailure[] failures)
        {
            return new NameMapBatchResult
            {
                IsSuccess = false,
                SuccessCount = 0,
                FailureCount = failures.Length,
                Failures = failures.ToList()
            };
        }
    }

    /// <summary>
    /// Represents a failure in a batch name mapping operation.
    /// </summary>
    public class NameMapBatchFailure
    {
        /// <summary>
        /// Gets or sets the name that failed to be mapped.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID that failed to be mapped.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Gets or sets the reason for failure.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the failure type.
        /// </summary>
        public NameMapBatchFailureType FailureType { get; set; }
    }

    /// <summary>
    /// Types of batch operation failures.
    /// </summary>
    public enum NameMapBatchFailureType
    {
        /// <summary>
        /// Name already exists.
        /// </summary>
        NameExists,

        /// <summary>
        /// ID already exists.
        /// </summary>
        IdExists,

        /// <summary>
        /// Invalid name format.
        /// </summary>
        InvalidName,

        /// <summary>
        /// Invalid ID range.
        /// </summary>
        InvalidId,

        /// <summary>
        /// General conflict.
        /// </summary>
        Conflict
    }
}
