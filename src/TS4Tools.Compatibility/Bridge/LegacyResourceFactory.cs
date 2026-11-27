// Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 117-129 (WrapperForType: the wrapper
// is constructed through its (int APIversion, Stream s) constructor; a null stream creates a new resource) and
// legacy_references/Sims4Tools/s4pe/MainForm.cs line 2652 (s4pe asks for API version 0).

using LegacyResource = s4pi.Interfaces.IResource;

namespace TS4Tools.Compatibility;

/// <summary>
/// A TS4Tools <see cref="IResourceFactory"/> that creates resources with a legacy s4pi wrapper type (one listed by
/// an <see cref="s4pi.Interfaces.AResourceHandler"/>) and returns them as <see cref="LegacyResourceAdapter"/>s.
/// </summary>
public sealed class LegacyResourceFactory : IResourceFactory
{
    /// <summary>API version requested from legacy wrappers (s4pe always passes 0).</summary>
    public const int ApiVersion = 0;

    private readonly ConstructorInfo _constructor;

    /// <summary>
    /// Creates a factory for a legacy wrapper type.
    /// </summary>
    /// <param name="wrapperType">A legacy <see cref="LegacyResource"/> type with an <c>(int, Stream)</c> constructor.</param>
    /// <exception cref="ArgumentException">The type is not a usable legacy wrapper.</exception>
    public LegacyResourceFactory(Type wrapperType)
    {
        ArgumentNullException.ThrowIfNull(wrapperType);
        if (wrapperType.IsAbstract || !typeof(LegacyResource).IsAssignableFrom(wrapperType))
            throw new ArgumentException($"{wrapperType.FullName} is not a concrete s4pi.Interfaces.IResource.", nameof(wrapperType));

        _constructor = wrapperType.GetConstructor([typeof(int), typeof(Stream)])
            ?? throw new ArgumentException($"{wrapperType.FullName} has no (int APIversion, Stream s) constructor.", nameof(wrapperType));
        WrapperType = wrapperType;
    }

    /// <summary>Gets the legacy wrapper type this factory constructs.</summary>
    public Type WrapperType { get; }

    /// <inheritdoc />
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data) =>
        new LegacyResourceAdapter(Construct(new MemoryStream(data.ToArray())));

    /// <inheritdoc />
    public IResource CreateEmpty(ResourceKey key) => new LegacyResourceAdapter(Construct(null));

    private LegacyResource Construct(Stream? stream)
    {
        try
        {
            return (LegacyResource)_constructor.Invoke([ApiVersion, stream]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the wrapper's own exception (legacy shows it as the preview error).
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(LegacyResourceFactory)}({WrapperType.FullName})";
}
