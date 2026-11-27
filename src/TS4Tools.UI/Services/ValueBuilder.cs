// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs lines 264-445 (ValueBuilderFields, ValueBuilder, ToString)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2711-2725 and 2947-2983 (HasStringValueContentField / GetPreviewControl)

using TS4Tools.UI.Views.Controls;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.Services;

/// <summary>
/// Builds the "Value" text of a wrapper: every public property, recursively, in the style of legacy
/// <c>AApiVersionedFields.ValueBuilder</c>.
/// </summary>
/// <remarks>
/// <para>Legacy wrappers expose a string <c>Value</c> content field built by <c>ValueBuilder</c>; s4pe shows it as the
/// preview for every wrapper without a built-in control. The format is kept: simple fields are
/// <c>Name: value</c>, nested objects and lists get a <c>--- Type: Name (0xCount) ---</c> header with their
/// contents indented three spaces and a closing <c>---</c>; list elements are <c>[i]: value</c> with a hex index
/// padded to the width of the count. Integers are shown as hex like <c>TypedValue</c> with format "X".</para>
/// <para>Differences: the modern wrappers are plain objects, so the field list is their public properties (minus
/// raw-data members such as <c>Data</c> that legacy's ban lists remove), and output is bounded (depth, elements per
/// list, total length) because some wrappers hold very large vertex/index buffers. Catalog tag values are followed
/// by their name from <see cref="CatalogTagRegistry"/> (legacy wraps them in CatalogTag whose ToString does this).</para>
/// </remarks>
public static class ValueBuilder
{
    /// <summary>Maximum nesting depth.</summary>
    public const int MaxDepth = 12;

    /// <summary>Maximum elements written per list.</summary>
    public const int MaxListElements = 4096;

    /// <summary>Maximum output length in characters.</summary>
    public const int MaxLength = 4 * 1024 * 1024;

    // Source: AApiVersionedFields.cs lines 119-129 (banlist: AApiVersionedFields' own properties),
    // 264 (valueBuilderBanlist: Value, Stream, AsBytes) and 266-267 (dictionaryBanlist).
    private static readonly HashSet<string> BannedFields = new(StringComparer.Ordinal)
    {
        "Value", "Stream", "AsBytes", "ContentFields", "RecommendedApiVersion", "RequestedApiVersion",
        "Data", "IsDirty", "Key",
    };

    private static readonly HashSet<string> CollectionFields = new(StringComparer.Ordinal)
    {
        "Keys", "Values", "Count", "Capacity", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot",
    };

    /// <summary>
    /// Builds the value text for <paramref name="obj"/>.
    /// </summary>
    public static string Build(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        var sb = new StringBuilder();
        var visiting = new HashSet<object>(ReferenceEqualityComparer.Instance);
        AppendObject(sb, obj, 0, visiting);
        if (sb.Length > MaxLength)
        {
            sb.Length = MaxLength;
            sb.Append("\n... (output truncated)");
        }
        return sb.ToString().Trim('\n');
    }

    private static void AppendObject(StringBuilder sb, object obj, int depth, HashSet<object> visiting)
    {
        if (sb.Length > MaxLength) return;
        bool tracked = !obj.GetType().IsValueType && visiting.Add(obj);
        try
        {
            var type = obj.GetType();
            bool isList = PropertyListAdapter.IsListType(type);
            foreach (var prop in GetFields(type, isList))
            {
                if (sb.Length > MaxLength) return;
                object? value;
                try
                {
                    value = prop.GetValue(obj);
                }
                catch (TargetInvocationException ex)
                {
                    sb.Append('\n').Append(prop.Name).Append(": (Exception: ").Append(ex.InnerException?.Message ?? ex.Message).Append(')');
                    continue;
                }

                AppendField(sb, prop.Name, value, depth, visiting);
            }

            // Source: AApiVersionedFields.cs lines 417-430 (IDictionary entries) - and lists of this object itself.
            if (isList && depth == 0)
            {
                AppendList(sb, type.Name, "(entries)", obj, depth, visiting);
            }
        }
        finally
        {
            if (tracked) visiting.Remove(obj);
        }
    }

