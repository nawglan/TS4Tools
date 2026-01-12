using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// View model for the RCOL resource viewer.
/// </summary>
public partial class RcolViewerViewModel : ViewModelBase
{
    private RcolResource? _resource;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _versionDisplay = string.Empty;

    [ObservableProperty]
    private int _publicChunksCount;

    [ObservableProperty]
    private int _chunkCount;

    [ObservableProperty]
    private int _externalCount;

    [ObservableProperty]
    private string _headerInfo = string.Empty;

    [ObservableProperty]
    private RcolChunkViewModel? _selectedChunk;

    /// <summary>
    /// The chunks in this RCOL resource.
    /// </summary>
    public ObservableCollection<RcolChunkViewModel> Chunks { get; } = [];

    /// <summary>
    /// External resource references.
    /// </summary>
    public ObservableCollection<RcolExternalViewModel> ExternalResources { get; } = [];

    /// <summary>
    /// Loads an RCOL resource for display.
    /// </summary>
    public void LoadResource(RcolResource resource)
    {
        _resource = resource;
        IsValid = resource.IsValid;
        VersionDisplay = $"0x{resource.Version:X8}";
        PublicChunksCount = resource.PublicChunksCount;
        ChunkCount = resource.ChunkCount;
        ExternalCount = resource.ExternalResourceCount;

        BuildHeaderInfo(resource);
        LoadChunks(resource);
        LoadExternalResources(resource);

        // Select first chunk by default
        if (Chunks.Count > 0)
        {
            SelectedChunk = Chunks[0];
        }
    }

    private void BuildHeaderInfo(RcolResource resource)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"RCOL Version: {VersionDisplay}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Public Chunks: {PublicChunksCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Total Chunks: {ChunkCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"External Resources: {ExternalCount}");

        if (!IsValid)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING: Invalid or unsupported RCOL format");
        }

        HeaderInfo = sb.ToString();
    }

    private void LoadChunks(RcolResource resource)
    {
        Chunks.Clear();

        for (int i = 0; i < resource.Chunks.Count; i++)
        {
            var chunk = resource.Chunks[i];
            Chunks.Add(new RcolChunkViewModel(chunk, i));
        }
    }

    private void LoadExternalResources(RcolResource resource)
    {
        ExternalResources.Clear();

        for (int i = 0; i < resource.ExternalResources.Count; i++)
        {
            var external = resource.ExternalResources[i];
            ExternalResources.Add(new RcolExternalViewModel(external, i));
        }
    }
}
