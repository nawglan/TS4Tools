using System.Collections.ObjectModel;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Implementation of ISimOutfitResource for handling Sim outfit resources (SIMO).
/// Based on the legacy Sims4Tools SimOutfitResource implementation.
/// </summary>
public class SimOutfitResource : ISimOutfitResource
{
    private static readonly byte[] MagicBytes = [0x53, 0x49, 0x4D, 0x4F]; // "SIMO"

    private readonly List<ulong> _dataReferences = [];
    private readonly List<SliderReference> _bodySliders = [];
    private readonly List<SliderReference> _faceSliders = [];
    private readonly List<string> _contentFields =
    [
        "Version",
        "Age",
        "Gender", 
        "SkinToneReference",
        "CasPartReference",
        "DataReferences",
        "BodySliders",
        "FaceSliders"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    // Internal fields based on legacy implementation
    private float _unknown1, _unknown2, _unknown3, _unknown4;
    private float _unknown5, _unknown6, _unknown7, _unknown8;
    private byte[] _unknownData = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SimOutfitResource"/> class.
    /// </summary>
    public SimOutfitResource()
    {
        Version = 0x100;
        Age = AgeGenderFlags.None;
        Gender = AgeGenderFlags.None;
        SkinToneReference = 0;
        CasPartReference = 0;
        DataReferences = new ReadOnlyCollection<ulong>(_dataReferences);
        BodySliders = new ReadOnlyCollection<SliderReference>(_bodySliders);
        FaceSliders = new ReadOnlyCollection<SliderReference>(_faceSliders);
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimOutfitResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public SimOutfitResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        LoadFromStreamAsync(stream).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public uint Version { get; set; }

    /// <inheritdoc />
    public AgeGenderFlags Age { get; set; }

    /// <inheritdoc />
    public AgeGenderFlags Gender { get; set; }

    /// <inheritdoc />
    public ulong SkinToneReference { get; set; }

    /// <inheritdoc />
    public ulong CasPartReference { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ulong> DataReferences { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<SliderReference> BodySliders { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<SliderReference> FaceSliders { get; private set; }

    /// <inheritdoc />
    public bool IsValid => 
        Version > 0 && 
        (Age != AgeGenderFlags.None || Gender != AgeGenderFlags.None) &&
        _dataReferences.Count > 0;

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SimOutfitResource));

            _stream ??= new MemoryStream();
            return _stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SimOutfitResource));

            using var ms = new MemoryStream();
            SaveToStreamAsync(ms).ConfigureAwait(false).GetAwaiter().GetResult();
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; set; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index < _contentFields.Count 
            ? this[_contentFields[index]]
            : TypedValue.Create<object?>(null);
        set
        {
            if (index < _contentFields.Count)
                this[_contentFields[index]] = value;
        }
    }

