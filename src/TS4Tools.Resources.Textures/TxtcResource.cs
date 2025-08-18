/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Textures;

/// <summary>
/// Modern .NET 9 implementation of TXTC (Texture Compositor) resource for TS4Tools.
/// This resource type represents texture compositions used extensively in The Sims 4.
/// TXTC resources can contain either references to external texture resources or embedded texture data,
/// along with composition parameters for blending, tiling, and transformation.
/// </summary>
/// <remarks>
/// Resource Type ID: 0x00B2D882
/// Used for: Texture composition, material definitions, overlay systems
/// Priority: P1 Critical - Essential for texture and material system functionality
///
/// The TXTC format supports:
/// - Multiple texture layers with composition parameters
/// - External texture references via TGI
/// - Embedded texture data for standalone textures
/// - Advanced blending modes and transformations
/// - Mipmap chain support for performance optimization
/// </remarks>
public class TxtcResource : IResource, IDisposable
{
    #region Constants

    /// <summary>The resource type identifier for TXTC resources.</summary>
    public static readonly string ResourceType = "0x00B2D882";

    /// <summary>Expected magic number in TXTC file header.</summary>
    private const uint ExpectedMagicNumber = 0x54585443; // "TXTC"

    /// <summary>Minimum supported version of TXTC format.</summary>
    private const uint MinSupportedVersion = 1;

    /// <summary>Maximum supported version of TXTC format.</summary>
    private const uint MaxSupportedVersion = 3;

    #endregion

    #region Fields

    private readonly ILogger<TxtcResource> _logger;
    private readonly MemoryStream _stream;
    private readonly int _apiVersion;
    private uint _version = 1;
    private TxtcFlags _flags = TxtcFlags.None;
    private uint _layerCount;
    private readonly List<TextureLayer> _layers = [];
    private uint _compositionWidth;
    private uint _compositionHeight;
    private TextureFormat _outputFormat = TextureFormat.ARGB;
    private readonly Dictionary<string, object> _customProperties = [];
    private bool _disposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TxtcResource"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="data">Optional initial data stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public TxtcResource(ILogger<TxtcResource> logger, int apiVersion = 1, Stream? data = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _apiVersion = apiVersion;
        _stream = new MemoryStream();

        if (data != null)
        {
            data.CopyTo(_stream);
            _stream.Position = 0;
        }

        _logger.LogDebug("Creating new TxtcResource instance with API version {ApiVersion}", apiVersion);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TxtcResource"/> class with specific composition dimensions.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="apiVersion">The requested API version.</param>
    /// <param name="width">The composition width in pixels.</param>
    /// <param name="height">The composition height in pixels.</param>
    /// <param name="format">The output texture format.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid.</exception>
    public TxtcResource(ILogger<TxtcResource> logger, int apiVersion, uint width, uint height, TextureFormat format = TextureFormat.ARGB)
        : this(logger, apiVersion)
    {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0");
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0");

        _compositionWidth = width;
        _compositionHeight = height;
        _outputFormat = format;

        _logger.LogDebug("Created TxtcResource with dimensions {Width}x{Height}, format {Format}", width, height, format);
    }

