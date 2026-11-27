// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/TGIN.cs lines 28-227

using s4pi.Interfaces;

namespace s4pi.Extensions;

/// <summary>
/// A structure to manage conversion from <see cref="AResourceKey"/> to the standardised resource export file name format.
/// </summary>
/// <seealso cref="ExtList"/>
[Serializable]
public struct TGIN
{
    /// <summary>The Resource Type represented by this instance.</summary>
    public uint ResType;

    /// <summary>The Resource Group represented by this instance.</summary>
    public uint ResGroup;

    /// <summary>The Resource Instance ID represented by this instance.</summary>
    public ulong ResInstance;

    /// <summary>The Resource Name (from the package name map, based on the IID) represented by this instance.</summary>
    public string? ResName;

    /// <summary>
    /// Instantiate a new <see cref="TGIN"/> based on the <see cref="IResourceKey"/> and <paramref name="name"/>.
    /// </summary>
    /// <param name="rk">An <see cref="IResourceKey"/>.</param>
    /// <param name="name">The name of the resource.</param>
    public TGIN(IResourceKey rk, string? name)
    {
        ArgumentNullException.ThrowIfNull(rk);
        ResType = rk.ResourceType;
        ResGroup = rk.ResourceGroup;
        ResInstance = rk.Instance;
        ResName = name;
    }

    /// <summary>
    /// Cast an <see cref="AResourceKey"/> value to a <see cref="TGIN"/> (with no <see cref="ResName"/>).
    /// </summary>
    /// <param name="value">An <see cref="AResourceKey"/> value.</param>
    public static implicit operator TGIN(AResourceKey value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new TGIN { ResType = value.ResourceType, ResGroup = value.ResourceGroup, ResInstance = value.Instance };
    }

    /// <summary>
    /// Cast a <see cref="TGIN"/> to an <see cref="AResourceKey"/> value.
    /// </summary>
    /// <param name="value">A <see cref="TGIN"/>.</param>
    public static implicit operator AResourceKey(TGIN value) => new TGIBlock(0, null, value.ResType, value.ResGroup, value.ResInstance);

    /// <summary>
    /// Casts a string in the standardised resource export file name format to a <see cref="TGIN"/>.
    /// </summary>
    /// <param name="value">A file name or path in the standardised export file name format (or a TS4 <c>Group!Instance.type</c> name).</param>
    /// <remarks>Source: TGIN.cs lines 105-152</remarks>
    public static implicit operator TGIN(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var res = new TGIN();

        value = Path.GetFileName(value);
        if (FileNameConverter.IsValidFileName(value))
            value = FileNameConverter.ConvertToPackageManagerConvention(value);

        value = Path.GetFileNameWithoutExtension(value);

        int i = value.ToLowerInvariant().IndexOf("s4_", StringComparison.Ordinal);
        if (i == 0)
            value = value[3..];
        i = value.IndexOf("%%+", StringComparison.Ordinal);
        if (i >= 0)
            value = value[..i];

        string[] fnParts = value.Split(['_', '-'], 4);
        if (fnParts.Length >= 3)
        {
            try
            {
                res.ResType = Convert.ToUInt32(fnParts[0], 16);
                res.ResGroup = Convert.ToUInt32(fnParts[1], 16);
                res.ResInstance = Convert.ToUInt64(fnParts[2], 16);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                // Legacy ignores malformed numbers, keeping what was parsed so far.
            }
            if (fnParts.Length == 4)
                res.ResName = UnescapeString(fnParts[3]);
        }

        return res;
    }

    /// <summary>
    /// Casts a <see cref="TGIN"/> to a string in the standardised resource export file name format
    /// (<c>S4_TTTTTTTT_GGGGGGGG_IIIIIIIIIIIIIIII[_name]%%+TAG.ext</c>).
    /// </summary>
    /// <param name="value">A <see cref="TGIN"/>.</param>
    /// <remarks>Source: TGIN.cs lines 160-181</remarks>
    public static implicit operator string(TGIN value)
    {
        string extn = ".dat";
        string key = "0x" + value.ResType.ToString("X8", CultureInfo.InvariantCulture);
        // ExtList's own indexer falls back to "*", so probe the dictionary directly (legacy ContainsKey checks).
        var ext = (Dictionary<string, List<string>>)ExtList.Ext;
        if (ext.TryGetValue(key, out var parts) || ext.TryGetValue("*", out parts))
            extn = string.Join("", parts);

        return string.Format(CultureInfo.InvariantCulture,
            !string.IsNullOrEmpty(value.ResName)
                ? "S4_{0:X8}_{1:X8}_{2:X16}_{3}%%+{4}"
                : "S4_{0:X8}_{1:X8}_{2:X16}%%+{4}",
            value.ResType,
            value.ResGroup,
            value.ResInstance,
            value.ResName == null ? "" : EscapeString(value.ResName),
            extn);
    }

    /// <summary>
    /// Returns the standardised resource export file name equivalent to this instance.
    /// </summary>
    /// <returns>The export file name.</returns>
    public override readonly string ToString() => this;

    // Source: TGIN.cs lines 193-209
    private static string UnescapeString(string value)
    {
        for (int i = value.IndexOf('%', StringComparison.Ordinal); i >= 0 && i + 2 < value.Length; i = value.IndexOf('%', StringComparison.Ordinal))
        {
            string bad = value.Substring(i + 1, 2);
            if (!byte.TryParse(bad, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                break;
            value = value.Replace("%" + bad, new string((char)b, 1), StringComparison.Ordinal);
        }
        return value;
    }

    // Source: TGIN.cs lines 211-225 (invalid file name and path characters, and '-', become %xx)
    private static string EscapeString(string value)
    {
        foreach (char[] ary in new[] { Path.GetInvalidFileNameChars(), Path.GetInvalidPathChars(), ['-'] })
        {
            foreach (char c in ary)
            {
                string bad = new(c, 1);
                string rep = string.Format(CultureInfo.InvariantCulture, "%{0:x2}", (uint)c);
                value = value.Replace(bad, rep, StringComparison.Ordinal);
            }
        }
        return value;
    }
}
