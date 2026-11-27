using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Browser list columns of a resource (the read-only display data legacy shows in its list view).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 584-621 (SetColumns)
/// and lines 709-725 (CreateItem: Name, Tag, then every resource index entry field as text).
/// </remarks>
public sealed partial class ResourceItemViewModel
{
    private uint _chunkOffset;
    private uint _memorySize;
    private ushort _compressionType;

    /// <summary>Offset of the resource data within the package file.</summary>
    public uint ChunkOffset
    {
        get => _chunkOffset;
        private set => SetProperty(ref _chunkOffset, value);
    }

    /// <summary>Uncompressed size of the resource.</summary>
    public uint MemorySize
    {
        get => _memorySize;
        private set => SetProperty(ref _memorySize, value);
    }

    /// <summary>Compression flag of the index entry (0x0000 = uncompressed).</summary>
    public ushort CompressionType
    {
        get => _compressionType;
        private set => SetProperty(ref _compressionType, value);
    }

    /// <summary>Keeps the column text properties in step with the values they format.</summary>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(FileSize): base.OnPropertyChanged(nameof(FileSizeText)); break;
            case nameof(ChunkOffset): base.OnPropertyChanged(nameof(ChunkOffsetText)); break;
            case nameof(MemorySize): base.OnPropertyChanged(nameof(MemorySizeText)); break;
            case nameof(CompressionType): base.OnPropertyChanged(nameof(CompressedText)); break;
        }
    }

    /// <summary>
    /// Copies the index entry fields shown in the browser columns.
    /// </summary>
    public void ApplyEntryData(IResourceIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ChunkOffset = entry.ChunkOffset;
        MemorySize = entry.MemorySize;
        CompressionType = entry.CompressionType;
        FileSize = entry.FileSize;
    }

    /// <summary>Name column text: the NameMap name, or empty when the instance has none.</summary>
    public string NameText => InstanceName ?? string.Empty;

    /// <summary>Type column text: 0x{X8}.</summary>
    public string TypeText => $"0x{Key.ResourceType:X8}";

    /// <summary>Group column text: 0x{X8}.</summary>
    public string GroupText => $"0x{Key.ResourceGroup:X8}";

    /// <summary>Instance column text: 0x{X16}.</summary>
    public string InstanceText => $"0x{Key.Instance:X16}";

    /// <summary>Chunkoffset column text: 0x{X8}.</summary>
    public string ChunkOffsetText => $"0x{ChunkOffset:X8}";

    /// <summary>Filesize column text: 0x{X8}.</summary>
    public string FileSizeText => $"0x{FileSize:X8}";

    /// <summary>Memsize column text: 0x{X8}.</summary>
    public string MemorySizeText => $"0x{MemorySize:X8}";

    /// <summary>Compressed column text: 0x{X4}.</summary>
    public string CompressedText => $"0x{CompressionType:X4}";

    /// <summary>
    /// Gets the text of a browser column (also the text filters are matched against).
    /// </summary>
    /// <param name="column">A column name from <see cref="BrowserColumns.All"/>.</param>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 996-999
    /// (filter text: Name -> ResourceName, Tag -> ResourceTag, otherwise value[field].ToString("X")).
    /// </remarks>
    public string GetColumnText(string column) => column switch
    {
        BrowserColumns.Name => NameText,
        BrowserColumns.Tag => Tag,
        BrowserColumns.Type => TypeText,
        BrowserColumns.Group => GroupText,
        BrowserColumns.Instance => InstanceText,
        BrowserColumns.ChunkOffset => ChunkOffsetText,
        BrowserColumns.FileSize => FileSizeText,
        BrowserColumns.MemorySize => MemorySizeText,
        BrowserColumns.Compressed => CompressedText,
        _ => string.Empty
    };
}
