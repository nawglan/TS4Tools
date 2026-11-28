// Source: legacy wrappers throw on data they cannot parse (e.g.
// legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/BoneResource.cs lines 43-61), so they never hold
// unparsed data. Wrappers here that keep such data verbatim instead (BONE, GEOM list, CLIP) use this guard so that
// an edit to it is refused rather than silently discarded when the raw bytes are written back.

namespace TS4Tools.Wrappers;

/// <summary>
/// Protects a wrapper that writes unparsed data back verbatim from edits that would be lost.
/// </summary>
/// <remarks>
/// Top-level members call <see cref="ThrowIfEditing"/>. Edits that reach nested objects (lists of mutable elements,
/// sub-objects with setters) cannot be intercepted there, so the wrapper also takes a snapshot of its parsed state
/// after a failed parse (<see cref="Capture"/>) and checks it before writing the raw bytes back
/// (<see cref="ThrowIfChanged"/>).
/// </remarks>
internal sealed class UnparsedDataGuard
{
    private byte[]? _snapshot;

    /// <summary>True while the wrapper's Parse runs (parsing assigns the members itself).</summary>
    public bool Parsing { get; set; }

    /// <summary>Refuses an edit while the wrapper holds unparsed data.</summary>
    /// <exception cref="InvalidOperationException">The wrapper holds unparsed data.</exception>
    public void ThrowIfEditing(bool isValid, int rawLength, string typeName)
    {
        if (!Parsing && !isValid && rawLength > 0)
            throw Refused(typeName);
    }

    /// <summary>Records the parsed state after a failed parse.</summary>
    public void Capture(Func<ReadOnlyMemory<byte>> serializeParsedState)
    {
        try
        {
            _snapshot = serializeParsedState().ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The partly parsed state cannot be written: only the top-level checks apply.
            _snapshot = null;
        }
    }

    /// <summary>Throws if the parsed state changed since <see cref="Capture"/> (a nested edit that would be lost).</summary>
    /// <exception cref="InvalidOperationException">The parsed state was edited.</exception>
    public void ThrowIfChanged(Func<ReadOnlyMemory<byte>> serializeParsedState, string typeName)
    {
        if (_snapshot is null) return;
        ReadOnlyMemory<byte> current;
        try
        {
            current = serializeParsedState();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw Refused(typeName, ex);
        }
        if (!current.Span.SequenceEqual(_snapshot))
            throw Refused(typeName);
    }

    private static InvalidOperationException Refused(string typeName, Exception? inner = null) =>
        new($"{typeName} could not be parsed; it is kept unchanged and cannot be edited.", inner);
}
