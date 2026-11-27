// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs lines 24-121

namespace s4pi.Interfaces;

/// <summary>
/// An abstract class, descended from <see cref="AApiVersionedFields"/>, providing an abstract implementation of <see cref="IResource"/>.
/// </summary>
/// <remarks>
/// Wrappers parse <see cref="stream"/> in their <c>(int APIversion, Stream s)</c> constructor and implement
/// <see cref="UnParse"/> to serialise their current state. Pass <see cref="OnResourceChanged"/> as the handler of
/// owned elements and lists so that changes mark the resource dirty.
/// </remarks>
public abstract class AResource : AApiVersionedFields, IResource
{
    #region Attributes
    /// <summary>
    /// Resource data <see cref="System.IO.Stream"/>.
    /// </summary>
    protected Stream? stream;

    /// <summary>
    /// Indicates the resource stream may no longer reflect the resource content.
    /// </summary>
    protected bool dirty;
    #endregion

    #region Constructors
    /// <summary>
    /// Create a resource from <paramref name="s"/>.
    /// </summary>
    /// <param name="APIversion">The requested API version.</param>
    /// <param name="s"><see cref="System.IO.Stream"/> containing the resource data, or null for a new resource.</param>
    protected AResource(int APIversion, Stream? s)
    {
        requestedApiVersion = APIversion;
        stream = s;
    }
    #endregion

    #region AApiVersionedFields
    /// <summary>
    /// A <see cref="List{T}"/> of available field names on this API object.
    /// </summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IResource Members
    /// <summary>
    /// The resource content as a <see cref="System.IO.Stream"/>, positioned at 0.
    /// </summary>
    /// <remarks>
    /// Source: AResource.cs lines 67-80. When <see cref="dirty"/> (or always, while
    /// <see cref="s4pi.Settings.Settings.AsBytesWorkaround"/> is set) the stream is regenerated with <see cref="UnParse"/>.
    /// </remarks>
    public virtual Stream Stream
    {
        get
        {
            if (dirty || s4pi.Settings.Settings.AsBytesWorkaround)
            {
                stream = UnParse();
                dirty = false;
            }
            stream!.Position = 0;
            return stream;
        }
    }

    /// <summary>
    /// The resource content as a <see cref="byte"/> array.
    /// </summary>
    /// <remarks>Source: AResource.cs lines 84-94</remarks>
    public virtual byte[] AsBytes
    {
        get
        {
            if (Stream is MemoryStream ms) return ms.ToArray();

            stream!.Position = 0;
            using var r = LegacyBinary.Reader(stream);
            return r.ReadBytes((int)stream.Length);
        }
    }

    /// <summary>
    /// Raised if the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    #endregion

    /// <summary>
    /// <see cref="UnParse"/> returns a <see cref="System.IO.Stream"/> containing the data of the resource.
    /// </summary>
    /// <returns>A <see cref="System.IO.Stream"/> containing the data of the resource.</returns>
    protected abstract Stream UnParse();

    /// <summary>
    /// <see cref="OnResourceChanged"/> marks the resource dirty and raises the <see cref="ResourceChanged"/> event.
    /// </summary>
    /// <param name="sender">The object that changed.</param>
    /// <param name="e">(Empty) event data object.</param>
    protected virtual void OnResourceChanged(object? sender, EventArgs e)
    {
        dirty = true;
        ResourceChanged?.Invoke(sender, e);
    }
}
