using System.Reflection;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Provides plugin metadata through attributes.
/// Allows plugin authors to declare rich metadata about their plugins.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class PluginInfoAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the PluginInfoAttribute.
    /// </summary>
    /// <param name="name">The human-readable name of the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    public PluginInfoAttribute(string name, string version)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// The human-readable name of the plugin.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The version of the plugin as a string.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// A brief description of what the plugin does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The author or organization that created the plugin.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The website or URL for the plugin.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// License information for the plugin.
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// The minimum version of TS4Tools required for this plugin.
    /// </summary>
    public string? MinimumTS4ToolsVersion { get; set; }

    /// <summary>
    /// The maximum version of TS4Tools this plugin supports.
    /// </summary>
    public string? MaximumTS4ToolsVersion { get; set; }

    /// <summary>
    /// Comma-separated list of resource types this plugin handles.
    /// Example: "0x12345678,0x87654321"
    /// </summary>
    public string? SupportedResourceTypes { get; set; }

    /// <summary>
    /// Tags or categories for the plugin (comma-separated).
    /// Example: "imaging,cas,mesh"
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Whether this plugin is considered stable for production use.
    /// </summary>
    public bool IsStable { get; set; } = true;

    /// <summary>
    /// Whether this plugin is experimental and may change significantly.
    /// </summary>
    public bool IsExperimental { get; set; } = false;

    /// <summary>
    /// Gets the parsed version as a System.Version object.
    /// </summary>
    /// <returns>The version as a Version object, or null if parsing fails.</returns>
    public Version? GetParsedVersion()
    {
        return System.Version.TryParse(Version, out var parsedVersion) ? parsedVersion : null;
    }

    /// <summary>
    /// Gets the supported resource types as a collection.
    /// </summary>
    /// <returns>Collection of resource type strings.</returns>
    public IReadOnlyList<string> GetSupportedResourceTypes()
    {
        if (string.IsNullOrWhiteSpace(SupportedResourceTypes))
            return Array.Empty<string>();

        return SupportedResourceTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();
    }

    /// <summary>
    /// Gets the tags as a collection.
    /// </summary>
    /// <returns>Collection of tag strings.</returns>
    public IReadOnlyList<string> GetTags()
    {
        if (string.IsNullOrWhiteSpace(Tags))
            return Array.Empty<string>();

        return Tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();
    }

    /// <summary>
    /// Extracts plugin info from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to extract plugin info from.</param>
    /// <returns>The PluginInfoAttribute if found, null otherwise.</returns>
    public static PluginInfoAttribute? FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<PluginInfoAttribute>();
    }
}

/// <summary>
/// Declares a dependency on another plugin.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class PluginDependencyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the PluginDependencyAttribute.
    /// </summary>
    /// <param name="pluginName">The name of the required plugin.</param>
    /// <param name="minimumVersion">The minimum version of the required plugin.</param>
    public PluginDependencyAttribute(string pluginName, string minimumVersion)
    {
        PluginName = pluginName ?? throw new ArgumentNullException(nameof(pluginName));
        MinimumVersion = minimumVersion ?? throw new ArgumentNullException(nameof(minimumVersion));
    }

    /// <summary>
    /// The name of the required plugin.
    /// </summary>
    public string PluginName { get; }

    /// <summary>
    /// The minimum version of the required plugin.
    /// </summary>
    public string MinimumVersion { get; }

    /// <summary>
    /// The maximum version of the required plugin (optional).
    /// </summary>
    public string? MaximumVersion { get; set; }

    /// <summary>
    /// Whether this dependency is optional.
    /// Optional dependencies don't prevent plugin loading if missing.
    /// </summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>
    /// Gets the parsed minimum version as a System.Version object.
    /// </summary>
    /// <returns>The minimum version as a Version object, or null if parsing fails.</returns>
    public Version? GetParsedMinimumVersion()
    {
        return System.Version.TryParse(MinimumVersion, out var parsedVersion) ? parsedVersion : null;
    }

    /// <summary>
    /// Gets the parsed maximum version as a System.Version object.
    /// </summary>
    /// <returns>The maximum version as a Version object, or null if not specified or parsing fails.</returns>
    public Version? GetParsedMaximumVersion()
    {
        return !string.IsNullOrWhiteSpace(MaximumVersion) && 
               System.Version.TryParse(MaximumVersion, out var parsedVersion) 
            ? parsedVersion 
            : null;
    }

    /// <summary>
    /// Checks if a given plugin version satisfies this dependency.
    /// </summary>
    /// <param name="availableVersion">The version of the available plugin.</param>
    /// <returns>True if the version satisfies this dependency; false otherwise.</returns>
    public bool IsSatisfiedBy(Version availableVersion)
    {
        ArgumentNullException.ThrowIfNull(availableVersion);

        var minVersion = GetParsedMinimumVersion();
        if (minVersion != null && availableVersion < minVersion)
            return false;

        var maxVersion = GetParsedMaximumVersion();
        if (maxVersion != null && availableVersion > maxVersion)
            return false;

        return true;
    }

    /// <summary>
    /// Extracts all plugin dependencies from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to extract dependencies from.</param>
    /// <returns>Collection of PluginDependencyAttribute instances.</returns>
    public static IReadOnlyList<PluginDependencyAttribute> FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttributes<PluginDependencyAttribute>().ToArray();
    }
}

/// <summary>
/// Marks a plugin as providing compatibility for legacy resource types.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class PluginResourceCompatibilityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the PluginResourceCompatibilityAttribute.
    /// </summary>
    /// <param name="resourceType">The resource type this plugin provides compatibility for.</param>
    public PluginResourceCompatibilityAttribute(string resourceType)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    }

    /// <summary>
    /// The resource type this plugin provides compatibility for.
    /// </summary>
    public string ResourceType { get; }

    /// <summary>
    /// The version range of the resource format supported.
    /// </summary>
    public string? SupportedVersionRange { get; set; }

    /// <summary>
    /// Whether this plugin replaces the default handler for this resource type.
    /// </summary>
    public bool ReplacesDefaultHandler { get; set; } = false;

    /// <summary>
    /// Priority for this handler when multiple handlers support the same type.
    /// Higher values have higher priority.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Extracts all resource compatibility declarations from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to extract compatibility info from.</param>
    /// <returns>Collection of PluginResourceCompatibilityAttribute instances.</returns>
    public static IReadOnlyList<PluginResourceCompatibilityAttribute> FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttributes<PluginResourceCompatibilityAttribute>().ToArray();
    }
}
