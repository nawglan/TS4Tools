// Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 263-338 (header fields shown)
// Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs (s4pe "Package info" panel, the GUI equivalent)

using TS4Tools.Package;

namespace TS4Tools.Cli.Commands;

/// <summary><c>ts4tools info &lt;package&gt; [--json]</c>: header fields and resource count.</summary>
internal static class InfoCommand
{
    public static readonly CommandSpec Spec = new(
        "info",
        "ts4tools info <package> [--json]",
        "Show the DBPF header fields and resource count of a package.",
        """
        Options:
          --json    Write the header as a JSON object instead of text.
        """,
        1, 1, ["--json"], []);

    public static async Task<int> RunAsync(ParsedArgs args, CliContext context)
    {
        string path = args.Positionals[0];
        await using var package = await PackageFiles.OpenReadAsync(path, context.CancellationToken).ConfigureAwait(false);
        var h = ManifestHeader.From(package);

        if (args.Has("--json"))
        {
            using var json = new JsonOutput(context.Out);
            var w = json.Writer;
            w.WriteStartObject();
            w.WriteString("file", Path.GetFileName(path));
            w.WriteNumber("fileLength", new FileInfo(path).Length);
            w.WriteString("magic", h.Magic);
            w.WriteNumber("major", h.Major);
            w.WriteNumber("minor", h.Minor);
            w.WriteNumber("userVersionMajor", h.UserVersionMajor);
            w.WriteNumber("userVersionMinor", h.UserVersionMinor);
            w.WriteNumber("unused1", h.Unused1);
            w.WriteNumber("creationTime", h.CreationTime);
            w.WriteNumber("updatedTime", h.UpdatedTime);
            w.WriteNumber("unused2", h.Unused2);
            w.WriteNumber("indexCount", h.IndexCount);
            w.WriteNumber("indexRecordPositionLow", h.IndexRecordPositionLow);
            w.WriteNumber("indexSize", h.IndexSize);
            w.WriteNumber("unused3", h.Unused3);
            w.WriteNumber("unused4", h.Unused4);
            w.WriteNumber("indexPosition", h.IndexPosition);
            w.WriteString("unused5", h.Unused5);
            w.WriteString("indexType", h.IndexType);
            w.WriteNumber("resourceCount", package.ResourceCount);
            w.WriteEndObject();
            return ExitCodes.Success;
        }

        var o = context.Out;
        o.WriteLine($"File:              {Path.GetFileName(path)} ({new FileInfo(path).Length:N0} bytes)");
        o.WriteLine($"Magic:             {h.Magic}");
        o.WriteLine($"Version:           {h.Major}.{h.Minor}");
        o.WriteLine($"User version:      {h.UserVersionMajor}.{h.UserVersionMinor}");
        o.WriteLine($"Creation time:     {h.CreationTime}");
        o.WriteLine($"Updated time:      {h.UpdatedTime}");
        o.WriteLine($"Unused1/2/3/4:     {h.Unused1} / {h.Unused2} / {h.Unused3} / {h.Unused4}");
        o.WriteLine($"Unused5:           {h.Unused5}");
        o.WriteLine($"Index type:        {h.IndexType}");
        o.WriteLine($"Index count:       {h.IndexCount}");
        o.WriteLine($"Index size:        {h.IndexSize}");
        o.WriteLine($"Index position:    {h.IndexPosition} (low field {h.IndexRecordPositionLow})");
        o.WriteLine($"Resources:         {package.ResourceCount}");
        return ExitCodes.Success;
    }
}

/// <summary>Opening packages with friendly errors.</summary>
internal static class PackageFiles
{
    /// <exception cref="CliException">The file does not exist.</exception>
    public static async Task<DbpfPackage> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new CliException($"package not found: {path}");
        try
        {
            return await DbpfPackage.OpenAsync(path, readWrite: false, cancellationToken).ConfigureAwait(false);
        }
        catch (PackageFormatException ex)
        {
            throw new CliException($"{path}: not a valid package: {ex.Message}");
        }
    }
}

/// <summary>An indented UTF-8 JSON writer onto a <see cref="TextWriter"/>.</summary>
internal sealed class JsonOutput : IDisposable
{
    private readonly TextWriter _target;
    private readonly MemoryStream _buffer = new();

    public JsonOutput(TextWriter target)
    {
        _target = target;
        Writer = new Utf8JsonWriter(_buffer, new JsonWriterOptions { Indented = true });
    }

    public Utf8JsonWriter Writer { get; }

    /// <summary>Moves what has been written so far to the target, keeping memory bounded for long outputs.</summary>
    public void Drain()
    {
        Writer.Flush();
        _target.Write(Encoding.UTF8.GetString(_buffer.GetBuffer(), 0, (int)_buffer.Length));
        _buffer.SetLength(0);
    }

    public void Dispose()
    {
        Drain();
        _target.WriteLine();
        Writer.Dispose();
        _buffer.Dispose();
    }
}