    private static IEnumerable<PropertyInfo> GetFields(Type type, bool isList) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Where(p => !BannedFields.Contains(p.Name))
            .Where(p => !(isList && CollectionFields.Contains(p.Name)))
            .Where(p => !typeof(Stream).IsAssignableFrom(p.PropertyType))
            .OrderBy(p => p.MetadataToken);

    private static void AppendField(StringBuilder sb, string name, object? value, int depth, HashSet<object> visiting)
    {
        if (value == null)
        {
            sb.Append('\n').Append(name).Append(": (null)");
            return;
        }

        var type = value.GetType();
        if (TryFormatSimple(value, IsTagName(name), out var text))
        {
            sb.Append('\n').Append(name).Append(": ").Append(text);
            return;
        }

        if (PropertyListAdapter.IsListType(type))
        {
            AppendList(sb, type.Name, name, value, depth, visiting);
            return;
        }

        if (depth >= MaxDepth || (!type.IsValueType && visiting.Contains(value)))
        {
            sb.Append('\n').Append(name).Append(": ").Append(value);
            return;
        }

        // Nested object. Source: AApiVersionedFields.cs lines 304-321 - multi-line values get a header block.
        var nested = new StringBuilder();
        AppendObject(nested, value, depth + 1, visiting);
        var elem = nested.ToString().Trim('\n');
        if (elem.Length == 0)
        {
            sb.Append('\n').Append(name).Append(": ").Append(value);
        }
        else if (elem.Contains('\n', StringComparison.Ordinal))
        {
            sb.Append("\n--- ").Append(type.Name).Append(": ").Append(name).Append(" ---\n   ")
              .Append(elem.Replace("\n", "\n   ", StringComparison.Ordinal).TrimEnd()).Append("\n---");
        }
        else
        {
            sb.Append('\n').Append(name).Append(": ").Append(elem);
        }
    }

