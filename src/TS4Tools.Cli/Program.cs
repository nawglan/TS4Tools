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
            // A reader that closed the pipe early (`ts4tools list x | head`) makes the flush fail with EPIPE.
            // That is not an error of the command: keep its exit code instead of crashing with a stack trace.
            try
            {
                await stdout.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                await stdout.DisposeAsync().ConfigureAwait(false);
            }
            catch (IOException)
            {
            }
            try
            {
                await stderr.DisposeAsync().ConfigureAwait(false);
            }
            catch (IOException)
            {
            }
        }
    }
}
