using System.Reflection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Text;

/// <summary>
/// Factory for creating text-based resource instances.
/// Handles various XML, JSON, and plain text configuration files used in Sims 4.
/// </summary>
public class TextResourceFactory : ResourceFactoryBase<ITextResource>
{
    private readonly ILogger<TextResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public TextResourceFactory(ILogger<TextResourceFactory> logger) 
        : base(LoadSupportedResourceTypes(), priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("TextResourceFactory initialized with {Count} supported resource types", 
            SupportedResourceTypes.Count);
    }

    /// <inheritdoc />
    public override async Task<ITextResource> CreateResourceAsync(
        int apiVersion, 
        Stream? stream = null, 
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        // Create a temporary resource key for the factory method
        var tempResourceKey = new Core.Package.ResourceKey(0x03B33DDF, 0x00000000, 0x123456789ABCDEF0UL);
        
        // Create a simple logger for the text resource
        using var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var textLogger = loggerFactory.CreateLogger<TextResource>();

        if (stream == null)
        {
            var resource = new TextResource(tempResourceKey, textLogger, apiVersion);
            _logger.LogDebug("Created empty TextResource with API version {ApiVersion}", apiVersion);
            return await Task.FromResult(resource);
        }

        try
        {
            var resource = new TextResource(tempResourceKey, stream, textLogger, apiVersion);
            _logger.LogDebug("Created TextResource from stream with {Length} bytes", stream.Length);
            return await Task.FromResult(resource);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create TextResource from stream");
            throw new ArgumentException($"Failed to parse text resource: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Loads the supported resource types from the embedded resource file.
    /// </summary>
    /// <returns>A list of string resource type identifiers</returns>
    private static IEnumerable<string> LoadSupportedResourceTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "TS4Tools.Resources.Text.TextResourceTypes.txt";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }
        
        using var reader = new StreamReader(stream);
        var types = new List<string>();
        
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
                continue;
                
            var parts = line.Split(' ', 2);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                types.Add(parts[0].Trim());
            }
        }
        
        return types;
    }

    /// <summary>
    /// Gets a human-readable description of this factory.
    /// </summary>
    public string Description => "Creates text-based resources including XML configuration files, JSON data, and plain text resources used in Sims 4";

    /// <summary>
    /// Gets the name of this factory.
    /// </summary>
    public string Name => "Text Resource Factory";
}
