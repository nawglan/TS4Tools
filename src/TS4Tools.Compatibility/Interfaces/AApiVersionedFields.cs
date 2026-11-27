// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 28-617

namespace s4pi.Interfaces;

/// <summary>
/// API Objects should all descend from this Abstract class.
/// It provides versioning support and ContentFields support.
/// </summary>
public abstract class AApiVersionedFields : IApiVersion, IContentFields
{
    #region IApiVersion Members

    /// <summary>
    /// The version of the API in use.
    /// </summary>
    public int RequestedApiVersion => requestedApiVersion;

    /// <summary>
    /// The best supported version of the API available.
    /// </summary>
    public abstract int RecommendedApiVersion { get; }

    #endregion

    #region IContentFields Members

    /// <summary>
    /// The list of available field names on this API object.
    /// </summary>
    /// <remarks>Implement with a call to <c>GetContentFields(requestedApiVersion, this.GetType())</c>.</remarks>
    public abstract List<string> ContentFields { get; }

    /// <summary>
    /// A typed value on this object.
    /// </summary>
    /// <param name="index">The name of the field (i.e. one of the values from ContentFields); dotted paths
    /// ("Field.SubField") walk nested properties.</param>
    /// <returns>The typed value of the named field (default format "X").</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown index name is requested.</exception>
    /// <remarks>Source: AApiVersionedFields.cs lines 69-109</remarks>
    public virtual TypedValue this[string index]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(index);
            object? result = this;
            var t = GetType();
            foreach (var f in index.Split('.'))
            {
                var p = ContentFieldReflection.FindProperty(t, f)
                    ?? throw new ArgumentOutOfRangeException(nameof(index), "Unexpected value received in index: " + index);
                t = p.PropertyType;
                result = p.GetValue(result, null);
            }
            return new TypedValue(t, result, "X");
        }
        set
        {
            ArgumentNullException.ThrowIfNull(index);
            ArgumentNullException.ThrowIfNull(value);
            var fields = index.Split('.');
            object? result = this;
            var t = GetType();
            PropertyInfo? p = null;
            for (var i = 0; i < fields.Length; i++)
            {
                p = ContentFieldReflection.FindProperty(t, fields[i])
                    ?? throw new ArgumentOutOfRangeException(nameof(index), "Unexpected value received in index: " + index);
                if (i < fields.Length - 1)
                {
                    t = p.PropertyType;
                    result = p.GetValue(result, null);
                }
            }
            p!.SetValue(result, value.Value, null);
        }
    }

    #endregion

    /// <summary>
    /// Versioning is not currently implemented.
    /// Set this to the version of the API requested on object creation.
    /// </summary>
    protected int requestedApiVersion;

    // Source: AApiVersionedFields.cs lines 119-129 — the public properties of this class are never content fields.
    private static readonly List<string> banlist = typeof(AApiVersionedFields).GetProperties().Select(m => m.Name).ToList();

    // Source: AApiVersionedFields.cs lines 131-138. The attribute is read from the PropertyInfo enumerated by
    // GetContentFields (legacy re-looked the property up by name, which is the same member unless the name is ambiguous).
    private static int Version(Type attribute, PropertyInfo property)
    {
        foreach (VersionAttribute attr in property.GetCustomAttributes(attribute, true))
            return attr.Version;
        return 0;
    }

    // Source: AApiVersionedFields.cs lines 150-158
    private static int GetRecommendedApiVersion(Type t)
    {
        var fi = t.GetField("recommendedApiVersion", BindingFlags.Static | BindingFlags.NonPublic);
        if (fi == null || fi.FieldType != typeof(int))
            return 0;
        return (int)fi.GetValue(null)!;
    }

    // Source: AApiVersionedFields.cs lines 160-177
    private static bool CheckVersion(PropertyInfo property, int requestedApiVersion)
    {
        if (requestedApiVersion == 0) return true;
        var min = Version(typeof(MinimumVersionAttribute), property);
        if (min != 0 && requestedApiVersion < min) return false;
        var max = Version(typeof(MaximumVersionAttribute), property);
        if (max != 0 && requestedApiVersion > max) return false;
        return true;
    }

    /// <summary>
    /// Return the list of fields for a given API Class.
    /// </summary>
    /// <param name="APIversion">Set to 0 (== "best").</param>
    /// <param name="t">The class type for which to get the fields.</param>
    /// <returns>List of field names for the given API version, sorted by <see cref="ElementPriorityAttribute"/> then name (ordinal).</returns>
    /// <remarks>Source: AApiVersionedFields.cs lines 186-209</remarks>
    public static List<string> GetContentFields(int APIversion, Type t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var fields = new List<string>();

        var recommendedApiVersion = GetRecommendedApiVersion(t); // zero if no "recommendedApiVersion" const field
        foreach (var m in t.GetProperties())
        {
            if (banlist.Contains(m.Name)) continue;
            if (!CheckVersion(m, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;
            fields.Add(m.Name);
        }
        fields.Sort(new PriorityComparer(t));

        return fields;
    }

    /// <summary>
    /// Get the TGIBlock list for a Content Field.
    /// </summary>
    /// <param name="o">The object to query.</param>
    /// <param name="f">The property name under inspection.</param>
    /// <returns>The TGIBlock list for a Content Field, if present; otherwise <c>null</c>.</returns>
    /// <remarks>Source: AApiVersionedFields.cs lines 217-231</remarks>
    public static DependentList<TGIBlock>? GetTGIBlocks(AApiVersionedFields o, string f)
    {
        ArgumentNullException.ThrowIfNull(o);
        var tgiBlockListCF = TGIBlockListContentFieldAttribute.GetTGIBlockListContentField(o.GetType(), f);
        if (tgiBlockListCF != null)
        {
            try
            {
                return o[tgiBlockListCF].Value as DependentList<TGIBlock>;
            }
#pragma warning disable CA1031 // Legacy swallows any failure reading the referenced list
            catch
            {
            }
#pragma warning restore CA1031
        }
        return null;
    }

    /// <summary>
    /// Get the TGIBlock list for a Content Field.
    /// </summary>
    /// <param name="f">The property name under inspection.</param>
    /// <returns>The TGIBlock list for a Content Field, if present; otherwise <c>null</c>.</returns>
    public DependentList<TGIBlock>? GetTGIBlocks(string f) => GetTGIBlocks(this, f);

    // Source: AApiVersionedFields.cs lines 243-262
    private sealed class PriorityComparer(Type type) : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var res = ElementPriorityAttribute.GetPriority(type, x!).CompareTo(ElementPriorityAttribute.GetPriority(type, y!));
            if (res == 0)
                res = string.Compare(x, y, StringComparison.Ordinal);
            return res;
        }
    }

    private static readonly List<string> valueBuilderBanlist = ["Value", "Stream", "AsBytes"];

    private static readonly List<string> dictionaryBanlist = ["Keys", "Values", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot"];

    /// <summary>
    /// The fields ValueBuilder will return; used to eliminate those that should not be used.
    /// </summary>
    /// <remarks>Source: AApiVersionedFields.cs lines 272-285</remarks>
    protected virtual List<string> ValueBuilderFields
    {
        get
        {
            var fields = ContentFields;
            fields.RemoveAll(banlist.Contains);
            fields.RemoveAll(valueBuilderBanlist.Contains);
            if (this is IDictionary)
                fields.RemoveAll(dictionaryBanlist.Contains);
            return fields;
        }
    }

    private static string IndexFormat(int count) => "X" + count.ToString("X", CultureInfo.InvariantCulture).Length.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a string representing the value of the field (and any contained sub-fields).
    /// </summary>
    /// <remarks>Source: AApiVersionedFields.cs lines 290-436</remarks>
    protected string ValueBuilder
    {
        get
        {
            var sb = new StringBuilder();
            var culture = CultureInfo.CurrentCulture;
            const string headerFormat = "\n--- {0}: {1} (0x{2:X}) ---";

            foreach (var f in ValueBuilderFields)
            {
                var tv = this[f];

                if (typeof(AApiVersionedFields).IsAssignableFrom(tv.Type))
                {
                    if (tv.Value is AApiVersionedFields apiObj && apiObj.ContentFields.Contains("Value") &&
                        typeof(string).IsAssignableFrom(GetContentFieldTypes(requestedApiVersion, tv.Type)["Value"]))
                    {
                        var elem = (string?)apiObj["Value"].Value ?? "";
                        if (elem.Contains('\n', StringComparison.Ordinal))
                            sb.Append("\n--- " + tv.Type.Name + ": " + f + " ---\n   " + elem.Replace("\n", "\n   ", StringComparison.Ordinal).TrimEnd() + "\n---");
                        else
                            sb.Append("\n" + f + ": " + elem);
                    }
                }
                else if (tv.Type.BaseType != null && tv.Type.BaseType.Name.Contains("IndexList`", StringComparison.Ordinal))
                {
                    var l = (IList)tv.Value!;
                    var fmt = "\n   [{0:" + IndexFormat(l.Count) + "}]: {1}";
                    var i = 0;

                    sb.Append(string.Format(culture, headerFormat, tv.Type.Name, f, l.Count));
                    foreach (AHandlerElement v in l)
                        sb.Append(string.Format(culture, fmt, i++, (string?)v["Value"].Value));
                    sb.Append("\n---");
                }
                else if (tv.Type.BaseType != null && tv.Type.BaseType.Name.Contains("SimpleList`", StringComparison.Ordinal))
                {
                    var l = (IList)tv.Value!;
                    var fmt = "\n   [{0:" + IndexFormat(l.Count) + "}]: {1}";
                    var i = 0;

                    sb.Append(string.Format(culture, headerFormat, tv.Type.Name, f, l.Count));
                    foreach (AHandlerElement v in l)
                        sb.Append(string.Format(culture, fmt, i++, v["Val"]));
                    sb.Append("\n---");
                }
                else if (typeof(DependentList<TGIBlock>).IsAssignableFrom(tv.Type))
                {
                    var l = (DependentList<TGIBlock>)tv.Value!;
                    var fmt = "\n   [{0:" + IndexFormat(l.Count) + "}]: {1}";
                    var i = 0;

                    sb.Append(string.Format(culture, headerFormat, tv.Type.Name, f, l.Count));
                    foreach (var v in l)
                        sb.Append(string.Format(culture, fmt, i++, v));
                    sb.Append("\n---");
                }
                else if (tv.Type.BaseType != null && tv.Type.BaseType.Name.Contains("DependentList`", StringComparison.Ordinal))
                {
                    var l = (IList)tv.Value!;
                    var fmtLong = "\n--- {0}[{1:" + IndexFormat(l.Count) + "}] ---\n   ";
                    var fmtShort = "\n   [{0:" + IndexFormat(l.Count) + "}]: {1}";
                    var i = 0;

                    sb.Append(string.Format(culture, headerFormat, tv.Type.Name, f, l.Count));
                    foreach (AHandlerElement v in l)
                    {
                        if (v.ContentFields.Contains("Value") &&
                            typeof(string).IsAssignableFrom(GetContentFieldTypes(requestedApiVersion, v.GetType())["Value"]))
                        {
                            var elem = (string?)v["Value"].Value ?? "";
                            if (elem.Contains('\n', StringComparison.Ordinal))
                                sb.Append(string.Format(culture, fmtLong, f, i++) + elem.Replace("\n", "\n   ", StringComparison.Ordinal).TrimEnd());
                            else
                                sb.Append(string.Format(culture, fmtShort, i++, elem));
                        }
                    }
                    sb.Append("\n---");
                }
                else if (tv.Type.HasElementType && typeof(AApiVersionedFields).IsAssignableFrom(tv.Type.GetElementType()))
                {
                    // it's an AApiVersionedFields array, slightly glossy...
                    sb.Append(string.Format(culture, headerFormat, tv.Type.Name, f, ((Array)tv.Value!).Length));
                    sb.Append("\n   " + tv.ToString("X").Replace("\n", "\n   ", StringComparison.Ordinal).TrimEnd() + "\n---");
                }
                else
                {
                    var suffix = "";
                    var tgis = GetTGIBlocks(f);
                    if (tgis != null && tgis.Count > 0)
                    {
                        try
                        {
                            var i = Convert.ToInt32(tv.Value, culture);
                            if (i >= 0 && i < tgis.Count)
                                suffix = " (" + tgis[i] + ")";
                        }
#pragma warning disable CA1031 // Legacy reports any conversion failure inline
                        catch (Exception e)
                        {
                            sb.Append(" (Exception: " + e.Message + ")");
                        }
#pragma warning restore CA1031
                    }
                    sb.Append("\n" + f + ": " + tv + suffix);
                }
            }

            if (this is IDictionary d)
            {
                var fmt = "\n   [{0:" + IndexFormat(d.Count) + "}] {1}: {2}";
                var i = 0;
                sb.Append("\n--- (0x" + d.Count.ToString("X", CultureInfo.InvariantCulture) + ") ---");
                foreach (var key in d.Keys)
                {
                    var val = d[key];
                    sb.Append(string.Format(culture, fmt, i++,
                        new TypedValue(key.GetType(), key, "X"),
                        new TypedValue(val?.GetType() ?? typeof(object), val, "X")));
                }
                sb.Append("\n---");
            }

            return sb.ToString().Trim('\n');
        }
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents the current <see cref="AApiVersionedFields"/> object.
    /// </summary>
    /// <returns>The <see cref="ValueBuilder"/> text dump.</returns>
    public override string ToString() => ValueBuilder;

    /// <summary>
    /// Sorts Content Field names by their <see cref="ElementPriorityAttribute"/> (if set).
    /// </summary>
    /// <param name="x">First content field name.</param>
    /// <param name="y">Second content field name.</param>
    /// <returns>A signed number indicating the relative values of this instance and value.</returns>
    public int CompareByPriority(string x, string y) => new PriorityComparer(GetType()).Compare(x, y);

    /// <summary>
    /// Gets a lookup table from field name to type.
    /// </summary>
    /// <param name="APIversion">Version of API to use.</param>
    /// <param name="t">API data type to query.</param>
    /// <returns>Field name to property type map.</returns>
    /// <remarks>Source: AApiVersionedFields.cs lines 464-486</remarks>
    public static Dictionary<string, Type> GetContentFieldTypes(int APIversion, Type t)
    {
        ArgumentNullException.ThrowIfNull(t);
        var types = new Dictionary<string, Type>();

        var recommendedApiVersion = GetRecommendedApiVersion(t);
        foreach (var m in t.GetProperties())
        {
            if (banlist.Contains(m.Name)) continue;
            if (!CheckVersion(m, APIversion == 0 ? recommendedApiVersion : APIversion)) continue;
            // Legacy Dictionary.Add throws on a hidden (duplicate-named) property; record the most-derived one.
            types[m.Name] = (ContentFieldReflection.FindProperty(t, m.Name) ?? m).PropertyType;
        }

        return types;
    }

    /// <summary>
    /// A class enabling sorting API objects by a ContentFields name.
    /// </summary>
    /// <typeparam name="T">API object type.</typeparam>
    public class Comparer<T> : IComparer<T>
        where T : IContentFields
    {
        private readonly string field;

        /// <summary>
        /// Sort API Objects by <paramref name="field"/>.
        /// </summary>
        /// <param name="field">ContentField name to sort by.</param>
        public Comparer(string field) { this.field = field; }

        /// <summary>
        /// Compares two objects of type T by the named content field.
        /// </summary>
        /// <param name="x">The first IContentFields object to compare.</param>
        /// <param name="y">The second IContentFields object to compare.</param>
        /// <returns>Less than zero: x &lt; y; zero: equal; greater than zero: x &gt; y.</returns>
        public int Compare(T? x, T? y) => x![field].CompareTo(y![field]);
    }

    // Random helper functions that should live somewhere...

    /// <summary>
    /// Convert a string (up to 8 characters) to a UInt64.
    /// </summary>
    /// <param name="s">String to convert.</param>
    /// <returns>UInt64 packed representation of <paramref name="s"/>.</returns>
    /// <remarks>Source: AApiVersionedFields.cs lines 570-582</remarks>
    public static ulong FOURCC(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (s.Length > 8) throw new ArgumentLengthException("String", 8);
        ulong i = 0;
        for (var j = s.Length - 1; j >= 0; j--)
            i += ((uint)s[j]) << (j * 8);
        return i;
    }

    /// <summary>
    /// Convert a UInt64 to a string (up to 8 characters, high-order zeros omitted).
    /// </summary>
    /// <param name="i">Bytes to convert.</param>
    /// <returns>String representation of <paramref name="i"/>.</returns>
    /// <remarks>Source: AApiVersionedFields.cs lines 589-601</remarks>
    public static string FOURCC(ulong i)
    {
        var s = "";
        for (var j = 7; j >= 0; j--)
        {
            var c = (char)((i >> (j * 8)) & 0xff);
            if (s.Length > 0 || c != 0)
                s = c + s;
        }
        return s;
    }

    /// <summary>
    /// Return a space-separated string containing valid enumeration names for the given type.
    /// </summary>
    /// <param name="t">Enum type.</param>
    /// <returns>Valid enum names.</returns>
    public static string FlagNames(Type t) => string.Join(" ", Enum.GetNames(t)).Trim();
}