    #endregion

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream => _stream;

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            _stream.Position = 0;
            using var memoryStream = new MemoryStream();
            _stream.CopyTo(memoryStream);
            _stream.Position = 0;
            return memoryStream.ToArray();
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public int RequestedApiVersion => _apiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IContentFields Implementation

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        "Version", "Flags", "CompositionWidth", "CompositionHeight", "OutputFormat", "LayerCount"
    };

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index < ContentFields.Count ? this[ContentFields[index]] : TypedValue.Create<object?>(null);
        set
        {
            if (index < ContentFields.Count)
                this[ContentFields[index]] = value;
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => name switch
        {
            "Version" => TypedValue.Create(_version, "0x{0:X}"),
            "Flags" => TypedValue.Create(_flags),
            "CompositionWidth" => TypedValue.Create(_compositionWidth),
            "CompositionHeight" => TypedValue.Create(_compositionHeight),
            "OutputFormat" => TypedValue.Create(_outputFormat),
            "LayerCount" => TypedValue.Create((uint)_layers.Count),
            _ => TypedValue.Create<object?>(null)
        };
        set
        {
            switch (name)
            {
                case "Flags":
                    var flags = value.GetValue<TxtcFlags>();
                    _flags = flags;
                    OnResourceChanged();
                    break;
                case "CompositionWidth":
                    var width = value.GetValue<uint>();
                    if (width > 0)
                    {
                        _compositionWidth = width;
                        OnResourceChanged();
                    }
                    break;
                case "CompositionHeight":
                    var height = value.GetValue<uint>();
                    if (height > 0)
                    {
                        _compositionHeight = height;
                        OnResourceChanged();
                    }
                    break;
                case "OutputFormat":
                    var format = value.GetValue<TextureFormat>();
                    _outputFormat = format;
                    OnResourceChanged();
                    break;
            }
        }
    }

    #endregion

    #region Public Properties

    /// <summary>Gets the TXTC format version.</summary>
    public uint Version => _version;

    /// <summary>Gets the configuration flags.</summary>
    public TxtcFlags Flags => _flags;

    /// <summary>Gets the composition width in pixels.</summary>
    public uint CompositionWidth => _compositionWidth;

    /// <summary>Gets the composition height in pixels.</summary>
    public uint CompositionHeight => _compositionHeight;

    /// <summary>Gets the output texture format.</summary>
    public TextureFormat OutputFormat => _outputFormat;

    /// <summary>Gets the texture layers in this composition.</summary>
    public IReadOnlyList<TextureLayer> Layers => _layers.AsReadOnly();

    /// <summary>Gets custom properties associated with this resource.</summary>
    public IReadOnlyDictionary<string, object> CustomProperties => _customProperties.AsReadOnly();

    #endregion

    #region Public Methods

    /// <summary>
    /// Deserializes TXTC data from the current stream.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeserializeAsync()
    {
        _stream.Position = 0;
        using var reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        _logger.LogDebug("Deserializing TxtcResource from stream of {Length} bytes", _stream.Length);

        // Read and validate header
        var magic = reader.ReadUInt32();
        if (magic != ExpectedMagicNumber)
        {
            throw new InvalidDataException($"Invalid TXTC magic number. Expected 0x{ExpectedMagicNumber:X8}, got 0x{magic:X8}");
        }

        _version = reader.ReadUInt32();
        if (_version < MinSupportedVersion || _version > MaxSupportedVersion)
        {
            throw new NotSupportedException($"TXTC version {_version} is not supported. Supported versions: {MinSupportedVersion}-{MaxSupportedVersion}");
        }

        _flags = (TxtcFlags)reader.ReadUInt32();
        _compositionWidth = reader.ReadUInt32();
        _compositionHeight = reader.ReadUInt32();
        _outputFormat = (TextureFormat)reader.ReadUInt32();
        _layerCount = reader.ReadUInt32();

        _logger.LogDebug("TXTC Header: Version={Version}, Flags={Flags}, Dimensions={Width}x{Height}, Format={Format}, Layers={LayerCount}",
            _version, _flags, _compositionWidth, _compositionHeight, _outputFormat, _layerCount);

        // Read layers
        _layers.Clear();
        for (uint i = 0; i < _layerCount; i++)
        {
            var layer = await ReadLayerAsync(reader, i);
            _layers.Add(layer);
        }

        // Read custom properties if present
        if (_stream.Position < _stream.Length)
        {
            await ReadCustomPropertiesAsync(reader);
        }

        _logger.LogInformation("Successfully deserialized TxtcResource with {LayerCount} layers", _layers.Count);
    }

    /// <summary>
    /// Serializes the TXTC data to the current stream.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SerializeAsync()
    {
        _logger.LogDebug("Serializing TxtcResource with {LayerCount} layers", _layers.Count);

        _stream.SetLength(0);
        _stream.Position = 0;
        using var writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Write header
        writer.Write(ExpectedMagicNumber); // TXTC magic
        writer.Write(_version);
        writer.Write((uint)_flags);
        writer.Write(_compositionWidth);
        writer.Write(_compositionHeight);
        writer.Write((uint)_outputFormat);
        writer.Write((uint)_layers.Count);

        // Write layers
        foreach (var layer in _layers)
        {
            await WriteLayerAsync(writer, layer);
        }

        // Write custom properties if any
        await WriteCustomPropertiesAsync(writer);

        _stream.Position = 0;
        _logger.LogDebug("Serialized TxtcResource to {Size} bytes", _stream.Length);
    }

    /// <summary>
    /// Adds a texture layer with external reference to the composition.
    /// </summary>
    /// <param name="reference">The texture reference to add.</param>
    /// <param name="compositionParams">Optional composition parameters.</param>
    /// <param name="layerFlags">Optional layer-specific flags.</param>
    /// <returns>The index of the added layer.</returns>
    public int AddLayer(TextureReference reference, CompositionParameters? compositionParams = null, uint layerFlags = 0)
    {
        var layer = new TextureLayer
        {
            Index = (uint)_layers.Count,
            Reference = reference,
            CompositionParams = compositionParams ?? CompositionParameters.Default,
            LayerFlags = layerFlags
        };

        _layers.Add(layer);
        OnResourceChanged();

        _logger.LogDebug("Added external reference layer at index {Index} with TGI {Tgi}", layer.Index, reference.Tgi);
        return (int)layer.Index;
    }

    /// <summary>
    /// Adds a texture layer with embedded data to the composition.
    /// </summary>
    /// <param name="embeddedData">The embedded texture data to add.</param>
    /// <param name="compositionParams">Optional composition parameters.</param>
    /// <param name="layerFlags">Optional layer-specific flags.</param>
    /// <returns>The index of the added layer.</returns>
    public int AddLayer(EmbeddedTextureData embeddedData, CompositionParameters? compositionParams = null, uint layerFlags = 0)
    {
        var layer = new TextureLayer
        {
            Index = (uint)_layers.Count,
            EmbeddedData = embeddedData,
            CompositionParams = compositionParams ?? CompositionParameters.Default,
            LayerFlags = layerFlags
        };

        _layers.Add(layer);
        _flags |= TxtcFlags.EmbeddedData;
        OnResourceChanged();

        _logger.LogDebug("Added embedded data layer at index {Index} with {DataSize} bytes", layer.Index, embeddedData.DataSize);
        return (int)layer.Index;
    }

    /// <summary>
    /// Sets a custom property value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public void SetCustomProperty(string name, object value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Property name cannot be null or whitespace", nameof(name));

        _customProperties[name] = value;
        OnResourceChanged();

        _logger.LogDebug("Set custom property '{Name}' to '{Value}'", name, value);
    }

    #endregion

    #region Private Methods

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task WriteLayerAsync(BinaryWriter writer, TextureLayer layer)
    {
        writer.Write(layer.Index);
        writer.Write(layer.LayerFlags);

        // Write layer type and data
        if (layer.UsesExternalReference)
        {
            writer.Write((byte)1); // External reference
            var tgiRef = layer.Reference!.Value;
            writer.Write(tgiRef.Tgi.TypeId);
            writer.Write(tgiRef.Tgi.GroupId);
            writer.Write(tgiRef.Tgi.InstanceId);
            writer.Write((uint)tgiRef.Format);
            writer.Write(tgiRef.Width);
            writer.Write(tgiRef.Height);
            writer.Write((uint)tgiRef.MipmapLevel);
        }
        else if (layer.HasEmbeddedData)
        {
            writer.Write((byte)2); // Embedded data
            var embedded = layer.EmbeddedData!.Value;
            writer.Write((uint)embedded.Format);
            writer.Write(embedded.Width);
            writer.Write(embedded.Height);
            writer.Write(embedded.DataSize);
            writer.Write(embedded.Data);
        }
        else
        {
            writer.Write((byte)0); // Empty layer
        }

        // Write composition parameters
        var comp = layer.CompositionParams;
        writer.Write(comp.BlendMode);
        writer.Write(comp.Opacity);
        writer.Write(comp.TileU);
        writer.Write(comp.TileV);
        writer.Write(comp.OffsetU);
        writer.Write(comp.OffsetV);
        writer.Write(comp.Rotation);

        await Task.CompletedTask; // Maintain async signature for consistency
    }

    private async Task<TextureLayer> ReadLayerAsync(BinaryReader reader, uint index)
    {
        var layerIndex = reader.ReadUInt32();
        var layerFlags = reader.ReadUInt32();
        var layerType = reader.ReadByte();

        TextureReference? reference = null;
        EmbeddedTextureData? embeddedData = null;

        switch (layerType)
        {
            case 1: // External reference
                var typeId = reader.ReadUInt32();
                var groupId = reader.ReadUInt32();
                var instanceId = reader.ReadUInt64();
                var format = (TextureFormat)reader.ReadUInt32();
                var width = reader.ReadUInt32();
                var height = reader.ReadUInt32();
                var mipmapLevel = (MipmapLevel)reader.ReadUInt32();

                reference = new TextureReference(
                    new TgiReference(typeId, groupId, instanceId),
                    format, width, height, mipmapLevel);
                break;

            case 2: // Embedded data
                var embeddedFormat = (TextureFormat)reader.ReadUInt32();
                var embeddedWidth = reader.ReadUInt32();
                var embeddedHeight = reader.ReadUInt32();
                var dataSize = reader.ReadUInt32();
                var data = reader.ReadBytes((int)dataSize);

                embeddedData = new EmbeddedTextureData(embeddedFormat, embeddedWidth, embeddedHeight, data);
                break;
        }

        // Read composition parameters
        var blendMode = reader.ReadUInt32();
        var opacity = reader.ReadByte();
        var tileU = reader.ReadSingle();
        var tileV = reader.ReadSingle();
        var offsetU = reader.ReadSingle();
        var offsetV = reader.ReadSingle();
        var rotation = reader.ReadSingle();

        var compositionParams = new CompositionParameters
        {
            BlendMode = blendMode,
            Opacity = opacity,
            TileU = tileU,
            TileV = tileV,
            OffsetU = offsetU,
            OffsetV = offsetV,
            Rotation = rotation
        };

        await Task.CompletedTask; // Maintain async signature

        var layer = new TextureLayer
        {
            Index = layerIndex, // Use the read index, not the passed index
            CompositionParams = compositionParams,
            LayerFlags = layerFlags
        };

        // Set the reference or embedded data based on what we read
        if (reference.HasValue)
        {
            layer.Reference = reference;
        }
        else if (embeddedData.HasValue)
        {
            layer.EmbeddedData = embeddedData;
        }

        return layer;
    }

    private async Task WriteCustomPropertiesAsync(BinaryWriter writer)
    {
        writer.Write(_customProperties.Count);
        foreach (var kvp in _customProperties)
        {
            writer.Write(kvp.Key);
            var json = JsonSerializer.Serialize(kvp.Value);
            writer.Write(json);
        }
        await Task.CompletedTask;
    }

    private async Task ReadCustomPropertiesAsync(BinaryReader reader)
    {
        try
        {
            var count = reader.ReadInt32();
            _customProperties.Clear();

            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var json = reader.ReadString();
                var value = JsonSerializer.Deserialize<object>(json);
                _customProperties[key] = value!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read custom properties, continuing without them");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the <see cref="TxtcResource"/>.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
