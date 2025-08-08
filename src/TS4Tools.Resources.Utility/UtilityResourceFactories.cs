using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Factory for creating DataResource instances
/// </summary>
public sealed class DataResourceFactory : ResourceFactoryBase<IResource>
{
    private readonly ILogger<DataResource> _logger;

    /// <summary>
    /// Resource types supported by this factory
    /// </summary>
    private static readonly string[] SupportedTypes =
    {
        "0x0166038C", // DATA
        "0x0166038D", // DATA variant 1
        "0x0166038E"  // DATA variant 2
    };

    public DataResourceFactory(ILogger<DataResource> logger)
        : base(SupportedTypes, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make it async-compliant
        var resourceKey = new ResourceKey(0x0166038C, 0, 0);
        return new DataResource(_logger, resourceKey, stream);
    }

    /// <summary>
    /// Factory name
    /// </summary>
    public string Name => "Data Resource Factory";

    /// <summary>
    /// Factory version
    /// </summary>
    public string Version => "1.0.0";
}

/// <summary>
/// Factory for creating ConfigResource instances
/// </summary>
public sealed class ConfigResourceFactory : ResourceFactoryBase<IResource>
{
    private readonly ILogger<ConfigResource> _logger;

    /// <summary>
    /// Resource types supported by this factory
    /// </summary>
    private static readonly string[] SupportedTypes =
    {
        "0x0000038C", // CONFIG
        "0x0000038D", // CONFIG variant 1
        "0x0000038E"  // CONFIG variant 2
    };

    public ConfigResourceFactory(ILogger<ConfigResource> logger)
        : base(SupportedTypes, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make it async-compliant
        var resourceKey = new ResourceKey(0x0000038C, 0, 0);
        return new ConfigResource(_logger, resourceKey, stream);
    }

    /// <summary>
    /// Factory name
    /// </summary>
    public string Name => "Config Resource Factory";

    /// <summary>
    /// Factory version
    /// </summary>
    public string Version => "1.0.0";
}

/// <summary>
/// Factory for creating MetadataResource instances
/// </summary>
public sealed class MetadataResourceFactory : ResourceFactoryBase<IResource>
{
    private readonly ILogger<MetadataResource> _logger;

    /// <summary>
    /// Resource types supported by this factory
    /// </summary>
    private static readonly string[] SupportedTypes =
    {
        "0x0166044C", // METADATA
        "0x0166044D", // METADATA variant 1
        "0x0166044E"  // METADATA variant 2
    };

    public MetadataResourceFactory(ILogger<MetadataResource> logger)
        : base(SupportedTypes, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make it async-compliant
        var resourceKey = new ResourceKey(0x0166044C, 0, 0);
        return new MetadataResource(_logger, resourceKey, stream);
    }

    /// <summary>
    /// Factory name
    /// </summary>
    public string Name => "Metadata Resource Factory";

    /// <summary>
    /// Factory version
    /// </summary>
    public string Version => "1.0.0";
}
