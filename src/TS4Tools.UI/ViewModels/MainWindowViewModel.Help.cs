using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Services;
using TS4Tools.UI.Views.Dialogs;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Help menu, command line handling and the work done when the window first opens.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2425-2565 (Help menu),
/// lines 401-545 (command line) and lines 201-226 (MainForm_Shown).
/// </remarks>
public partial class MainWindowViewModel
{
    /// <summary>Where the manual lives online (README of the project).</summary>
    public const string HelpUrl = "https://github.com/nawglan/TS4Tools#readme";

    /// <summary>Where the full text of the licence can be read.</summary>
    /// <remarks>Source: MainForm.cs lines 2541-2565 (HelpLicence)</remarks>
    public const string LicenceUrl = "https://www.gnu.org/licenses/gpl-3.0.html";

    /// <summary>The warranty disclaimer.</summary>
    /// <remarks>Source: MainForm.cs lines 2509-2538 (HelpWarranty)</remarks>
    public const string WarrantyText =
        "Disclaimer of Warranty.\n\n" +
        "THERE IS NO WARRANTY FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW." +
        " EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER" +
        " PARTIES PROVIDE THE PROGRAM \"AS IS\" WITHOUT WARRANTY OF ANY KIND," +
        " EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO," +
        " THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE." +
        " THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU." +
        " SHOULD THE PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING, REPAIR OR CORRECTION.\n\n\n" +
        "Limitation of Liability.\n\n" +
        "IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING WILL ANY COPYRIGHT HOLDER," +
        " OR ANY OTHER PARTY WHO MODIFIES AND/OR CONVEYS THE PROGRAM AS PERMITTED ABOVE," +
        " BE LIABLE TO YOU FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES" +
        " ARISING OUT OF THE USE OR INABILITY TO USE THE PROGRAM" +
        " (INCLUDING BUT NOT LIMITED TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE" +
        " OR LOSSES SUSTAINED BY YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER PROGRAMS)," +
        " EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.";

    /// <summary>The licence summary and the question that offers to open the full text.</summary>
    /// <remarks>Source: MainForm.cs lines 2541-2565 (HelpLicence)</remarks>
    public const string LicenceText =
        "This program is distributed under the terms of the\nGNU General Public Licence version 3.\n\n" +
        "If you wish to see the full text of the licence,\nplease visit " + LicenceUrl + ".\n\n" +
        "Do you wish to visit this site now?";

    private Action<string> _openExternal = OpenWithShell;

    /// <summary>Opens a URL or file with the operating system (replaceable for tests).</summary>
    internal Action<string> OpenExternal
    {
        get => _openExternal;
        set => _openExternal = value ?? OpenWithShell;
    }

    /// <summary>Checks for an update at start up (replaceable for tests).</summary>
    internal Func<CancellationToken, Task<UpdateCheckResult?>> StartupUpdateCheck { get; set; }
        = ct => UpdateCheckerService.Instance.DailyCheckAsync(ct);