    private static void AppendList(StringBuilder sb, string typeName, string name, object list, int depth, HashSet<object> visiting)
    {
        var adapter = PropertyListAdapter.TryCreate(list, list.GetType(), null);
        int count = adapter?.Count ?? 0;

        // Source: AApiVersionedFields.cs line 298 (headerFormat) and 325/364-365 (element formats)
        sb.Append(CultureInfo.InvariantCulture, $"\n--- {typeName}: {name} (0x{count:X}) ---");
        if (adapter == null)
        {
            sb.Append("\n---");
            return;
        }

        var width = count.ToString("X", CultureInfo.InvariantCulture).Length;
        bool tags = IsTagName(name) || list is CatalogTagList;
        int shown = Math.Min(count, MaxListElements);
        for (int i = 0; i < shown && sb.Length <= MaxLength; i++)
        {
            var index = i.ToString("X" + width, CultureInfo.InvariantCulture);
            object? element;
            try
            {
                element = adapter[i];
            }
            catch (Exception ex) when (ex is TargetInvocationException or ArgumentException or IndexOutOfRangeException)
            {
                sb.Append("\n   [").Append(index).Append("]: (Exception: ").Append(ex.InnerException?.Message ?? ex.Message).Append(')');
                continue;
            }

            if (element == null)
            {
                sb.Append("\n   [").Append(index).Append("]: (null)");
                continue;
            }

            if (TryFormatSimple(element, tags, out var text))
            {
                sb.Append("\n   [").Append(index).Append("]: ").Append(text);
                continue;
            }

            if (depth >= MaxDepth)
            {
                sb.Append("\n   [").Append(index).Append("]: ").Append(element);
                continue;
            }

            var nested = new StringBuilder();
            if (PropertyListAdapter.IsListType(element.GetType()))
                AppendList(nested, element.GetType().Name, $"[{index}]", element, depth + 1, visiting);
            else
                AppendObject(nested, element, depth + 1, visiting);
            var elem = nested.ToString().Trim('\n');
            if (elem.Contains('\n', StringComparison.Ordinal))
            {
                // Source: AApiVersionedFields.cs line 364 (fmtLong "--- name[i] ---")
                sb.Append("\n--- ").Append(name).Append('[').Append(index).Append("] ---\n   ")
                  .Append(elem.Replace("\n", "\n   ", StringComparison.Ordinal).TrimEnd());
            }
            else
            {
                sb.Append("\n   [").Append(index).Append("]: ").Append(elem.Length == 0 ? element.ToString() : elem);
            }
        }

        if (count > shown)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n   ... ({count - shown} more)");
        }
        sb.Append("\n---");
    }

    /// <summary>
    /// Formats primitives, enums, strings, TGI values, byte data and value types with their own ToString.
    /// </summary>
    internal static bool TryFormatSimple(object value, bool isTag, [NotNullWhen(true)] out string? text)
    {
        switch (value)
        {
            case string s:
                text = s;
                return true;
            case bool b:
                text = b.ToString(CultureInfo.InvariantCulture);
                return true;
            case float f:
                text = f.ToString("R", CultureInfo.InvariantCulture);
                return true;
            case double d:
                text = d.ToString("R", CultureInfo.InvariantCulture);
                return true;
            case decimal m:
                text = m.ToString(CultureInfo.InvariantCulture);
                return true;
            case char c:
                text = c.ToString();
                return true;
            case byte[] bytes:
                text = FormatBytes(bytes);
                return true;
            case ReadOnlyMemory<byte> rom:
                text = FormatBytes(rom.Span);
                return true;
            case Memory<byte> mem:
                text = FormatBytes(mem.Span);
                return true;
            case Enum e:
                // Source: TypedValue with "X" format shows enums as Name (0xHEX)
                text = $"{e} (0x{Convert.ToUInt64(e, CultureInfo.InvariantCulture):X})";
                return true;
        }

        var hex = value switch
        {
            uint v => $"0x{v:X8}",
            ulong v => $"0x{v:X16}",
            ushort v => $"0x{v:X4}",
            byte v => $"0x{v:X2}",
            int v => $"0x{v:X8}",
            long v => $"0x{v:X16}",
            short v => $"0x{v:X4}",
            sbyte v => $"0x{v:X2}",
            _ => null
        };
        if (hex != null)
        {
            if (isTag && value is uint or ushort or byte
                && CatalogTagRegistry.GetTagName(Convert.ToUInt32(value, CultureInfo.InvariantCulture)) is { } tagName)
                hex += $" ({tagName})";
            text = hex;
            return true;
        }

        var type = value.GetType();
        if (TgiParts.TryGet(type, out var parts))
        {
            text = parts.Format(value);
            return true;
        }

        // Value types (vectors, bounding boxes, ...) that format themselves.
        if (type.IsValueType && type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType == type)
        {
            text = value.ToString() ?? string.Empty;
            return true;
        }

        text = null;
        return false;
    }

    private static string FormatBytes(ReadOnlySpan<byte> bytes)
    {
        const int preview = 16;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"({bytes.Length} bytes)");
        if (bytes.Length > 0)
        {
            sb.Append(' ');
            sb.Append(Convert.ToHexString(bytes[..Math.Min(preview, bytes.Length)]));
            if (bytes.Length > preview) sb.Append("...");
        }
        return sb.ToString();
    }

    private static bool IsTagName(string name) =>
        name.Contains("Tag", StringComparison.Ordinal)
        && !name.Contains("Tgi", StringComparison.OrdinalIgnoreCase)
        && !name.EndsWith("Count", StringComparison.Ordinal)
        && !name.EndsWith("Version", StringComparison.Ordinal);
}
