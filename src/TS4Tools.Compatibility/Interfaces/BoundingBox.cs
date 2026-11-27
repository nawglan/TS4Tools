// Source: legacy_references/Sims4Tools/s4pi/Interfaces/BoundingBox.cs lines 24-321

namespace s4pi.Interfaces;

/// <summary>
/// Defines a vertex - a point in 3d space defined by three coordinates.
/// </summary>
public class Vertex : AHandlerElement, IEquatable<Vertex>
{
    private const int recommendedApiVersion = 1;

    #region Attributes
    private float x;
    private float y;
    private float z;
    #endregion

    #region Constructors
    /// <summary>Create a vertex at { 0, 0, 0 }.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    public Vertex(int apiVersion, EventHandler? handler) : base(apiVersion, handler) { }

    /// <summary>Create a vertex from a <see cref="System.IO.Stream"/> (three little-endian floats).</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="s">The stream containing the vertex data.</param>
    public Vertex(int apiVersion, EventHandler? handler, Stream s) : base(apiVersion, handler) { Parse(s); }

    /// <summary>Create a vertex from a given value.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">The vertex to copy.</param>
    public Vertex(int apiVersion, EventHandler? handler, Vertex basis)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).x, basis.y, basis.z) { }

    /// <summary>Create a vertex at { x, y, z }.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    public Vertex(int apiVersion, EventHandler? handler, float x, float y, float z) : base(apiVersion, handler)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    #endregion

    #region Data I/O
    private void Parse(Stream s)
    {
        using var r = LegacyBinary.Reader(s);
        x = r.ReadSingle();
        y = r.ReadSingle();
        z = r.ReadSingle();
    }

    /// <summary>Write the vertex to the given <see cref="System.IO.Stream"/>.</summary>
    /// <param name="s">The stream to write to.</param>
    public void UnParse(Stream s)
    {
        using var w = LegacyBinary.Writer(s);
        w.Write(x);
        w.Write(y);
        w.Write(z);
    }
    #endregion

    #region AHandlerElement Members
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>The list of available field names on this API object.</summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IEquatable<Vertex> Members
    /// <summary>Indicates whether the current <see cref="Vertex"/> instance is equal to another.</summary>
    /// <param name="other">The other vertex.</param>
    /// <returns>true if the coordinates are equal.</returns>
    public bool Equals(Vertex? other) => other is not null && x == other.x && y == other.y && z == other.z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vertex v && Equals(v);

    /// <inheritdoc />
    public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    #endregion

    #region Content Fields
    /// <summary>X coordinate.</summary>
    [ElementPriority(1)]
    public float X { get => x; set { if (x != value) { x = value; OnElementChanged(); } } }

    /// <summary>Y coordinate.</summary>
    [ElementPriority(2)]
    public float Y { get => y; set { if (y != value) { y = value; OnElementChanged(); } } }

    /// <summary>Z coordinate.</summary>
    [ElementPriority(3)]
    public float Z { get => z; set { if (z != value) { z = value; OnElementChanged(); } } }

    /// <summary>A displayable representation of the object.</summary>
    public string Value => "{ " + string.Format(CultureInfo.CurrentCulture, "{0:F4}; {1:F4}; {2:F4}", x, y, z) + " }";
    #endregion
}

/// <summary>
/// Defines a bounding box - a imaginary box large enough to completely contain an object - by its minimum and maximum vertices.
/// </summary>
public class BoundingBox : AHandlerElement, IEquatable<BoundingBox>
{
    private const int recommendedApiVersion = 1;

    #region Attributes
    private Vertex min;
    private Vertex max;
    #endregion

    #region Constructors
    /// <summary>Create a zero-sized bounding box at the origin.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    public BoundingBox(int apiVersion, EventHandler? handler) : base(apiVersion, handler)
    {
        min = new Vertex(requestedApiVersion, handler);
        max = new Vertex(requestedApiVersion, handler);
    }

    /// <summary>Create a bounding box from a <see cref="System.IO.Stream"/> (min then max vertex).</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="s">The stream containing the bounding box data.</param>
    public BoundingBox(int apiVersion, EventHandler? handler, Stream s) : base(apiVersion, handler)
    {
        min = new Vertex(requestedApiVersion, handler, s);
        max = new Vertex(requestedApiVersion, handler, s);
    }

    /// <summary>Create a bounding box from a given value.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="basis">The bounding box to copy.</param>
    public BoundingBox(int apiVersion, EventHandler? handler, BoundingBox basis)
        : this(apiVersion, handler, (basis ?? throw new ArgumentNullException(nameof(basis))).min, basis.max) { }

    /// <summary>Create a bounding box with the specified vertices.</summary>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="handler">The <see cref="EventHandler"/> delegate to invoke if the element changes.</param>
    /// <param name="min">The lowest coordinated vertex.</param>
    /// <param name="max">The highest coordinated vertex.</param>
    public BoundingBox(int apiVersion, EventHandler? handler, Vertex min, Vertex max) : base(apiVersion, handler)
    {
        this.min = new Vertex(requestedApiVersion, handler, min);
        this.max = new Vertex(requestedApiVersion, handler, max);
    }
    #endregion

    #region Data I/O
    /// <summary>Write the bounding box to the given <see cref="System.IO.Stream"/>.</summary>
    /// <param name="s">The stream to write to.</param>
    public void UnParse(Stream s)
    {
        min ??= new Vertex(requestedApiVersion, handler);
        min.UnParse(s);

        max ??= new Vertex(requestedApiVersion, handler);
        max.UnParse(s);
    }
    #endregion

    #region AHandlerElement Members
    /// <summary>The best supported version of the API available.</summary>
    public override int RecommendedApiVersion => recommendedApiVersion;

    /// <summary>The list of available field names on this API object.</summary>
    public override List<string> ContentFields => GetContentFields(requestedApiVersion, GetType());
    #endregion

    #region IEquatable<BoundingBox> Members
    /// <summary>Indicates whether the current <see cref="BoundingBox"/> instance is equal to another.</summary>
    /// <param name="other">The other bounding box.</param>
    /// <returns>true if both vertices are equal.</returns>
    public bool Equals(BoundingBox? other) => other is not null && min.Equals(other.min) && max.Equals(other.max);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BoundingBox b && Equals(b);

    /// <inheritdoc />
    public override int GetHashCode() => min.GetHashCode() ^ max.GetHashCode();
    #endregion

    #region Content Fields
    /// <summary>The lowest coordinated vertex of the bounding box (setting stores a copy).</summary>
    [ElementPriority(1)]
    public Vertex Min
    {
        get => min;
        set { if (min != value) { min = new Vertex(requestedApiVersion, handler, value); OnElementChanged(); } }
    }

    /// <summary>The highest coordinated vertex of the bounding box (setting stores a copy).</summary>
    [ElementPriority(2)]
    public Vertex Max
    {
        get => max;
        set { if (max != value) { max = new Vertex(requestedApiVersion, handler, value); OnElementChanged(); } }
    }

    /// <summary>A displayable representation of the object.</summary>
    public string Value => string.Format(CultureInfo.CurrentCulture, "[ Min: {0} | Max: {1} ]", min.Value, max.Value);
    #endregion
}
