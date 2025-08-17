using TS4Tools.Extensions.ResourceTypes;
using TS4Tools.Extensions.Utilities;

namespace TS4Tools.Extensions;

/// <summary>
/// Provides extension methods for registering TS4Tools.Extensions services
/// with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TS4Tools.Extensions services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddTS4ToolsExtensions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register core extension services
        services.AddSingleton<IResourceTypeRegistry, ResourceTypeRegistry>();
        services.AddScoped<IFileNameService, FileNameService>();

        return services;
    }

    /// <summary>
    /// Adds TS4Tools.Extensions services with custom configuration to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">A delegate to configure the extension options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddTS4ToolsExtensions(
        this IServiceCollection services,
        Action<ExtensionOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options
        services.Configure(configureOptions);

        // Add services
        return services.AddTS4ToolsExtensions();
    }
}

/// <summary>
/// Configuration options for TS4Tools.Extensions services.
/// </summary>
public sealed class ExtensionOptions
{
    private readonly Dictionary<string, (string Tag, string Extension)> _customResourceTypes = new();

    /// <summary>
    /// Gets or sets a value indicating whether to register additional resource types from external sources.
    /// </summary>
    public bool EnableExtendedResourceTypes { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum filename length for generated filenames.
    /// </summary>
    [Range(50, 255)]
    public int MaxFileNameLength { get; set; } = 240;

    /// <summary>
    /// Gets or sets a value indicating whether to use sanitized filenames by default.
    /// </summary>
    public bool UseSanitizedFilenames { get; set; } = true;

    /// <summary>
    /// Gets custom resource type mappings.
    /// Key is the resource type (as hex string), value is a tuple of (tag, extension).
    /// </summary>
    public IReadOnlyDictionary<string, (string Tag, string Extension)> CustomResourceTypes => _customResourceTypes;

    /// <summary>
    /// Adds a custom resource type mapping.
    /// </summary>
    /// <param name="resourceTypeHex">The resource type as a hex string.</param>
    /// <param name="tag">The resource tag.</param>
    /// <param name="extension">The file extension.</param>
    public void AddCustomResourceType(string resourceTypeHex, string tag, string extension)
    {
        _customResourceTypes[resourceTypeHex] = (tag, extension);
    }
}
