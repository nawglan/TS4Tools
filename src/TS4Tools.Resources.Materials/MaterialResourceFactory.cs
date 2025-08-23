using System.Diagnostics.CodeAnalysis;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Materials;

/// <summary>
/// Factory for creating material resources (resource type 0x545AC67A).
/// Materials control the visual appearance of 3D objects, including textures, lighting, and surface properties.
/// Also known as SWB (Surface Wetness Buffer) resources.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Factory methods need to handle various resource creation scenarios")]
public sealed class MaterialResourceFactory : ResourceFactoryBase<IMaterialResource>
{
    /// <summary>
    /// The resource type identifier for Material resources.
    /// </summary>
    public const uint ResourceType = 0x545AC67A;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialResourceFactory"/> class.
    /// </summary>
    public MaterialResourceFactory() : base(new[] { "0x545AC67A", "SWB", "Material" }, priority: 100)
    {
    }

    /// <summary>
    /// Creates a new material resource instance asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource.</param>
    /// <param name="stream">Optional stream containing material data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the created resource instance.</returns>
    /// <exception cref="ArgumentException">Thrown when API version is invalid.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream data cannot be parsed as material.</exception>
    public override async Task<IMaterialResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateApiVersion(apiVersion);

        var resource = new MaterialResource();

        if (stream != null)
        {
            try
            {
                using var memoryStream = await CreateMemoryStreamAsync(stream, cancellationToken);
                if (memoryStream != null)
                {
                    var data = memoryStream.ToArray();
                    await resource.LoadFromDataAsync(data, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not ArgumentNullException and not OperationCanceledException)
            {
                throw new InvalidDataException($"Failed to create Material resource from stream: {ex.Message}", ex);
            }
        }

        return resource;
    }
}
