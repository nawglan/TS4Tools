using TS4Tools.Package;

namespace TS4Tools.Cli.Tests;

/// <summary>Runs the CLI in-process and builds synthetic packages in a per-test scratch directory.</summary>
internal sealed class Scratch : IDisposable
{
    public Scratch()
    {
        // Under the test output folder rather than /tmp (which may be RAM-backed).
        Root = Path.Combine(AppContext.BaseDirectory, "scratch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch (IOException) { /* best effort */ }
    }
}

internal sealed record CliRun(int ExitCode, string Out, string Err);

internal static class CliRunner
{
    public static async Task<CliRun> RunAsync(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int code = await CliApp.RunAsync(args, stdout, stderr);
        return new CliRun(code, stdout.ToString(), stderr.ToString());
    }
}

/// <summary>A resource to put in a synthetic package.</summary>
internal sealed record TestResource(ResourceKey Key, byte[] Data, ushort Compression = 0x5A42, ushort Unknown2 = 1);

internal static class TestPackages
{
    public static byte[] Compressible(int size, int seed = 7) =>
        Enumerable.Range(0, size).Select(i => (byte)(i % seed)).ToArray();

    public static byte[] Random(int size, int seed)
    {
        var bytes = new byte[size];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    public static async Task<string> CreateAsync(string path, IEnumerable<TestResource> resources,
        Action<DbpfPackage>? configure = null)
    {
        using var package = DbpfPackage.CreateNew();
        configure?.Invoke(package);
        foreach (var r in resources)
        {
            var entry = package.AddResource(r.Key, r.Data, rejectDuplicates: false)!;
            package.SetCompression(entry, r.Compression);
            package.SetUnknown2(entry, r.Unknown2);
        }
        await package.SaveAsAsync(path);
        return path;
    }

    /// <summary>The settable header fields, as a tuple for equality checks.</summary>
    public static object HeaderFields(DbpfPackage p) => (p.MajorVersion, p.MinorVersion, p.UserVersionMajor,
        p.UserVersionMinor, p.Unused1, p.CreationTime, p.UpdatedTime, p.Unused2, p.Unused3, p.Unused4,
        Convert.ToHexString(p.Unused5.Span));
}
