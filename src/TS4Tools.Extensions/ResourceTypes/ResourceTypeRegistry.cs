using System.Collections.Concurrent;

namespace TS4Tools.Extensions.ResourceTypes;

/// <summary>
/// Default implementation of <see cref="IResourceTypeRegistry"/> that provides lookup services
/// for resource types, their tags, and file extensions.
/// </summary>
public sealed partial class ResourceTypeRegistry : IResourceTypeRegistry
{
    private readonly ConcurrentDictionary<uint, ResourceTypeInfo> _registry = new();
    private readonly ILogger<ResourceTypeRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceTypeRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ResourceTypeRegistry(ILogger<ResourceTypeRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeDefaultTypes();
    }

    /// <inheritdoc />
    public string? GetExtension(uint resourceType)
    {
        return _registry.TryGetValue(resourceType, out var info) ? info.Extension : null;
    }

    /// <inheritdoc />
    public string? GetTag(uint resourceType)
    {
        return _registry.TryGetValue(resourceType, out var info) ? info.Tag : null;
    }

    /// <inheritdoc />
    public IEnumerable<uint> GetSupportedTypes()
    {
        return _registry.Keys;
    }

    /// <inheritdoc />
    public bool IsSupported(uint resourceType)
    {
        return _registry.ContainsKey(resourceType);
    }

    /// <inheritdoc />
    public void RegisterType(uint resourceType, string tag, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var info = new ResourceTypeInfo(tag, extension);
        _registry.AddOrUpdate(resourceType, info, (_, _) => info);

        LogResourceTypeRegistered(_logger, resourceType, tag, extension);
    }

    /// <summary>
    /// LoggerMessage delegate for resource type registration.
    /// </summary>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Registered resource type 0x{ResourceType:X8} with tag '{Tag}' and extension '{Extension}'")]
    private static partial void LogResourceTypeRegistered(ILogger logger, uint resourceType, string tag, string extension);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Initialized resource type registry with {Count} default types")]
    private static partial void LogRegistryInitialized(ILogger logger, int count);

    /// <summary>
    /// Initializes the registry with the most common Sims 4 resource types.
    /// This provides a basic set of known types to get started.
    /// </summary>
    private void InitializeDefaultTypes()
    {
        // Core resource types commonly found in Sims 4 packages
        var defaultTypes = new Dictionary<uint, (string Tag, string Extension)>
        {
            // Images and Textures
            { 0x00B2D882, ("DDS", ".dds") },
            { 0x2E75C764, ("THUM", ".thum") },
            { 0x2E75C765, ("IMG", ".img") },

            // String Tables
            { 0x220557DA, ("STBL", ".stbl") },

            // 3D Models and Meshes
            { 0x015A1849, ("GEOM", ".geom") },
            { 0x01661233, ("MODL", ".modl") },
            { 0x736884F1, ("MLOD", ".mlod") },

            // Catalog Resources
            { 0x319E4F1D, ("CASP", ".casp") },
            { 0x034AEECB, ("OBJD", ".objd") },
            { 0x0355E0A6, ("CAS", ".cas") },

            // Animation and Rigs
            { 0x6B20C4F3, ("CLIP", ".clip") },
            { 0x8EAF13DE, ("RIG", ".rig") },

            // Audio
            { 0x18D878AF, ("SNR", ".snr") },

            // Scripts and Tuning
            { 0x6017E896, ("SIMO", ".simo") },
            { 0x62E94D38, ("BHV", ".bhv") },

            // UI and Layout
            { 0x0166038C, ("LAYO", ".layo") },

            // Default/Unknown
            { 0x00000000, ("UNKN", ".dat") }
        };

        foreach (var (resourceType, (tag, extension)) in defaultTypes)
        {
            RegisterType(resourceType, tag, extension);
        }

        LogRegistryInitialized(_logger, defaultTypes.Count);
    }

    /// <summary>
    /// Represents information about a resource type.
    /// </summary>
    /// <param name="Tag">The human-readable tag/name for the resource type.</param>
    /// <param name="Extension">The file extension (including the dot) for the resource type.</param>
    private sealed record ResourceTypeInfo(string Tag, string Extension);
}
