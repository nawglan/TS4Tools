using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TS4Tools.Package;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Search dialog for finding resources in a package.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs
/// </remarks>
public partial class SearchWindow : Window, IDisposable
{
    private readonly DbpfPackage _package;
    private readonly IReadOnlyList<ResourceItemViewModel> _resources;
    private readonly List<ResourceItemViewModel> _results = [];
    private CancellationTokenSource? _searchCts;
    private bool _isSearching;
    private bool _disposed;

    /// <summary>
    /// Gets the selected resource when user clicks "Go to".
    /// </summary>
    public ResourceItemViewModel? SelectedResult { get; private set; }

    public SearchWindow()
    {
        InitializeComponent();
        _package = null!;
        _resources = [];
    }

    public SearchWindow(DbpfPackage package, IReadOnlyList<ResourceItemViewModel> resources)
    {
        InitializeComponent();
        _package = package;
        _resources = resources;

        SearchButton.Click += SearchButton_Click;
        GoToButton.Click += GoToButton_Click;
        CopyKeysButton.Click += CopyKeysButton_Click;
        ResultsListBox.SelectionChanged += ResultsListBox_SelectionChanged;
        ResultsListBox.DoubleTapped += ResultsListBox_DoubleTapped;
        SearchTextBox.KeyDown += SearchTextBox_KeyDown;
    }

    private void SearchTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            SearchButton_Click(sender, e);
        }
    }

    private async void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isSearching)
        {
            // Cancel current search
            _searchCts?.Cancel();
            return;
        }

        var searchText = SearchTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            StatusText.Text = "Enter search text";
            return;
        }

        _isSearching = true;
        SearchButton.Content = "_Stop";
        _searchCts = new CancellationTokenSource();
        _results.Clear();
        ResultsListBox.ItemsSource = null;

        try
        {
            var searchMode = SearchModeCombo.SelectedIndex;
            var useRegex = UseRegexCheckBox.IsChecked == true;
            var filterByType = FilterByTypeCheckBox.IsChecked == true;
            uint? typeFilter = null;

            if (filterByType && !string.IsNullOrEmpty(TypeFilterTextBox.Text))
            {
                if (uint.TryParse(TypeFilterTextBox.Text.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var parsedType))
                {
                    typeFilter = parsedType;
                }
            }

            StatusText.Text = "Searching...";

            await Task.Run(async () =>
            {
                var matches = new List<ResourceItemViewModel>();
                var token = _searchCts.Token;
                var total = _resources.Count;
                var processed = 0;

                System.Text.RegularExpressions.Regex? regex = null;
                if (useRegex)
                {
                    try
                    {
                        regex = new System.Text.RegularExpressions.Regex(searchText, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = "Invalid regex pattern");
                        return;
                    }
                }

                foreach (var resource in _resources)
                {
                    token.ThrowIfCancellationRequested();

                    // Apply type filter
                    if (typeFilter.HasValue && resource.Key.ResourceType != typeFilter.Value)
                    {
                        processed++;
                        continue;
                    }

                    bool isMatch = searchMode switch
                    {
                        0 => MatchResourceKey(resource, searchText, regex),
                        1 => MatchResourceName(resource, searchText, regex),
                        2 => await MatchContentAsync(resource, searchText, token),
                        _ => false
                    };

                    if (isMatch)
                    {
                        matches.Add(resource);
                    }

                    processed++;
                    if (processed % 100 == 0)
                    {
                        var pct = processed * 100 / total;
                        await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = $"Searching... {pct}% ({matches.Count} found)");
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _results.AddRange(matches);
                    ResultsListBox.ItemsSource = _results;
                    ResultsCount.Text = $"{_results.Count} result{(_results.Count != 1 ? "s" : "")}";
                    StatusText.Text = $"Search complete";
                });
            }, _searchCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Search cancelled";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _isSearching = false;
            SearchButton.Content = "_Search";
            _searchCts?.Dispose();
            _searchCts = null;
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs lines 64-89
    private static bool MatchResourceKey(ResourceItemViewModel resource, string searchText, System.Text.RegularExpressions.Regex? regex)
    {
        var displayKey = resource.DisplayKey;
        var typeName = resource.TypeName;

        if (regex != null)
        {
            return regex.IsMatch(displayKey) || regex.IsMatch(typeName);
        }

        return displayKey.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchResourceName(ResourceItemViewModel resource, string searchText, System.Text.RegularExpressions.Regex? regex)
    {
        if (string.IsNullOrEmpty(resource.InstanceName)) return false;

        if (regex != null)
        {
            return regex.IsMatch(resource.InstanceName);
        }

        return resource.InstanceName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    // Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs lines 279-291 (search method)
    private async Task<bool> MatchContentAsync(ResourceItemViewModel resource, string hexPattern, CancellationToken token)
    {
        // Parse hex pattern
        var pattern = ParseHexPattern(hexPattern);
        if (pattern == null || pattern.Length == 0) return false;

        var entry = _package.Find(resource.Key);
        if (entry == null) return false;

        try
        {
            var data = await _package.GetResourceDataAsync(entry, token);
            var bytes = data.Span;

            // Boyer-Moore-like simple search
            for (int i = 0; i <= bytes.Length - pattern.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (bytes[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
        }
        catch
        {
            // Ignore read errors
        }

        return false;
    }

    // Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs lines 102-146
    private static byte[]? ParseHexPattern(string input)
    {
        input = input.Trim();

        // Handle quoted string
        if (input.StartsWith('"') && input.EndsWith('"'))
        {
            var str = input.Trim('"');
            var bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                bytes[i] = (byte)str[i];
            return bytes;
        }

        // Handle space-separated hex bytes
        if (input.Contains(' '))
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                    return null;
            }
            return bytes;
        }

        // Handle 0x prefix or plain hex
        var hex = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? input[2..]
            : input;

        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        try
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
        catch
        {
            return null;
        }
    }

    private void ResultsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var hasSelection = ResultsListBox.SelectedItems?.Count > 0;
        GoToButton.IsEnabled = ResultsListBox.SelectedItem != null;
        CopyKeysButton.IsEnabled = hasSelection;
    }

    private void ResultsListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        GoToButton_Click(sender, e);
    }

    private void GoToButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ResultsListBox.SelectedItem is ResourceItemViewModel selected)
        {
            SelectedResult = selected;
            Close(true);
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs lines 334-339
    private async void CopyKeysButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ResultsListBox.SelectedItems == null) return;

        var keys = ResultsListBox.SelectedItems
            .OfType<ResourceItemViewModel>()
            .Select(r => $"S4_{r.Key.ResourceType:X8}_{r.Key.ResourceGroup:X8}_{r.Key.Instance:X16}");

        var text = string.Join(Environment.NewLine, keys);

        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
            StatusText.Text = $"Copied {ResultsListBox.SelectedItems.Count} key(s)";
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _searchCts?.Cancel();
        Close(false);
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
        GC.SuppressFinalize(this);
    }
}
