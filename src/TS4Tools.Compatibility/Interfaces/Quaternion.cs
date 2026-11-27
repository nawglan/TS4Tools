// Source: legacy_references/Sims4Tools/s4pi/Interfaces/Quaternion.cs lines 24-185

namespace s4pi.Interfaces;

/// <summary>
/// Defines a quaternion - a four-dimensional vector (A, B, C, D) of single-precision values.
/// </summary>
public class Quaternion : AHandlerElement, IEquatable<Quaternion>
{
    private const int recommendedApiVersion = 1;

    #region Attributes
    private float a;
    private float b;
    private float c;
    private float d;
    #endregion

    #region Constructors
    /// <summary>Create a quaternion { 0, 0, 0, 0 }.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    public Quaternion(int apiVersion, EventHandler? handler) : base(apiVersion, handler) { }

    /// <summary>Create a quaternion from a <see cref="System.IO.Stream"/> (four little-endian floats).</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="s">The stream containing the quaternion data.</param>
    public Quaternion(int apiVersion, EventHandler? handler, Stream s) : base(apiVersion, handler) { Parse(s); }

    /// <summary>Create a quaternion from a given value.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">The quaternion to copy.</param>
    public Quaternion(int apiVersion, EventHandler? handler, Quaternion basis)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).a, basis.b, basis.c, basis.d) { }

    /// <summary>Create a quaternion { a, b, c, d }.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="a">Q[A] value.</param>
    /// <param name="b">Q[B] value.</param>
    /// <param name="c">Q[C] value.</param>
    /// <param name="d">Q[D] value.</param>
    public Quaternion(int apiVersion, EventHandler? handler, float a, float b, float c, float d) : base(apiVersion, handler)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
    #endregion

    #region Data I/O
    private void Parse(Stream s)
    {
        using var r = LegacyBinary.Reader(s);
        a = r.ReadSingle();
        b = r.ReadSingle();
        c = r.ReadSingle();
        d = r.ReadSingle();
    }

    /// <summary>Write the quaternion to the given <see cref="System.IO.Stream"/>.</summary>
    /// <param name="s">The stream to write to.</param>
    public void UnParse(Stream s)
    {
        using var w = LegacyBinary.Writer(s);
        w.Write(a);
        w.Write(b);
        w.Write(c);
        w.Write(d);
    }
    #endregion

    #region AHandlerElement Members
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>The list of available field names on this API object.</summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IEquatable<Quaternion> Members
    /// <summary>Indicates whether the current <see cref="Quaternion"/> instance is equal to another.</summary>
    /// <param name="other">The other quaternion.</param>
    /// <returns>true if all four values are equal.</returns>
    public bool Equals(Quaternion? other) => other is not null && a == other.a && b == other.b && c == other.c && d == other.d;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Quaternion q && Equals(q);

    /// <inheritdoc />
    public override int GetHashCode() => a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode() ^ d.GetHashCode();
    #endregion

    #region Content Fields
    /// <summary>Q[A] value.</summary>
    [ElementPriority(1)]
    public float A { get => a; set { if (a != value) { a = value; OnElementChanged(); } } }

    /// <summary>Q[B] value.</summary>
    [ElementPriority(2)]
    public float B { get => b; set { if (b != value) { b = value; OnElementChanged(); } } }

    /// <summary>Q[C] value.</summary>
    [ElementPriority(3)]
    public float C { get => c; set { if (c != value) { c = value; OnElementChanged(); } } }

    /// <summary>Q[D] value.</summary>
    /// <remarks>Legacy gives D the same priority as C (Quaternion.cs line 176).</remarks>
    [ElementPriority(3)]
    public float D { get => d; set { if (d != value) { d = value; OnElementChanged(); } } }

    /// <summary>A displayable representation of the object.</summary>
    public string Value => "{ " + string.Format(CultureInfo.CurrentCulture, "{0:F4}; {1:F4}; {2:F4}; {3:F4}", a, b, c, d) + " }";
    #endregion
}
