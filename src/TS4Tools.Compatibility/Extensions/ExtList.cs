// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 24-110

namespace s4pi.Extensions;

/// <summary>
/// Provides a look-up from resource type to resource "tag" and file extension.
/// The collection is read-only.
/// </summary>
/// <remarks>
/// The data is TS4Tools.Core's embedded copy of legacy Extensions.txt (the same data behind
/// <see cref="global::TS4Tools.Extensions.ExtList"/>), parsed with the legacy rules: ';' comment lines and
/// lines with fewer than two fields are skipped, duplicate keys are an error, and "*" is the fallback.
/// </remarks>
/// <seealso cref="TGIN"/>
public class ExtList : Dictionary<string, List<string>>
{
    // Logical name of the embedded resource in TS4Tools.Core (see TS4Tools.Core.csproj).
    private const string ResourceName = "TS4Tools.Extensions.Extensions.txt";

    private static readonly Lazy<ExtList> e = new(() => new ExtList());

    /// <summary>
    /// A look-up from resource type to resource "tag" and file extension.
    /// The collection is read-only.
    /// </summary>
    public static ExtList Ext => e.Value;

    // Source: ExtList.cs lines 55-82
    private ExtList()
    {
        using var stream = typeof(global::TS4Tools.Extensions.ExtList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found in TS4Tools.Core");
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(';')) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count < 2) continue;

            string key = parts[0];
            if (ContainsKey(key))
                throw new InvalidOperationException($"ExtList already contains entry for key '{key}'");

            parts.RemoveAt(0);
            base.Add(key, parts);
        }
    }

    /// <summary>
    /// Gets the value associated with the specified resource type.
    /// </summary>
    /// <param name="key">The resource type.</param>
    /// <returns>The tag and extension for the type; if not found, the "*" default.</returns>
    public List<string> this[uint key] => this["0x" + key.ToString("X8", CultureInfo.InvariantCulture)];

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get ("0xTTTTTTTT").</param>
    /// <returns>The value associated with the key; if not found, the "*" default.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <exception cref="InvalidOperationException">An attempt was made to set a value.</exception>
    public new List<string> this[string key]
    {
        get => TryGetValue(key, out var value) ? value : base["*"];
        set => throw new InvalidOperationException();
    }
}
