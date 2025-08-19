using System.Reflection;

namespace TS4Tools.WrapperDealer.Plugins;

/// <summary>
/// Simplified plugin information extracted from PluginInfoAttribute.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
public sealed record PluginInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? Website { get; init; }
    public string? License { get; init; }
    public string? MinimumTS4ToolsVersion { get; init; }
    public string? MaximumTS4ToolsVersion { get; init; }
    public IReadOnlyList<string> SupportedResourceTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public bool IsStable { get; init; } = true;
    public bool IsExperimental { get; init; } = false;

    /// <summary>
    /// Creates a PluginInfo from a PluginInfoAttribute.
    /// </summary>
    /// <param name="attribute">The attribute to extract info from.</param>
    /// <returns>A new PluginInfo instance.</returns>
    public static PluginInfo FromAttribute(PluginInfoAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        return new PluginInfo
        {
            Name = attribute.Name,
            Version = attribute.Version,
            Description = attribute.Description,
            Author = attribute.Author,
            Website = attribute.Website,
            License = attribute.License,
            MinimumTS4ToolsVersion = attribute.MinimumTS4ToolsVersion,
            MaximumTS4ToolsVersion = attribute.MaximumTS4ToolsVersion,
            SupportedResourceTypes = attribute.GetSupportedResourceTypes(),
            Tags = attribute.GetTags(),
            IsStable = attribute.IsStable,
            IsExperimental = attribute.IsExperimental
        };
    }
}

/// <summary>
/// Simplified plugin dependency information extracted from PluginDependencyAttribute.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
public sealed record PluginDependency
{
    public required string PluginName { get; init; }
    public required string MinimumVersion { get; init; }
    public string? MaximumVersion { get; init; }
    public bool IsOptional { get; init; } = false;

    /// <summary>
    /// Creates a PluginDependency from a PluginDependencyAttribute.
    /// </summary>
    /// <param name="attribute">The attribute to extract dependency from.</param>
    /// <returns>A new PluginDependency instance.</returns>
    public static PluginDependency FromAttribute(PluginDependencyAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        return new PluginDependency
        {
            PluginName = attribute.PluginName,
            MinimumVersion = attribute.MinimumVersion,
            MaximumVersion = attribute.MaximumVersion,
            IsOptional = attribute.IsOptional
        };
    }

    /// <summary>
    /// Checks if a given plugin version satisfies this dependency.
    /// </summary>
    /// <param name="availableVersion">The version of the available plugin.</param>
    /// <returns>True if the version satisfies this dependency; false otherwise.</returns>
    public bool IsSatisfiedBy(Version availableVersion)
    {
        ArgumentNullException.ThrowIfNull(availableVersion);

        if (System.Version.TryParse(MinimumVersion, out var minVersion) && 
            availableVersion < minVersion)
            return false;

        if (!string.IsNullOrWhiteSpace(MaximumVersion) &&
            System.Version.TryParse(MaximumVersion, out var maxVersion) &&
            availableVersion > maxVersion)
            return false;

        return true;
    }
}

/// <summary>
/// Simplified plugin resource compatibility information extracted from PluginResourceCompatibilityAttribute.
/// Part of Phase 4.20.5 - Enhanced Plugin Metadata and Dependency Management.
/// </summary>
public sealed record PluginResourceCompatibility
{
    public required string ResourceType { get; init; }
    public string? SupportedVersionRange { get; init; }
    public bool ReplacesDefaultHandler { get; init; } = false;
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Creates a PluginResourceCompatibility from a PluginResourceCompatibilityAttribute.
    /// </summary>
    /// <param name="attribute">The attribute to extract compatibility from.</param>
    /// <returns>A new PluginResourceCompatibility instance.</returns>
    public static PluginResourceCompatibility FromAttribute(PluginResourceCompatibilityAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        return new PluginResourceCompatibility
        {
            ResourceType = attribute.ResourceType,
            SupportedVersionRange = attribute.SupportedVersionRange,
            ReplacesDefaultHandler = attribute.ReplacesDefaultHandler,
            Priority = attribute.Priority
        };
    }
}

/// <summary>
/// Represents metadata for a discovered plugin assembly.
/// Contains information needed for plugin validation and compatibility checking.
/// </summary>
public sealed record PluginMetadata
{
    /// <summary>
    /// The full file path to the plugin assembly.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The assembly name of the plugin.
    /// </summary>
    public required string AssemblyName { get; init; }

    /// <summary>
    /// The full name of the assembly including version and culture information.
    /// </summary>
    public required string AssemblyFullName { get; init; }

    /// <summary>
    /// The version of the plugin assembly.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// The target framework the plugin was compiled for.
    /// </summary>
    public string? TargetFramework { get; init; }

    /// <summary>
    /// Whether the plugin assembly is digitally signed.
    /// </summary>
    public bool IsSigned { get; init; }

    /// <summary>
    /// The size of the plugin assembly file in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// The last modified date of the plugin assembly file.
    /// </summary>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// Whether the plugin is compatible with the current TS4Tools version.
    /// </summary>
    public bool IsCompatible { get; init; }

    /// <summary>
    /// Any compatibility warnings or issues found during validation.
    /// </summary>
    public IReadOnlyList<string> CompatibilityWarnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The reason why the plugin is incompatible (if IsCompatible is false).
    /// </summary>
    public string? IncompatibilityReason { get; init; }

    /// <summary>
    /// Types found in the assembly that might be resource handlers.
    /// </summary>
    public IReadOnlyList<string> PotentialResourceHandlers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Rich plugin information from PluginInfoAttribute (Phase 4.20.5).
    /// </summary>
    public PluginInfo? PluginInfo { get; init; }

