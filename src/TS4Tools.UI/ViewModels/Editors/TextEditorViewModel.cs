using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class TextEditorViewModel : ViewModelBase
{
    private TextResource? _resource;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isXml;

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private int _characterCount;

    [ObservableProperty]
    private bool _hasBom;

    [ObservableProperty]
    private bool _isModified;

    public TextEditorViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Text))
            {
                UpdateStats();
            }
        };
    }

    public void LoadResource(TextResource resource)
    {
        _resource = resource;
        Text = resource.Text;
        HasBom = resource.HasBom;
        IsXml = resource.IsXml;
        IsModified = false;
        UpdateStats();
    }

    private void UpdateStats()
    {
        LineCount = string.IsNullOrEmpty(Text) ? 0 : Text.Split('\n').Length;
        CharacterCount = Text.Length;
        IsXml = Text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
             || Text.TrimStart().StartsWith('<');

        if (_resource != null && Text != _resource.Text)
        {
            IsModified = true;
        }
    }

    [RelayCommand]
    private void ApplyChanges()
    {
        if (_resource == null) return;

        _resource.Text = Text;
        _resource.HasBom = HasBom;
        IsModified = false;
    }

    [RelayCommand]
    private void RevertChanges()
    {
        if (_resource == null) return;

        Text = _resource.Text;
        HasBom = _resource.HasBom;
        IsModified = false;
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}
