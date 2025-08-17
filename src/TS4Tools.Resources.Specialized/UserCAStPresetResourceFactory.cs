namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Factory for creating UserCAStPresetResource instances following the TS4Tools resource factory pattern.
/// Provides async creation methods for User CAS preset resources with validation and error handling.
/// </summary>
public sealed class UserCAStPresetResourceFactory : ResourceFactoryBase<IUserCAStPresetResource>
{
    /// <summary>
    /// Initializes a new instance of the UserCAStPresetResourceFactory class.
    /// </summary>
    public UserCAStPresetResourceFactory()
        : base(new[] { $"0x{UserCAStPresetResource.ResourceType:X8}" })
    {
    }

    /// <summary>
    /// Creates a new empty UserCAStPresetResource asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="stream">Optional stream containing resource data (ignored for new resources)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created UserCAStPresetResource</returns>
    public override Task<IUserCAStPresetResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (stream == null)
        {
            // Create new empty resource
            var resource = new UserCAStPresetResource(apiVersion);
            return Task.FromResult<IUserCAStPresetResource>(resource);
        }

        // Create from stream data
        return CreateFromStreamAsync(apiVersion, stream, cancellationToken);
    }

    /// <summary>
    /// Creates a UserCAStPresetResource from binary data asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="data">The binary data to parse</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created UserCAStPresetResource</returns>
    /// <exception cref="ArgumentException">Thrown when data is invalid</exception>
    public Task<IUserCAStPresetResource> CreateFromDataAsync(
        int apiVersion,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var resource = UserCAStPresetResource.FromData(apiVersion, data.Span);
            return Task.FromResult<IUserCAStPresetResource>(resource);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to create UserCAStPresetResource from data: {ex.Message}", nameof(data), ex);
        }
    }

    /// <summary>
    /// Creates a UserCAStPresetResource from a stream asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="stream">The stream containing UserCAStPreset data</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created UserCAStPresetResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="ArgumentException">Thrown when stream data is invalid</exception>
    public async Task<IUserCAStPresetResource> CreateFromStreamAsync(
        int apiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await UserCAStPresetResource.FromStreamAsync(apiVersion, stream, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to create UserCAStPresetResource from stream: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Creates a UserCAStPresetResource with initial preset data.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="initialPresets">Initial presets to add to the resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created UserCAStPresetResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when initialPresets is null</exception>
    public async Task<IUserCAStPresetResource> CreateWithPresetsAsync(
        int apiVersion,
        IEnumerable<ICASPreset> initialPresets,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initialPresets);
        cancellationToken.ThrowIfCancellationRequested();

        var resource = new UserCAStPresetResource(apiVersion);

        foreach (var preset in initialPresets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await resource.AddPresetAsync(preset, cancellationToken).ConfigureAwait(false);
        }

        return resource;
    }

    /// <summary>
    /// Validates if the provided data represents a valid UserCAStPreset resource.
    /// </summary>
    /// <param name="data">The data to validate</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing true if the data is valid; otherwise, false</returns>
    public Task<bool> ValidateDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Try to parse the data to validate it
            var resource = UserCAStPresetResource.FromData(1, data.Span);
            resource.Dispose();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets information about the UserCAStPreset resource format.
    /// </summary>
    /// <returns>Information about the resource format</returns>
    public ResourceFormatInfo FormatInfo => new()
    {
        ResourceType = UserCAStPresetResource.ResourceType,
        Name = "UserCAStPreset Resource",
        Description = "User-created Character Asset System (CAS) presets for character customization",
        SupportedVersion = UserCAStPresetResource.SupportedVersion,
        FileExtensions = new[] { ".casPreset" },
        MimeType = "application/x-sims4-cas-preset"
    };
}

/// <summary>
/// Provides information about a resource format.
/// </summary>
public sealed class ResourceFormatInfo
{
    /// <summary>
    /// Gets or sets the resource type identifier.
    /// </summary>
    public uint ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the format.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the format.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported format version.
    /// </summary>
    public uint SupportedVersion { get; set; }

    /// <summary>
    /// Gets or sets the common file extensions for this format.
    /// </summary>
    public string[] FileExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the MIME type for this format.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
}