    private static void OpenWithShell(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TS4Tools] Could not open {target}: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds what Help > Contents should open: the bundled HelpFiles/Contents.htm (preferring a
    /// folder for the UI culture, then its two letter language) or the online manual.
    /// </summary>
    /// <remarks>Source: MainForm.cs lines 2455-2470 (HelpContents)</remarks>
    internal static string ResolveHelpContents(string baseDirectory, string culture, Func<string, bool> directoryExists, Func<string, bool> fileExists)
    {
        var folder = Path.Combine(baseDirectory, "HelpFiles");
        if (!directoryExists(folder)) return HelpUrl;

        if (culture.Length > 0 && directoryExists(Path.Combine(folder, culture)))
        {
            folder = Path.Combine(folder, culture);
        }
        else if (culture.Length >= 2 && directoryExists(Path.Combine(folder, culture[..2])))
        {
            folder = Path.Combine(folder, culture[..2]);
        }

        var contents = Path.Combine(folder, "Contents.htm");
        return fileExists(contents) ? contents : HelpUrl;
    }

    /// <summary>Opens the help contents.</summary>
    [RelayCommand]
    private void HelpContents()
    {
        var target = ResolveHelpContents(
            AppContext.BaseDirectory,
            CultureInfo.CurrentUICulture.Name,
            Directory.Exists,
            File.Exists);
        OpenExternal(target);
        StatusMessage = "Opened help";
    }

    /// <summary>Shows the warranty disclaimer.</summary>
    [RelayCommand]
    private async Task HelpWarrantyAsync() => await ShowTextAsync("Warranty", WarrantyText);

    /// <summary>Shows the licence and offers to open its full text.</summary>
    [RelayCommand]
    private async Task HelpLicenceAsync()
    {
        if (await ShowTextAsync("Licence", LicenceText, yesNo: true))
        {
            OpenExternal(LicenceUrl);
        }
    }

    /// <summary>
    /// Shows a text dialog over the main window. Returns whether Yes (or OK) was chosen; false
    /// when there is no window to show it over.
    /// </summary>
    private async Task<bool> ShowTextAsync(string title, string text, bool yesNo = false)
    {
        if (GetTopLevel() is not Window window) return false;

        var dialog = new TextMessageWindow(title, text, yesNo);
        return await dialog.ShowDialog<bool>(window);
    }

    /// <summary>
    /// Handles the command line, then checks for updates.
    /// </summary>
    /// <param name="args">The program arguments.</param>
    /// <remarks>
    /// Source: MainForm.cs lines 201-226 (MainForm_Shown opens the package named on the command line)
    /// and lines 2224-2227 / UpdateChecker.cs lines 68-80 (daily update check).
    /// </remarks>
    public async Task RunStartupAsync(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Read the Helpers folder on a worker thread now, rather than on the UI thread at the
        // first resource selection.
        var helpersLoaded = HelperManager.Instance.PreloadAsync();

        var options = CommandLineOptions.Parse(args);
        await OpenFromCommandLineAsync(options);
        await helpersLoaded;
        await CheckForUpdateAtStartupAsync();
    }

    /// <summary>Opens the package named on the command line and reports command line problems.</summary>
    public async Task OpenFromCommandLineAsync(CommandLineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ShowHelp)
        {
            await ShowTextAsync("Command line options", CommandLineOptions.HelpText);
        }

        if (options.Errors.Count > 0)
        {
            var message = string.Join("\n", options.Errors);
            StatusMessage = message.Replace('\n', ' ');
            await ShowTextAsync("Command line", message);
        }

        if (options.PackagePath != null)
        {
            await LoadPackageAsync(options.PackagePath);
        }

        // Source: MainForm.cs lines 214-226 (MainForm_Shown: ImportBatch(cmdLineBatch, "-import"),
        // which creates a new package when none is open)
        if (options.ImportFiles.Count > 0)
        {
            await ImportBatchFilesAsync(options.ImportFiles, "-import");
        }
    }

    private const string UpdateDialogTitle = "TS4Tools AutoUpdate";

    /// <summary>The question shown when an update is available.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs line 105</remarks>
    public const string UpdateAvailableText = "New version available\nDo you want to visit the download site now?";

    /// <summary>
    /// Runs the automatic check if enabled; when an update is available asks whether to visit the download site.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs lines 68-115 (DailyCheck, GetUpdate)
    /// </remarks>
    internal async Task CheckForUpdateAtStartupAsync()
    {
        if (!ShellSettings.AutoCheckForUpdates) return;

        try
        {
            var result = await StartupUpdateCheck(CancellationToken.None);
            if (result == null) return;

            if (!result.Success)
            {
                // Source: UpdateChecker.cs lines 117-127 (ShowErrorMessage: "... - will try again later")
                StatusMessage = "Problem checking for update - will try again later";
                await ResourceOpsDialogs.ShowMessageAsync(
                    $"Problem checking for update - will try again later\n{result.ErrorMessage}", UpdateDialogTitle);
                return;
            }

            if (result.UpdateAvailable)
            {
                StatusMessage = $"Update available: version {result.LatestVersion}";

                // Source: UpdateChecker.cs lines 105-114 (Yes opens the download page)
                var choice = await ResourceOpsDialogs.AskChoiceAsync(UpdateAvailableText, UpdateDialogTitle,
                    ["Yes", "No"], defaultIndex: 0, cancelIndex: 1);
                if (choice == 0)
                {
                    OpenExternal(string.IsNullOrEmpty(result.DownloadUrl) ? UpdateCheckerService.DownloadPage : result.DownloadUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TS4Tools] Startup update check failed: {ex.Message}");
        }
    }
}
