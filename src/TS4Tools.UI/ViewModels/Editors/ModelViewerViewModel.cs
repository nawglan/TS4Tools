// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml.cs lines 19-120 (SceneMesh list, mesh
//         selection, vertex/polygon counts) and 257-385 (InitScene)
// Source: legacy_references/Sims4Tools/s4pe Helpers/ModelViewer/MainWindow.xaml (Solid / Wireframe / Both draw modes)

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Services.ModelViewer;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// View model for the 3D model viewer window (legacy ModelViewer helper): mesh list with visibility, draw mode,
/// orbit camera and the rendered frame.
/// </summary>
public partial class ModelViewerViewModel : ViewModelBase, IDisposable
{
    private readonly List<(ModelMeshItem Item, PropertyChangedEventHandler Handler)> _subscriptions = [];
    private bool _disposed;

    [ObservableProperty]
    private string _title = "Model Viewer";

    [ObservableProperty]
    private string _statistics = string.Empty;

    [ObservableProperty]
    private string _warnings = string.Empty;

    [ObservableProperty]
    private ModelRenderMode _renderMode = ModelRenderMode.Both;

    [ObservableProperty]
    private OrbitCamera _camera = OrbitCamera.Default;

    [ObservableProperty]
    private WriteableBitmap? _frame;

    /// <summary>The meshes of the model.</summary>
    public ObservableCollection<ModelMeshItem> Meshes { get; } = [];

    /// <summary>Draw modes for the chooser.</summary>
    public IReadOnlyList<ModelRenderMode> RenderModes { get; } = Enum.GetValues<ModelRenderMode>();

    /// <summary>Raised when the frame should be re-rendered (mesh visibility, mode or camera changed).</summary>
    public event EventHandler? RenderRequested;

    /// <summary>Loads the meshes of an RCOL model resource.</summary>
    public void Load(RcolResource rcol, string title)
    {
        ArgumentNullException.ThrowIfNull(rcol);
        Load(MeshExtractor.Extract(rcol), title);
    }

    /// <summary>Loads extracted model data.</summary>
    public void Load(ModelData model, string title)
    {
        ArgumentNullException.ThrowIfNull(model);
        ClearSubscriptions();
        Meshes.Clear();
        foreach (var mesh in model.Meshes)
        {
            var item = new ModelMeshItem(mesh);
            PropertyChangedEventHandler handler = (_, _) => RenderRequested?.Invoke(this, EventArgs.Empty);
            item.PropertyChanged += handler;
            _subscriptions.Add((item, handler));
            Meshes.Add(item);
        }

        Title = $"Model Viewer - {title}";
        // Source: MainWindow.xaml.cs lines 383-384 ("Vertices: {0}", "Polygons: {0}")
        Statistics = $"Vertices: {model.VertexCount}    Polygons: {model.PolygonCount}    Meshes: {model.Meshes.Count}";
        Warnings = string.Join(Environment.NewLine, model.Warnings);
        Camera = OrbitCamera.Default;
    }

    partial void OnRenderModeChanged(ModelRenderMode value) => RenderRequested?.Invoke(this, EventArgs.Empty);

    partial void OnCameraChanged(OrbitCamera value) => RenderRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Rotates the camera by a mouse drag delta in pixels.</summary>
    public void Rotate(double dx, double dy)
    {
        var pitch = Math.Clamp(Camera.Pitch + (float)(dy * 0.01), -1.55f, 1.55f);
        Camera = Camera with { Yaw = Camera.Yaw + (float)(dx * 0.01), Pitch = pitch };
    }

    /// <summary>Zooms by a wheel delta.</summary>
    public void ZoomBy(double delta)
    {
        var zoom = Math.Clamp(Camera.Zoom * (float)Math.Pow(1.15, delta), 0.05f, 50f);
        Camera = Camera with { Zoom = zoom };
    }

    /// <summary>Resets the camera.</summary>
    [RelayCommand]
    private void ResetCamera() => Camera = OrbitCamera.Default;

    /// <summary>The meshes currently drawn.</summary>
    public IReadOnlyList<MeshData> VisibleMeshes => Meshes.Where(m => m.IsVisible).Select(m => m.Mesh).ToList();

    /// <summary>Renders the visible meshes at the given size into <see cref="Frame"/>.</summary>
    public void Render(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        width = Math.Min(width, 4096);
        height = Math.Min(height, 4096);

        var pixels = SoftwareRenderer.Render(VisibleMeshes, width, height, Camera, RenderMode);
        var bitmap = Frame;
        if (bitmap == null || bitmap.PixelSize.Width != width || bitmap.PixelSize.Height != height)
        {
            bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        using (var fb = bitmap.Lock())
        {
            int rowBytes = width * 4;
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(pixels, y * rowBytes, fb.Address + y * fb.RowBytes, rowBytes);
            }
        }

        // Re-assign so bindings refresh even when the same bitmap instance is reused.
        Frame = null;
        Frame = bitmap;
    }

    private void ClearSubscriptions()
    {
        foreach (var (item, handler) in _subscriptions)
            item.PropertyChanged -= handler;
        _subscriptions.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearSubscriptions();
        Frame?.Dispose();
        Frame = null;
        GC.SuppressFinalize(this);
    }
}

/// <summary>One mesh in the model viewer list (legacy SceneMesh).</summary>
public partial class ModelMeshItem(MeshData mesh) : ObservableObject
{
    /// <summary>The mesh.</summary>
    public MeshData Mesh => mesh;

    /// <summary>Display text: name and counts.</summary>
    public string Display => $"{mesh.Name} ({mesh.VertexCount} verts, {mesh.TriangleCount} tris)";

    [ObservableProperty]
    private bool _isVisible = true;
}
