namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating NGMP (Named Game Map) hash map resource instances.
/// Handles the creation and initialization of NGMP hash map resources with proper validation.
/// </summary>
public sealed class NGMPHashMapResourceFactory : ResourceFactoryBase<INGMPHashMapResource>
{
    /// <summary>
    /// The resource type identifier for NGMP hash map resources.
    /// </summary>
    public const uint ResourceType = 0xF3A38370; // NGMP hash map resource type from The Sims 4

    private readonly ILogger<NGMPHashMapResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NGMPHashMapResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public NGMPHashMapResourceFactory(ILogger<NGMPHashMapResourceFactory>? logger = null)
        : base(new[] { "0xF3A38370", "NGMP" }, priority: 100)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NGMPHashMapResourceFactory>.Instance;
        _logger.LogDebug("NGMPHashMapResourceFactory initialized with priority {Priority}", 100);
    }

    /// <summary>
    /// Creates a new NGMP hash map resource instance asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource.</param>
    /// <param name="stream">Optional stream containing NGMP hash map data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the created resource instance.</returns>
    /// <exception cref="ArgumentException">Thrown when API version is invalid.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream data cannot be parsed as NGMP hash map.</exception>
    public override async Task<INGMPHashMapResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateApiVersion(apiVersion);

        _logger.LogDebug("Creating NGMPHashMapResource with API version {ApiVersion}", apiVersion);

        var resource = new NGMPHashMapResource(null);

        if (stream != null)
        {
            try
            {
                using var memoryStream = await CreateMemoryStreamAsync(stream, cancellationToken);
                if (memoryStream != null)
                {
                    await resource.ParseAsync(memoryStream, cancellationToken);
                    _logger.LogDebug("Successfully created NGMPHashMapResource from stream with {Count} pairs", resource.Count);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogError(ex, "Failed to parse NGMP hash map data from stream");
                throw new InvalidDataException("Unable to parse NGMP hash map data from the provided stream", ex);
            }
        }
        else
        {
            _logger.LogDebug("Created empty NGMPHashMapResource");
        }

        return resource;
    }

    /// <summary>
    /// Validates the API version.
    /// </summary>
    /// <param name="apiVersion">The API version to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the API version is not supported.</exception>
    protected override void ValidateApiVersion(int apiVersion)
    {
        if (apiVersion < 1)
        {
            throw new ArgumentException($"API version {apiVersion} is not supported. Minimum version is 1.", nameof(apiVersion));
        }
    }

    /// <summary>
    /// Creates a memory stream from the input stream for processing.
    /// </summary>
    /// <param name="inputStream">The input stream.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A memory stream containing the data, or null if the input stream is empty.</returns>
    private new async Task<MemoryStream?> CreateMemoryStreamAsync(Stream? inputStream, CancellationToken cancellationToken)
    {
        if (inputStream == null || inputStream.Length == 0)
        {
            _logger.LogWarning("Input stream is empty");
            return null;
        }

        var memoryStream = new MemoryStream();
        try
        {
            await inputStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            _logger.LogDebug("Copied {Length} bytes to memory stream for processing", memoryStream.Length);
            return memoryStream;
        }
        catch
        {
            memoryStream.Dispose();
            throw;
        }
    }
}
