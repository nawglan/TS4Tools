using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// One field of the resource filter: an entry box, the applied (active) filter and a checkbox
/// saying whether the field takes part in filtering.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 28-117.
/// Value is the regular expression being entered, Filter is the one currently applied; Set copies
/// Value to Filter (or "match anything" when unchecked) and Revise copies Filter back to Value.
/// </remarks>
public sealed partial class FilterFieldViewModel : ObservableObject
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private Regex _value = new(string.Empty, RegexOptions.None, MatchTimeout);
    private Regex _filter = new(".*", RegexOptions.None, MatchTimeout);
    private bool _entryEdited;
    private bool _settingEntry;

    public FilterFieldViewModel(string name, bool exact)
    {
        Name = name;
        Exact = exact;
    }

    /// <summary>The column this field filters.</summary>
    public string Name { get; }

    /// <summary>Whether the entry must match the whole text (rather than a part of it).</summary>
    public bool Exact { get; }

    /// <summary>Whether this field is used when the filter is set.</summary>
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>The text of the entry box (a regular expression).</summary>
    [ObservableProperty]
    private string _entryText = string.Empty;

    /// <summary>The currently applied filter, as displayed.</summary>
    [ObservableProperty]
    private string _appliedText = string.Empty;

    /// <summary>The applied filter expression.</summary>
    public Regex Filter => _filter;

    /// <summary>The expression in the entry box (after it has been committed).</summary>
    public Regex Value => _value;

    partial void OnEntryTextChanged(string value)
    {
        if (!_settingEntry) _entryEdited = true;
    }

    /// <summary>
    /// Turns the entry text into the value expression, anchoring it as legacy does when focus leaves
    /// the entry box. An invalid expression is replaced by the previous value.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 106-116 (tbEntry_Leave)</remarks>
    public void CommitEntry()
    {
        if (!_entryEdited) return;
        _entryEdited = false;

        if (EntryText.Length == 0)
        {
            _value = new Regex(string.Empty, RegexOptions.None, MatchTimeout);
            return;
        }

        try
        {
            var core = EntryText.TrimStart('^').TrimEnd('$');
            var any = Exact ? string.Empty : ".*";
            SetValue(new Regex($"^{any}{core}{any}$", RegexOptions.IgnoreCase, MatchTimeout));
        }
        catch (ArgumentException)
        {
            SetValue(_value);
        }
    }

    /// <summary>Sets the entry from a regular expression (not anchored by this call).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 59-73 (Value setter)</remarks>
    public void SetValue(Regex value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        _settingEntry = true;
        try
        {
            EntryText = Display(value);
        }
        finally
        {
            _settingEntry = false;
        }

        _entryEdited = false;
    }

    /// <summary>Sets the entry to match the given text exactly as typed (QBE and paste).</summary>
    public void SetValue(string pattern)
    {
        try
        {
            SetValue(new Regex(pattern, RegexOptions.None, MatchTimeout));
        }
        catch (ArgumentException)
        {
            SetValue(new Regex(Regex.Escape(pattern), RegexOptions.None, MatchTimeout));
        }
    }

    /// <summary>Sets the filter from the entry, taking the checkbox into account.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs line 103 (Set)</remarks>
    public void Set()
    {
        CommitEntry();
        _filter = IsChecked ? _value : new Regex(".*", RegexOptions.None, MatchTimeout);
        AppliedText = Display(_filter);
    }

    /// <summary>Sets the entry from the applied filter.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs line 98 (Revise)</remarks>
    public void Revise() => SetValue(_filter);

    /// <summary>Whether the applied filter accepts <paramref name="text"/>.</summary>
    public bool Matches(string text)
    {
        try
        {
            return _filter.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 41-50, 62-70
    private static string Display(Regex rx)
    {
        var text = rx.ToString().TrimStart('^').TrimEnd('$');
        while (text.StartsWith(".*", StringComparison.Ordinal)) text = text[2..];
        while (text.EndsWith(".*", StringComparison.Ordinal)) text = text[..^2];
        return text;
    }
}

/// <summary>
/// The resource filter: one <see cref="FilterFieldViewModel"/> per filterable column and a switch
/// that turns filtering of the browser list on and off.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs lines 28-190 and
/// legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2754-2773 (FilterChanged / PasteClicked).
/// </remarks>
public sealed partial class ResourceFilterViewModel : ObservableObject
{
    public ResourceFilterViewModel()
    {
        Fields = [.. BrowserColumns.FilterFields.Select(f => new FilterFieldViewModel(f, BrowserColumns.IsExactFilter(f)))];
    }

    /// <summary>Raised when the filter switch changes or a filter is set while filtering is on.</summary>
    public event EventHandler? FilterChanged;

    /// <summary>The filter fields in column order.</summary>
    public IReadOnlyList<FilterFieldViewModel> Fields { get; }

    /// <summary>Whether the applied filter restricts the browser list.</summary>
    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value) => FilterChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Gets a field by column name.</summary>
    public FilterFieldViewModel this[string name] => Fields.First(f => f.Name == name);

    /// <summary>Applies the entered values; the list is refiltered when filtering is on.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs lines 178-182 (btnSet_Click)</remarks>
    public void Set()
    {
        foreach (var field in Fields) field.Set();
        if (IsEnabled) FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Copies the applied filters back into the entries.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs line 172 (btnRevise_Click)</remarks>
    public void Revise()
    {
        foreach (var field in Fields) field.Revise();
    }

    /// <summary>
    /// Query by example: fills every entry from a resource so it can be used as the filter.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs lines 174-180 (btnQBE_Click).
    /// The name has backslashes doubled; the other texts are used as they are.
    /// </remarks>
    public void QueryByExample(ResourceItemViewModel resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        foreach (var field in Fields)
        {
            var text = resource.GetColumnText(field.Name);
            field.SetValue(field.Name == BrowserColumns.Name ? text.Replace(@"\", @"\\", StringComparison.Ordinal) : text);
        }
    }

    /// <summary>Fills the Type, Group and Instance entries from a resource key.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs lines 168-171 (PasteResourceKey)</remarks>
    public void PasteResourceKey(ResourceKey key)
    {
        this[BrowserColumns.Type].SetValue($"0x{key.ResourceType:X8}");
        this[BrowserColumns.Group].SetValue($"0x{key.ResourceGroup:X8}");
        this[BrowserColumns.Instance].SetValue($"0x{key.Instance:X16}");
    }

    /// <summary>Whether a resource passes every applied filter.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 995-1000</remarks>
    public bool Matches(ResourceItemViewModel resource)
    {
        foreach (var field in Fields)
        {
            if (!field.Matches(resource.GetColumnText(field.Name))) return false;
        }

        return true;
    }
}
