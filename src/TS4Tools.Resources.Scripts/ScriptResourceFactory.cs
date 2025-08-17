using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Scripts;

/// <summary>
/// Factory for creating script resources that handle encrypted .NET assemblies.
/// </summary>
public sealed class ScriptResourceFactory : ResourceFactoryBase<IScriptResource>
{
    /// <summary>
    /// Resource type identifier for encrypted signed assemblies.
    /// </summary>
    public const uint ScriptResourceType = 0x073FAA07;

    private readonly ILogger<ScriptResourceFactory> _logger;
    private readonly ILogger<ScriptResource> _scriptLogger;

    /// <summary>
    /// Initializes a new instance of the ScriptResourceFactory class.
    /// </summary>
    /// <param name="logger">Logger for the factory</param>
    /// <param name="scriptLogger">Logger for script resources</param>
    public ScriptResourceFactory(
        ILogger<ScriptResourceFactory> logger,
        ILogger<ScriptResource> scriptLogger)
        : base(new[] { $"0x{ScriptResourceType:X8}" }, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scriptLogger = scriptLogger ?? throw new ArgumentNullException(nameof(scriptLogger));
    }

    /// <inheritdoc />
    public override async Task<IScriptResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating script resource with API version {ApiVersion}", apiVersion);

        try
        {
            if (stream == null || stream.Length == 0)
            {
                _logger.LogDebug("Creating empty script resource");
                return new ScriptResource(_scriptLogger)
                {
                    ResourceKey = new ResourceKey(0, ScriptResourceType, 0)
                };
            }

            // Read all data from stream
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var data = memoryStream.ToArray();

            var resourceKey = new ResourceKey(0, ScriptResourceType, 0);
            var resource = new ScriptResource(resourceKey, data, _scriptLogger);
            _logger.LogDebug("Successfully created script resource with {DataSize} bytes", data.Length);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create script resource");
            throw new InvalidOperationException($"Failed to create script resource: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override IScriptResource CreateResource(Stream stream, uint resourceType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported", nameof(resourceType));
        }

        _logger.LogDebug("Creating script resource (sync) for type 0x{ResourceType:X8}", resourceType);

        try
        {
            // Read all data from stream
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var data = memoryStream.ToArray();

            var resourceKey = new ResourceKey(0, resourceType, 0);
            if (data.Length == 0)
            {
                _logger.LogDebug("Creating empty script resource (sync)");
                return new ScriptResource(_scriptLogger)
                {
                    ResourceKey = resourceKey
                };
            }

            var resource = new ScriptResource(resourceKey, data, _scriptLogger);
            _logger.LogDebug("Successfully created script resource (sync) with {DataSize} bytes", data.Length);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create script resource (sync)");
            throw new InvalidOperationException($"Failed to create script resource: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override IScriptResource CreateEmptyResource(uint resourceType)
    {
        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported", nameof(resourceType));
        }

        _logger.LogDebug("Creating empty script resource for type 0x{ResourceType:X8}", resourceType);

        return new ScriptResource(_scriptLogger)
        {
            ResourceKey = new ResourceKey(0, resourceType, 0)
        };
    }
}
