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
        IOException? stdoutError = null;
        int exitCode;
        try
        {
            exitCode = await CliApp.RunAsync(args, stdout, stderr, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            // Output that fits the buffer is only written here. A write failure (disk full, I/O error) must not be
            // reported as success, and must not crash with a stack trace either: report it and exit 1.
            try
            {
                await stdout.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                stdoutError = ex;
            }
        }

        if (stdoutError != null)
        {
            try { await stderr.WriteLineAsync("error: writing output: " + stdoutError.Message).ConfigureAwait(false); }
            catch (IOException) { /* nowhere left to report it */ }
            exitCode = ExitCodes.Failure;
        }

        try
        {
            await stdout.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Already reported (or nothing left to flush).
        }
        try
        {
            await stderr.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
        return exitCode;
    }
}
