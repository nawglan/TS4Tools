// Report rendering for `ts4tools scan` (console table, JSON, Markdown).
// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 32-82 (type tags shown in every table)

using TS4Tools.Extensions;

namespace TS4Tools.Cli.Scan;

internal static class ScanOutput
{
    private static readonly string[] ClassNames = ["ok", "unwrapped", "unsupported-version", "parse-error", "roundtrip-mismatch", "empty"];

    public static string ClassName(ScanClass cls) => ClassNames[(int)cls];

    private static string Tag(uint type) => ExtList.GetTag(type);

    private static IEnumerable<TypeStats> ProblemTypes(ScanReport report) =>
        report.Types.Values.Where(t => t.Problems > 0)
            .OrderByDescending(t => t.Problems).ThenBy(t => t.ResourceType);

    public static void WriteConsole(ScanReport report, TextWriter o, bool all)
    {
        o.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Scanned {report.PackagesScanned} package(s), {report.Resources:N0} resource(s), {report.Types.Count} type(s) in {report.Elapsed.TotalSeconds:F1}s{(report.Roundtrip ? " (round-trip checked)" : "")}"));
        o.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  ok {report.Count(ScanClass.Ok):N0}, unwrapped {report.Count(ScanClass.Unwrapped):N0}, unsupported-version {report.Count(ScanClass.UnsupportedVersion):N0}, parse-error {report.Count(ScanClass.ParseError):N0}, roundtrip-mismatch {report.Count(ScanClass.RoundtripMismatch):N0}, empty {report.Count(ScanClass.Empty):N0}"));
        o.WriteLine();

        var rows = (all ? report.Types.Values.OrderBy(t => t.ResourceType) : ProblemTypes(report)).ToList();
        if (rows.Count > 0)
        {
            o.WriteLine($"{"TYPE",-10} {"TAG",-5} {"WRAPPER",-28} {"TOTAL",9} {"OK",9} {"UNWRAP",9} {"UNSUPV",8} {"PARSE",8} {"RTRIP",8} {"EMPTY",8}");
            foreach (var t in rows)
            {
                string wrapper = t.Wrapper.Length > 28 ? t.Wrapper[..27] + "~" : t.Wrapper;
                o.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{KeyFormat.Hex32(t.ResourceType),-10} {Tag(t.ResourceType),-5} {wrapper,-28} {t.Total,9} {t.Counts[0],9} {t.Counts[1],9} {t.Counts[2],8} {t.Counts[3],8} {t.Counts[4],8} {t.Counts[5],8}"));
                foreach (var ((resource, version, max), count) in t.Versions.OrderBy(v => v.Key.Version))
                    o.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"{"",17}{resource} version 0x{version:X} (max 0x{max:X}): {count}"));
            }
        }
        int okTypes = report.Types.Values.Count(t => t.Problems == 0);
        if (!all && okTypes > 0)
            o.WriteLine($"({okTypes} type(s) fully ok not shown; use --all)");

        if (report.FailedPackages.Count > 0)
        {
            o.WriteLine();
            o.WriteLine($"Packages that failed to open: {report.FailedPackages.Count}");
            foreach (var f in report.FailedPackages.Take(20))
                o.WriteLine($"  {f.Package}: {f.Message}");
            if (report.FailedPackages.Count > 20)
                o.WriteLine($"  ... {report.FailedPackages.Count - 20} more");
        }
    }

    public static async Task WriteJsonAsync(ScanReport report, string path, CancellationToken ct)
    {
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        w.WriteStartObject();
        w.WriteNumber("formatVersion", 1);
        w.WriteString("generatedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        w.WriteStartArray("roots");
        foreach (var r in report.Roots) w.WriteStringValue(r);
        w.WriteEndArray();
        w.WriteBoolean("roundtrip", report.Roundtrip);
        if (report.TypeFilter != null)
        {
            w.WriteStartArray("typeFilter");
            foreach (var t in report.TypeFilter.Order()) w.WriteStringValue(KeyFormat.Hex32(t));
            w.WriteEndArray();
        }
        w.WriteNumber("packagesScanned", report.PackagesScanned);
        w.WriteNumber("resources", report.Resources);
        w.WriteNumber("elapsedSeconds", Math.Round(report.Elapsed.TotalSeconds, 1));
        w.WriteStartObject("totals");
        foreach (ScanClass c in Enum.GetValues<ScanClass>()) w.WriteNumber(ClassName(c), report.Count(c));
        w.WriteEndObject();

        w.WriteStartArray("failedPackages");
        foreach (var f in report.FailedPackages)
        {
            w.WriteStartObject();
            w.WriteString("package", f.Package);
            w.WriteString("message", f.Message);
            w.WriteEndObject();
        }
        w.WriteEndArray();

        w.WriteStartArray("types");
        foreach (var t in report.Types.Values)
        {
            w.WriteStartObject();
            w.WriteString("type", KeyFormat.Hex32(t.ResourceType));
            w.WriteString("tag", Tag(t.ResourceType));
            w.WriteString("wrapper", t.Wrapper);
            w.WriteNumber("total", t.Total);
            w.WriteStartObject("counts");
            foreach (ScanClass c in Enum.GetValues<ScanClass>()) w.WriteNumber(ClassName(c), t.Counts[(int)c]);
            w.WriteEndObject();
            w.WriteStartArray("unsupportedVersions");
            foreach (var ((resource, version, max), count) in t.Versions.OrderBy(v => v.Key.Version))
            {
                w.WriteStartObject();
                w.WriteString("resource", resource);
                w.WriteString("version", "0x" + version.ToString("X", CultureInfo.InvariantCulture));
                w.WriteString("maxSupported", "0x" + max.ToString("X", CultureInfo.InvariantCulture));
                w.WriteNumber("count", count);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartArray("parseErrors");
            foreach (var (message, count) in t.Messages.OrderByDescending(m => m.Value).ThenBy(m => m.Key, StringComparer.Ordinal))
            {
                w.WriteStartObject();
                w.WriteString("message", message);
                w.WriteNumber("count", count);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartObject("samples");
            foreach (ScanClass c in Enum.GetValues<ScanClass>())
            {
                if (c is ScanClass.Ok or ScanClass.Empty || t.Samples[(int)c].Count == 0) continue;
                w.WriteStartArray(ClassName(c));
                foreach (var s in t.Samples[(int)c])
                {
                    w.WriteStartObject();
                    w.WriteString("key", KeyFormat.Compact(s.Key));
                    w.WriteString("package", s.Package);
                    if (s.Detail.Length > 0) w.WriteString("detail", s.Detail);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            w.WriteEndObject();
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteEndObject();
        await w.FlushAsync(ct).ConfigureAwait(false);
    }

    private static string Md(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string N(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

    public static async Task WriteMarkdownAsync(ScanReport report, string path, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Resource wrapper coverage scan");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"Generated by `ts4tools scan{(report.Roundtrip ? " --roundtrip" : "")}` on {DateTime.UtcNow:yyyy-MM-dd} (UTC) over: {string.Join(", ", report.Roots.Select(r => $"`{Md(r)}`"))}.");
        sb.AppendLine("Package paths are relative to the scanned folder. Regenerate with `scripts/scan-unsupported.sh`.");
        sb.AppendLine();
        sb.AppendLine("Classes: **ok** (a TS4Tools wrapper parsed it" + (report.Roundtrip ? " and re-serialised it byte-identically" : "") +
                      "), **unwrapped** (no TS4Tools wrapper for the type; handled as raw bytes), **unsupported-version** " +
                      "(the wrapper refused a newer format version), **parse-error** (the wrapper threw), " +
                      "**roundtrip-mismatch** (parsed, but the re-serialised bytes differ" + (report.Roundtrip ? "" : "; not checked in this run") + "), **empty** (zero-length resource such as a 0xFFE0 deletion record in a Delta package; nothing to parse, not a problem).");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| | Count |");
        sb.AppendLine("|---|---:|");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Packages scanned | {N(report.PackagesScanned)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Packages that failed to open | {N(report.FailedPackages.Count)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Resources | {N(report.Resources)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Resource types | {N(report.Types.Count)} |");
        foreach (ScanClass c in Enum.GetValues<ScanClass>())
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {ClassName(c)} | {N(report.Count(c))} |");
        sb.AppendLine();

        var problems = ProblemTypes(report).Where(t => t.Counts[(int)ScanClass.Unwrapped] < t.Problems || t.Counts[(int)ScanClass.Unwrapped] == 0).ToList();
        sb.AppendLine("## Wrapped types with problems");
        sb.AppendLine();
        if (problems.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Type | Tag | Wrapper | Total | ok | unsupported-version | parse-error | roundtrip-mismatch | empty |");
            sb.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (var t in problems)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| `{KeyFormat.Hex32(t.ResourceType)}` | {Md(Tag(t.ResourceType))} | {Md(t.Wrapper)} | {N(t.Total)} | {N(t.Counts[0])} | {N(t.Counts[2])} | {N(t.Counts[3])} | {N(t.Counts[4])} | {N(t.Counts[5])} |");
        }
        sb.AppendLine();

        var versioned = report.Types.Values.Where(t => t.Versions.Count > 0).ToList();
        if (versioned.Count > 0)
        {
            sb.AppendLine("## Unsupported versions");
            sb.AppendLine();
            sb.AppendLine("| Type | Tag | Wrapper | Version found | Max supported | Count |");
            sb.AppendLine("|---|---|---|---:|---:|---:|");
            foreach (var t in versioned)
                foreach (var ((resource, version, max), count) in t.Versions.OrderBy(v => v.Key.Version))
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"| `{KeyFormat.Hex32(t.ResourceType)}` | {Md(Tag(t.ResourceType))} | {Md(resource)} | 0x{version:X} ({version}) | 0x{max:X} ({max}) | {N(count)} |");
            sb.AppendLine();
        }

        var details = problems.ToList();
        if (details.Count > 0)
        {
            sb.AppendLine("## Problem details");
            sb.AppendLine();
            foreach (var t in details)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"### `{KeyFormat.Hex32(t.ResourceType)}` {Md(Tag(t.ResourceType))} ({Md(t.Wrapper)})");
                sb.AppendLine();
                if (t.Messages.Count > 0)
                {
                    sb.AppendLine("Parse errors (numbers shown as `#`):");
                    sb.AppendLine();
                    foreach (var (message, count) in t.Messages.OrderByDescending(m => m.Value).ThenBy(m => m.Key, StringComparer.Ordinal))
                        sb.AppendLine(CultureInfo.InvariantCulture, $"- {N(count)} × `{Md(message)}`");
                    sb.AppendLine();
                }
                sb.AppendLine("Samples:");
                sb.AppendLine();
                foreach (ScanClass c in new[] { ScanClass.UnsupportedVersion, ScanClass.ParseError, ScanClass.RoundtripMismatch })
                    foreach (var s in t.Samples[(int)c])
                        sb.AppendLine(CultureInfo.InvariantCulture,
                            $"- {ClassName(c)}: `{KeyFormat.Compact(s.Key)}` in `{Md(s.Package)}`{(s.Detail.Length > 0 ? " — " + Md(s.Detail) : "")}");
                sb.AppendLine();
            }
        }

        var unwrapped = report.Types.Values.Where(t => t.Counts[(int)ScanClass.Unwrapped] > 0)
            .OrderByDescending(t => t.Counts[(int)ScanClass.Unwrapped]).ThenBy(t => t.ResourceType).ToList();
        sb.AppendLine("## Unwrapped types (no TS4Tools wrapper)");
        sb.AppendLine();
        if (unwrapped.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Type | Tag | Count | Sample |");
            sb.AppendLine("|---|---|---:|---|");
            foreach (var t in unwrapped)
            {
                var s = t.Samples[(int)ScanClass.Unwrapped].FirstOrDefault();
                string sample = s is null ? "" : $"`{KeyFormat.Compact(s.Key)}` in `{Md(s.Package)}`";
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| `{KeyFormat.Hex32(t.ResourceType)}` | {Md(Tag(t.ResourceType))} | {N(t.Counts[(int)ScanClass.Unwrapped])} | {sample} |");
            }
        }
        sb.AppendLine();

        var okTypes = report.Types.Values.Where(t => t.Problems == 0).ToList();
        sb.AppendLine("## Fully supported types");
        sb.AppendLine();
        if (okTypes.Count == 0)
        {
            sb.AppendLine("None.");
        }
        else
        {
            sb.AppendLine("| Type | Tag | Wrapper | Count |");
            sb.AppendLine("|---|---|---|---:|");
            foreach (var t in okTypes)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| `{KeyFormat.Hex32(t.ResourceType)}` | {Md(Tag(t.ResourceType))} | {Md(t.Wrapper)} | {N(t.Total)} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Packages that failed to open");
        sb.AppendLine();
        if (report.FailedPackages.Count == 0)
            sb.AppendLine("None.");
        foreach (var f in report.FailedPackages)
            sb.AppendLine(CultureInfo.InvariantCulture, $"- `{Md(f.Package)}`: {Md(f.Message)}");

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false), ct).ConfigureAwait(false);
    }
}
