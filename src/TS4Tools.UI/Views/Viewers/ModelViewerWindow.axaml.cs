// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs (WPF viewer window: orbit and zoom)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using TS4Tools.UI.ViewModels.Editors;

namespace TS4Tools.UI.Views.Viewers;

/// <summary>
/// Cross-platform model viewer: shows the software-rendered frame and turns mouse drags/wheel into camera moves.
/// </summary>
public partial class ModelViewerWindow : Window
{
    private readonly ModelViewerViewModel? _viewModel;
    private readonly EventHandler? _renderHandler;
    private Point? _dragStart;

    public ModelViewerWindow()
    {
        InitializeComponent();
    }

    public ModelViewerWindow(ModelViewerViewModel viewModel)
        : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        _renderHandler = (_, _) => RenderFrame();
        viewModel.RenderRequested += _renderHandler;

        Viewport.SizeChanged += OnViewportSizeChanged;
        Viewport.PointerPressed += OnPointerPressed;
        Viewport.PointerMoved += OnPointerMoved;
        Viewport.PointerReleased += OnPointerReleased;
        Viewport.PointerWheelChanged += OnPointerWheelChanged;
        Closed += OnClosed;
    }

    private void RenderFrame()
    {
        if (_viewModel == null) return;
        var scaling = RenderScaling;
        _viewModel.Render((int)(Viewport.Bounds.Width * scaling), (int)(Viewport.Bounds.Height * scaling));
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e) => RenderFrame();

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => _dragStart = e.GetPosition(Viewport);

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is not { } start || _viewModel == null) return;
        var position = e.GetPosition(Viewport);
        _viewModel.Rotate(position.X - start.X, position.Y - start.Y);
        _dragStart = position;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => _dragStart = null;

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e) => _viewModel?.ZoomBy(e.Delta.Y);

    private void OnClosed(object? sender, EventArgs e)
    {
        Viewport.SizeChanged -= OnViewportSizeChanged;
        Viewport.PointerPressed -= OnPointerPressed;
        Viewport.PointerMoved -= OnPointerMoved;
        Viewport.PointerReleased -= OnPointerReleased;
        Viewport.PointerWheelChanged -= OnPointerWheelChanged;
        Closed -= OnClosed;
        if (_viewModel != null)
        {
            if (_renderHandler != null) _viewModel.RenderRequested -= _renderHandler;
            _viewModel.Dispose();
        }
    }
}
