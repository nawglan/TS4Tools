// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs lines 67-121 (Stream/AsBytes serialise the
// current content; OnResourceChanged raises ResourceChanged) and legacy_references/Sims4Tools/s4pe/MainForm.cs
// lines 2663-2669 (s4pe subscribes to ResourceChanged to mark the resource dirty) and 2711-2725
// (HasStringValueContentField: a string "Value" content field is the preview text).

using LegacyResource = s4pi.Interfaces.IResource;

namespace TS4Tools.Compatibility;

/// <summary>
/// A TS4Tools <see cref="IResource"/> view of a resource created by a legacy s4pi wrapper.
/// </summary>
/// <remarks>
/// <para>This is the reverse of <see cref="s4pi.WrapperDealer.TS4ToolsResource"/>: it lets TS4Tools (the UI's Value
/// view, property grid and commit path) use a wrapper written against the s4pi API and recompiled against
/// TS4Tools.Compatibility.</para>
/// <para><see cref="Data"/> is the legacy <see cref="LegacyResource.AsBytes"/>, so it reflects edits made to the legacy
/// object (legacy regenerates its stream with UnParse when dirty). Each legacy
/// <see cref="LegacyResource.ResourceChanged"/> marks this resource dirty and raises <see cref="Changed"/>.</para>
/// </remarks>
public sealed class LegacyResourceAdapter : IResource
{
    private readonly EventHandler _legacyChanged;
    private bool _disposed;

    /// <summary>
    /// Wraps a legacy resource.
    /// </summary>
    /// <param name="legacyResource">The legacy wrapper instance.</param>
    public LegacyResourceAdapter(LegacyResource legacyResource)
    {
        LegacyResource = legacyResource ?? throw new ArgumentNullException(nameof(legacyResource));
        _legacyChanged = (_, _) =>
        {
            IsDirty = true;
            Changed?.Invoke(this, EventArgs.Empty);
        };
        LegacyResource.ResourceChanged += _legacyChanged;
    }

    /// <summary>Gets the legacy wrapper instance (shown in the property grid).</summary>
    public LegacyResource LegacyResource { get; }

    /// <summary>Gets the legacy wrapper type.</summary>
    public Type LegacyWrapperType => LegacyResource.GetType();

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return LegacyResource.AsBytes;
        }
    }

    /// <inheritdoc />
    public bool IsDirty { get; private set; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>
    /// Gets the legacy preview text: the wrapper's string <c>Value</c> content field when it has one (as s4pe shows
    /// it), otherwise its <see cref="object.ToString"/> (legacy <c>AApiVersionedFields.ToString</c> is its
    /// ValueBuilder dump).
    /// </summary>
    public string GetValueText()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Source: MainForm.cs lines 2711-2725 (HasStringValueContentField) and 2947-2955 (preview = resource["Value"])
        if (LegacyResource.ContentFields.Contains("Value") && LegacyResource["Value"].Value is string value)
        {
            return value;
        }

        return LegacyResource.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        LegacyResource.ResourceChanged -= _legacyChanged;
        (LegacyResource as IDisposable)?.Dispose();
    }
}
