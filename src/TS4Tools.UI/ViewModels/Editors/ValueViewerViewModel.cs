// Source: legacy_references/Sims4Tools/s4pe/BuiltInValueControl.cs lines 474-540 (TextControl: read-only monospace text)
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2947-3006 (GetPreviewControl: Value text, fallback
//         text preview, exception text)

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Compatibility;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>What a <see cref="ValueViewerViewModel"/> is showing.</summary>
public enum ValueViewKind
{
    /// <summary>The wrapper's Value dump (legacy ValueBuilder).</summary>
    Value,
    /// <summary>Raw bytes decoded as text for an unknown type (legacy fallback text preview).</summary>
    FallbackText,
    /// <summary>An error reading the resource (legacy GetExceptionControl).</summary>
    Error,
}

/// <summary>
/// Read-only text preview: the generic "Value" view for wrappers without a dedicated editor, the fallback text
/// preview and the error view.
/// </summary>
public partial class ValueViewerViewModel : ViewModelBase
{
    /// <summary>
    /// Banner legacy puts in front of fallback text previews.
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2970-2974.
    /// </summary>
    public const string FallbackBanner =
        "== == == == == == == == == == == == == == == == ==\n" +
        " **  Fallback preview:  data may be incomplete  ** \n" +
        "== == == == == == == == == == == == == == == == ==\n\n\n";

    private object? _wrapper;

    [ObservableProperty]
    private string _title = "Value";

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private ValueViewKind _kind;

    /// <summary>Whether <see cref="RefreshCommand"/> can rebuild the text from a wrapper.</summary>
    public bool CanRefresh => _wrapper != null;

    /// <summary>The wrapper whose value is shown, if any.</summary>
    public object? Wrapper => _wrapper;

    /// <summary>Shows the Value dump of <paramref name="wrapper"/>.</summary>
    public void LoadWrapper(object wrapper)
    {
        _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
        Kind = ValueViewKind.Value;
        Title = $"Value ({(wrapper is LegacyResourceAdapter legacy ? legacy.LegacyWrapperType : wrapper.GetType()).Name})";
        Refresh();
        OnPropertyChanged(nameof(CanRefresh));
    }

    /// <summary>Shows raw bytes as text with the legacy fallback banner.</summary>
    public void LoadFallbackText(ReadOnlySpan<byte> data)
    {
        _wrapper = null;
        Kind = ValueViewKind.FallbackText;
        Title = "Fallback text preview";
        Text = FallbackBanner + Encoding.UTF8.GetString(data);
        OnPropertyChanged(nameof(CanRefresh));
    }

    /// <summary>Shows an error report for a resource that could not be read.</summary>
    /// <remarks>Source: MainForm.cs lines 2985-3006 (GetExceptionControl).</remarks>
    public void LoadError(string resourceDescription, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _wrapper = null;
        Kind = ValueViewKind.Error;
        Title = "Error reading resource";

        var sb = new StringBuilder();
        sb.Append("Error reading resource ").Append(resourceDescription).Append("\r\n");
        for (var inner = exception; inner != null; inner = inner.InnerException)
        {
            sb.Append("\r\nSource: ").Append(inner.Source);
            sb.Append("\r\nAssembly: ").Append(inner.TargetSite?.DeclaringType?.Assembly.FullName);
            sb.Append("\r\n").Append(inner.Message);
            sb.Append("\r\n----\r\nStack trace:\r\n").Append(inner.StackTrace).Append("\r\n----\r\n");
        }
        Text = sb.ToString();
        OnPropertyChanged(nameof(CanRefresh));
    }

    /// <summary>Rebuilds the Value text (e.g. after property grid edits).</summary>
    [RelayCommand]
    public void Refresh()
    {
        if (_wrapper == null) return;
        try
        {
            // A bridged legacy wrapper shows its own Value text, as s4pe does (MainForm.cs lines 2951-2953).
            Text = _wrapper is LegacyResourceAdapter legacy ? legacy.GetValueText() : ValueBuilder.Build(_wrapper);
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            Text = $"(Exception building value: {(ex.InnerException ?? ex).Message})";
        }
    }

    /// <summary>
    /// Whether raw data looks like text: valid UTF-8 (BOM allowed) with no NUL and few control characters.
    /// </summary>
    /// <remarks>
    /// Legacy shows the fallback text preview for any data when the setting is on; here binary data stays in the
    /// hex view because decoding it as text only produces noise.
    /// </remarks>
    public static bool LooksLikeText(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return false;
        var sample = data[..Math.Min(data.Length, 8192)];
        if (sample.IndexOf((byte)0) >= 0) return false;

        int control = 0;
        foreach (var b in sample)
        {
            if (b < 0x20 && b is not ((byte)'\r' or (byte)'\n' or (byte)'\t' or 0x0C)) control++;
        }
        if (control * 50 > sample.Length) return false;

        try
        {
            // Throws on invalid UTF-8 (a truncated final sequence at the sample end is tolerated).
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var end = sample.Length;
            if (sample.Length < data.Length)
            {
                // Step back over a partial multi-byte sequence at the cut.
                while (end > 0 && (sample[end - 1] & 0xC0) == 0x80) end--;
                if (end > 0 && sample[end - 1] >= 0xC0) end--;
            }
            strict.GetCharCount(sample[..end]);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
