// ts4tools console entry point. No legacy equivalent (s4pe is GUI-only, legacy_references/Sims4Tools/s4pe/Program.cs).

namespace TS4Tools.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Buffered stdout: list/scan can print hundreds of thousands of lines.
        var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 1 << 16) { AutoFlush = false };
        var stderr = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true };
        try
        {
            return await CliApp.RunAsync(args, stdout, stderr, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            await stdout.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            await stdout.DisposeAsync().ConfigureAwait(false);
            await stderr.DisposeAsync().ConfigureAwait(false);
        }
    }
}
