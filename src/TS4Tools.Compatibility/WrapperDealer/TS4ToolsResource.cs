// Source: legacy_references/Sims4Tools/s4pi Wrappers/DefaultResource/DefaultResource.cs lines 26-60
// (the legacy byte-holding wrapper this adapter stands in for) and
// legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs lines 29-121 (base contract)

using s4pi.Interfaces;
using CoreResource = TS4Tools.IResource;
using IResourceFactory = TS4Tools.IResourceFactory;
using ResourceKey = TS4Tools.ResourceKey;

namespace s4pi.WrapperDealer;

/// <summary>
/// A legacy <see cref="AResource"/> view of a resource created by a TS4Tools <see cref="IResourceFactory"/>.
/// </summary>
/// <remarks>
/// <see cref="WrapperDealer"/> returns this type when no legacy <see cref="AResourceHandler"/> claims the resource
/// type, so the TS4Tools wrapper (or the TS4Tools default factory) handles it. <see cref="IResource.Stream"/> and
/// <see cref="IResource.AsBytes"/> serialise the TS4Tools resource's current data, and changes raised by the TS4Tools
/// resource are forwarded as <see cref="IResource.ResourceChanged"/>. The typed TS4Tools resource is available from
/// <see cref="GetNativeResource"/>.
/// </remarks>
public sealed class TS4ToolsResource : AResource, IDisposable
{
    private const int recommendedApiVersion = 1;

    private readonly CoreResource native;
    private bool disposed;

    /// <summary>
    /// Create the wrapper by running <paramref name="factory"/> over the data in <paramref name="s"/>.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="factory">The TS4Tools factory for the resource type.</param>
    /// <param name="key">The resource key passed to the factory.</param>
    /// <param name="s">The resource data, or null for a new (empty) resource.</param>
    public TS4ToolsResource(int APIversion, IResourceFactory factory, ResourceKey key, Stream? s) : base(APIversion, s)
    {
        ArgumentNullException.ThrowIfNull(factory);
        native = s == null ? factory.CreateEmpty(key) : factory.Create(key, LegacyBinary.ReadAllFromStart(s));
        native.Changed += OnNativeChanged;
        if (s == null) dirty = true;
    }

    /// <summary>
    /// Gets the TS4Tools resource behind this wrapper.
    /// </summary>
    /// <returns>The TS4Tools <see cref="CoreResource"/>.</returns>
    public CoreResource GetNativeResource() => native;

    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>
    /// Serialise the TS4Tools resource's current data.
    /// </summary>
    /// <returns>A new <see cref="MemoryStream"/> holding the data.</returns>
    protected override Stream UnParse()
    {
        var ms = new MemoryStream();
        ms.Write(native.Data.Span);
        ms.Position = 0;
        return ms;
    }

    private void OnNativeChanged(object? sender, EventArgs e) => OnResourceChanged(this, e);

    /// <summary>
    /// Detaches from and disposes the TS4Tools resource.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        native.Changed -= OnNativeChanged;
        native.Dispose();
    }
}
