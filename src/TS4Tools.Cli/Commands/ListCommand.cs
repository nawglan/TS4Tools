// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs (s4pe resource list: Tag, TGI, sizes, Compressed)
// Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 32-82 (Tag column, via ExtList)

using TS4Tools.Extensions;
using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary><c>ts4tools list &lt;package&gt; [--type 0xTTTTTTTT] [--json]</c>: one line per index entry.</summary>
internal static class ListCommand
{
    public static readonly CommandSpec Spec = new(
        "list",
        "ts4tools list <package> [--type 0xTTTTTTTT[,..]] [--json]",
        "List the resources of a package, in index order.",
        """
        Text output, one line per resource:
          TTTTTTTT GGGGGGGG IIIIIIIIIIIIIIII <tag> <memsize> <compression>
        where <compression> is the index label (0x5A42 ZLIB, 0x0000 stored, 0xFFFF RefPack,
        0xFFE0 deletion record).

        Options:
          --type 0xT[,0xT..]  Only list resources of these types.
          --json              Write a JSON array of {type, group, instance, tag, fileSize,
                              memSize, compressionType, unknown2, chunkOffset}.
        """,
        1, 1, ["--json"], ["--type"]);

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        HashSet<uint>? types = args.Get("--type") is { } t ? KeyFormat.ParseTypeList(t) : null;
        await using var package = await PackageFiles.OpenReadAsync(args.Positionals[0], context.CancellationToken)
            .ConfigureAwait(false);

        var entries = package.Resources.Where(e => types is null || types.Contains(e.Key.ResourceType));
        if (args.Has("--json"))
        {
            using var json = new JsonOutput(context.Out);
            var w = json.Writer;
            w.WriteStartArray();
            int n = 0;
            foreach (var e in entries)
            {
                w.WriteStartObject();
                w.WriteString("type", KeyFormat.Hex32(e.Key.ResourceType));
                w.WriteString("group", KeyFormat.Hex32(e.Key.ResourceGroup));
                w.WriteString("instance", KeyFormat.Hex64(e.Key.Instance));
                w.WriteString("tag", ExtList.GetTag(e.Key.ResourceType));
                w.WriteNumber("fileSize", e.FileSize);
                w.WriteNumber("memSize", e.MemorySize);
                w.WriteString("compressionType", KeyFormat.Hex16(e.CompressionType));
                w.WriteNumber("unknown2", DbpfPackage.GetUnknown2(e));
                w.WriteNumber("chunkOffset", e.ChunkOffset);
                w.WriteEndObject();
                if (++n % 4096 == 0) json.Drain();
            }
            w.WriteEndArray();
            return ExitCodes.Success;
        }

        foreach (var e in entries)
        {
            context.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{e.Key.ResourceType:X8} {e.Key.ResourceGroup:X8} {e.Key.Instance:X16} {ExtList.GetTag(e.Key.ResourceType),-4} {e.MemorySize} {KeyFormat.Hex16(e.CompressionType)}"));
        }
        return ExitCodes.Success;
    }
}