    /// <inheritdoc />
    public TypedValue this[string fieldName]
    {
        get => fieldName switch
        {
            "Version" => TypedValue.Create(Version),
            "Age" => TypedValue.Create(Age),
            "Gender" => TypedValue.Create(Gender),
            "SkinToneReference" => TypedValue.Create(SkinToneReference),
            "CasPartReference" => TypedValue.Create(CasPartReference),
            "DataReferences" => TypedValue.Create(DataReferences),
            "BodySliders" => TypedValue.Create(BodySliders),
            "FaceSliders" => TypedValue.Create(FaceSliders),
            _ => TypedValue.Create<object?>(null)
        };
        set
        {
            switch (fieldName)
            {
                case "Version":
                    Version = value.GetValue<uint>();
                    break;
                case "Age":
                    Age = value.GetValue<AgeGenderFlags>();
                    break;
                case "Gender":
                    Gender = value.GetValue<AgeGenderFlags>();
                    break;
                case "SkinToneReference":
                    SkinToneReference = value.GetValue<ulong>();
                    break;
                case "CasPartReference":
                    CasPartReference = value.GetValue<ulong>();
                    break;
            }
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public void AddDataReference(ulong reference)
    {
        if (!_dataReferences.Contains(reference))
        {
            _dataReferences.Add(reference);
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public bool RemoveDataReference(ulong reference)
    {
        if (_dataReferences.Remove(reference))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public void AddSliderReference(SliderReference slider, bool isBodySlider = true)
    {
        var targetList = isBodySlider ? _bodySliders : _faceSliders;
        
        // Remove existing slider with same hash
        targetList.RemoveAll(s => s.SliderHash == slider.SliderHash);
        
        // Add new slider if value is not default
        if (!slider.IsDefault)
        {
            targetList.Add(slider.Clamped());
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Read version and TGI offset
            Version = reader.ReadUInt32();
            var tgiOffset = reader.ReadUInt32() + 8;

            // Store TGI list position for later
            var currentPosition = stream.Position;

            // Read unknown floats (8 floats)
            _unknown1 = reader.ReadSingle();
            _unknown2 = reader.ReadSingle();
            _unknown3 = reader.ReadSingle();
            _unknown4 = reader.ReadSingle();
            _unknown5 = reader.ReadSingle();
            _unknown6 = reader.ReadSingle();
            _unknown7 = reader.ReadSingle();
            _unknown8 = reader.ReadSingle();

            // Read age and gender flags
            Age = (AgeGenderFlags)reader.ReadUInt32();
            Gender = (AgeGenderFlags)reader.ReadUInt32();
            SkinToneReference = reader.ReadUInt64();

            // Read unknown byte list
            var unknownByteCount = reader.ReadByte();
            _unknownData = reader.ReadBytes(unknownByteCount);

            // Read slider references (simplified parsing)
            await ReadSliderReferences(reader, cancellationToken).ConfigureAwait(false);

            // Read CAS part reference and data references
            CasPartReference = reader.ReadUInt64();
            
            // Read data reference list
            var dataRefCount = reader.ReadInt32();
            _dataReferences.Clear();
            for (int i = 0; i < dataRefCount; i++)
            {
                _dataReferences.Add(reader.ReadUInt64());
            }

            // Update our internal stream
            _stream?.Dispose();
            _stream = new MemoryStream();
            stream.Position = 0;
            await stream.CopyToAsync(_stream, cancellationToken).ConfigureAwait(false);
            _stream.Position = 0;

            OnResourceChanged();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load sim outfit resource from stream: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Write version and placeholder for TGI offset
            writer.Write(Version);
            var tgiOffsetPosition = stream.Position;
            writer.Write(0u); // Placeholder for TGI offset

            // Write unknown floats
            writer.Write(_unknown1);
            writer.Write(_unknown2);
            writer.Write(_unknown3);
            writer.Write(_unknown4);
            writer.Write(_unknown5);
            writer.Write(_unknown6);
            writer.Write(_unknown7);
            writer.Write(_unknown8);

            // Write age and gender flags
            writer.Write((uint)Age);
            writer.Write((uint)Gender);
            writer.Write(SkinToneReference);

            // Write unknown byte data
            writer.Write((byte)_unknownData.Length);
            writer.Write(_unknownData);

            // Write slider references (simplified)
            await WriteSliderReferences(writer, cancellationToken).ConfigureAwait(false);

            // Write CAS part reference and data references
            writer.Write(CasPartReference);
            writer.Write(_dataReferences.Count);
            foreach (var reference in _dataReferences)
            {
                writer.Write(reference);
            }

            // Update TGI offset (simplified - we're not writing full TGI data yet)
            var currentPosition = stream.Position;
            stream.Position = tgiOffsetPosition;
            writer.Write((uint)(currentPosition - 8));
            stream.Position = currentPosition;

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save sim outfit resource to stream: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the resource and cleans up managed resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resource and cleans up managed resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    protected virtual void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reads slider reference data from the stream.
    /// </summary>
    private async Task ReadSliderReferences(BinaryReader reader, CancellationToken cancellationToken)
    {
        // Simplified slider reading - the legacy format is quite complex
        // This is a basic implementation that can be enhanced later
        
        try
        {
            // Try to read some slider data if available
            var remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
            if (remainingBytes > 16) // At least enough for a few sliders
            {
                var sliderCount = Math.Min(reader.ReadInt32(), 100); // Cap at reasonable number
                
                for (int i = 0; i < sliderCount && i < 50; i++) // Limit iterations
                {
                    if (reader.BaseStream.Position + 12 > reader.BaseStream.Length) break;
                    
                    var hash = reader.ReadUInt32();
                    var value = reader.ReadSingle();
                    var category = (SliderCategory)reader.ReadInt32();
                    
                    var slider = new SliderReference(hash, value, category);
                    if (category == SliderCategory.Face)
                        _faceSliders.Add(slider);
                    else
                        _bodySliders.Add(slider);
                }
            }
        }
        catch
        {
            // If slider reading fails, continue without sliders
        }

        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <summary>
    /// Writes slider reference data to the stream.
    /// </summary>
    private async Task WriteSliderReferences(BinaryWriter writer, CancellationToken cancellationToken)
    {
        // Write combined slider count
        var totalSliders = _bodySliders.Count + _faceSliders.Count;
        writer.Write(totalSliders);

        // Write body sliders
        foreach (var slider in _bodySliders)
        {
            writer.Write(slider.SliderHash);
            writer.Write(slider.Value);
            writer.Write((int)slider.Category);
        }

        // Write face sliders
        foreach (var slider in _faceSliders)
        {
            writer.Write(slider.SliderHash);
            writer.Write(slider.Value);
            writer.Write((int)slider.Category);
        }

        await Task.CompletedTask; // Satisfy async requirement
    }
}