    /// <summary>
    /// Plugin dependencies from PluginDependencyAttribute (Phase 4.20.5).
    /// </summary>
    public IReadOnlyList<PluginDependency> Dependencies { get; init; } = Array.Empty<PluginDependency>();

    /// <summary>
    /// Resource compatibility declarations from PluginResourceCompatibilityAttribute (Phase 4.20.5).
    /// </summary>
    public IReadOnlyList<PluginResourceCompatibility> ResourceCompatibilities { get; init; } = Array.Empty<PluginResourceCompatibility>();

    /// <summary>
    /// Creates a new PluginMetadata instance from an assembly.
    /// </summary>
    /// <param name="assembly">The loaded assembly to extract metadata from.</param>
    /// <param name="filePath">The file path where the assembly was loaded from.</param>
    /// <returns>A new PluginMetadata instance.</returns>
    public static PluginMetadata FromAssembly(Assembly assembly, string filePath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fileInfo = new FileInfo(filePath);
        var assemblyName = assembly.GetName();
        var compatibilityResult = ValidateCompatibility(assembly);

        return new PluginMetadata
        {
            FilePath = filePath,
            AssemblyName = assemblyName.Name ?? "Unknown",
            AssemblyFullName = assembly.FullName ?? "Unknown",
            Version = assemblyName.Version ?? new Version(0, 0, 0, 0),
            TargetFramework = GetTargetFramework(assembly),
            IsSigned = IsAssemblySigned(assembly),
            FileSizeBytes = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime,
            IsCompatible = compatibilityResult.IsCompatible,
            CompatibilityWarnings = compatibilityResult.Warnings,
            IncompatibilityReason = compatibilityResult.IncompatibilityReason,
            PotentialResourceHandlers = FindPotentialResourceHandlers(assembly),
            PluginInfo = ExtractPluginInfo(assembly),
            Dependencies = ExtractDependencies(assembly),
            ResourceCompatibilities = ExtractResourceCompatibilities(assembly)
        };
    }

    private static string? GetTargetFramework(Assembly assembly)
    {
        try
        {
            var targetFrameworkAttribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            return targetFrameworkAttribute?.FrameworkName;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAssemblySigned(Assembly assembly)
    {
        try
        {
            // Check if the assembly has a strong name
            var publicKey = assembly.GetName().GetPublicKey();
            return publicKey != null && publicKey.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static CompatibilityResult ValidateCompatibility(Assembly assembly)
    {
        var warnings = new List<string>();
        var isCompatible = true;
        string? incompatibilityReason = null;

        try
        {
            // Check for .NET Framework vs .NET Core/5+ compatibility
            var targetFramework = GetTargetFramework(assembly);
            if (targetFramework != null)
            {
                if (targetFramework.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add("Plugin targets .NET Framework - may have compatibility issues with .NET 9");
                }
                else if (targetFramework.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase) ||
                         targetFramework.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract version number to check compatibility
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(targetFramework, @"(\d+)\.(\d+)");
                    if (versionMatch.Success && 
                        int.TryParse(versionMatch.Groups[1].Value, out var major) && 
                        major < 8)
                    {
                        warnings.Add($"Plugin targets {targetFramework} - consider updating to .NET 8+ for better compatibility");
                    }
                }
            }

            // Check for potential breaking API usage
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                // Look for legacy patterns that might cause issues
                if (type.Name.Contains("AResource") || type.BaseType?.Name.Contains("AResource") == true)
                {
                    warnings.Add($"Type {type.FullName} uses legacy AResource pattern - may need compatibility shims");
                }
            }
        }
        catch (Exception ex)
        {
            isCompatible = false;
            incompatibilityReason = $"Failed to validate compatibility: {ex.Message}";
        }

        return new CompatibilityResult(isCompatible, warnings, incompatibilityReason);
    }

    private static IReadOnlyList<string> FindPotentialResourceHandlers(Assembly assembly)
    {
        var handlers = new List<string>();

        try
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                // Look for types that might be resource handlers
                if (type.IsClass && !type.IsAbstract)
                {
                    // Check for common resource handler patterns
                    var typeName = type.FullName ?? type.Name;
                    
                    if (typeName.Contains("Resource") && !typeName.Contains("Exception") && !typeName.Contains("Attribute"))
                    {
                        handlers.Add(typeName);
                    }
                    
                    // Check for interfaces that suggest resource handling
                    var interfaces = type.GetInterfaces();
                    foreach (var iface in interfaces)
                    {
                        if (iface.Name.Contains("Resource") || iface.Name.Contains("Handler"))
                        {
                            if (!handlers.Contains(typeName))
                            {
                                handlers.Add(typeName);
                            }
                            break;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore reflection errors
        }

        return handlers.AsReadOnly();
    }

    private static PluginInfo? ExtractPluginInfo(Assembly assembly)
    {
        var pluginInfoAttribute = PluginInfoAttribute.FromAssembly(assembly);
        return pluginInfoAttribute != null ? PluginInfo.FromAttribute(pluginInfoAttribute) : null;
    }

    private static IReadOnlyList<PluginDependency> ExtractDependencies(Assembly assembly)
    {
        var dependencyAttributes = PluginDependencyAttribute.FromAssembly(assembly);
        return dependencyAttributes.Select(PluginDependency.FromAttribute).ToArray();
    }

    private static IReadOnlyList<PluginResourceCompatibility> ExtractResourceCompatibilities(Assembly assembly)
    {
        var compatibilityAttributes = PluginResourceCompatibilityAttribute.FromAssembly(assembly);
        return compatibilityAttributes.Select(PluginResourceCompatibility.FromAttribute).ToArray();
    }

    private sealed record CompatibilityResult(
        bool IsCompatible,
        IReadOnlyList<string> Warnings,
        string? IncompatibilityReason);
}
