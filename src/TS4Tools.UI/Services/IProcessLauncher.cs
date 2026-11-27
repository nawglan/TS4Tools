namespace TS4Tools.UI.Services;

/// <summary>
/// Starts external programs (text and hex editors) and waits for them to exit.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1226-1253 (EditOte starts the text editor
/// with UseShellExecute = false) and legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 259-278.
/// </remarks>
public interface IProcessLauncher
{
    /// <summary>
    /// Runs a program and completes when it exits.
    /// </summary>
    /// <param name="command">The program to run.</param>
    /// <param name="arguments">Its command line.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The exit code.</returns>
    /// <exception cref="InvalidOperationException">The program could not be started.</exception>
    Task<int> RunAsync(string command, string arguments, CancellationToken cancellationToken = default);
}

/// <summary>The operating system implementation of <see cref="IProcessLauncher"/>.</summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    /// <summary>The shared instance.</summary>
    public static ProcessLauncher Instance { get; } = new();

    public async Task<int> RunAsync(string command, string arguments, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start '{command}': {ex.Message}", ex);
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
