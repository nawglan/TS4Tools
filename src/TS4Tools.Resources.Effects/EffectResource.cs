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

using System.Collections.ObjectModel;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Implementation of IEffectResource for handling effect resources.
/// </summary>
public class EffectResource : IEffectResource
{
    private static readonly byte[] MagicBytes = [0x45, 0x46, 0x46, 0x58]; // "EFFX"

    private readonly List<EffectParameter> _parameters = [];
    private readonly List<EffectTexture> _textures = [];
    private readonly List<string> _contentFields =
    [
        "EffectType",
        "EffectName",
        "BlendMode",
        "IsEnabled",
        "Duration",
        "Priority",
        "Parameters",
        "Textures"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectResource"/> class.
    /// </summary>
    public EffectResource()
    {
        EffectType = EffectType.None;
        EffectName = string.Empty;
        BlendMode = BlendMode.Normal;
        IsEnabled = true;
        Duration = 1.0f;
        Priority = 0;
        Parameters = new ReadOnlyCollection<EffectParameter>(_parameters);
        Textures = new ReadOnlyCollection<EffectTexture>(_textures);
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public EffectResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ReadFromStream(stream);
    }

    /// <inheritdoc />
    public EffectType EffectType { get; set; }

    /// <inheritdoc />
    public string EffectName { get; set; } = string.Empty;

    /// <inheritdoc />
    public BlendMode BlendMode { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<EffectParameter> Parameters { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<EffectTexture> Textures { get; private set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public float Duration { get; set; }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EffectResource));

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
                throw new ObjectDisposedException(nameof(EffectResource));

            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields;

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Adds a parameter to the effect.
    /// </summary>
    /// <param name="parameter">The parameter to add</param>
    public void AddParameter(EffectParameter parameter)
    {
        _parameters.Add(parameter);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a parameter from the effect.
    /// </summary>
    /// <param name="parameter">The parameter to remove</param>
    /// <returns>True if the parameter was removed</returns>
    public bool RemoveParameter(EffectParameter parameter)
    {
        var removed = _parameters.Remove(parameter);
        if (removed)
            OnResourceChanged();
        return removed;
    }

    /// <summary>
    /// Clears all parameters.
    /// </summary>
    public void ClearParameters()
    {
        if (_parameters.Count > 0)
        {
            _parameters.Clear();
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Adds a texture to the effect.
    /// </summary>
    /// <param name="texture">The texture to add</param>
    public void AddTexture(EffectTexture texture)
    {
        _textures.Add(texture);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a texture from the effect.
    /// </summary>
    /// <param name="texture">The texture to remove</param>
    /// <returns>True if the texture was removed</returns>
    public bool RemoveTexture(EffectTexture texture)
    {
        var removed = _textures.Remove(texture);
        if (removed)
            OnResourceChanged();
        return removed;
    }

    /// <summary>
    /// Clears all textures.
    /// </summary>
    public void ClearTextures()
    {
        if (_textures.Count > 0)
        {
            _textures.Clear();
            OnResourceChanged();
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetFieldValue(_contentFields[index]);
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "EffectType" => TypedValue.Create(EffectType),
            "EffectName" => TypedValue.Create(EffectName),
            "BlendMode" => TypedValue.Create(BlendMode),
            "IsEnabled" => TypedValue.Create(IsEnabled),
            "Duration" => TypedValue.Create(Duration),
            "Priority" => TypedValue.Create(Priority),
            "Parameters" => TypedValue.Create(Parameters.Count),
            "Textures" => TypedValue.Create(Textures.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        SetFieldValue(_contentFields[index], value);
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "EffectType":
                if (value.Value is EffectType effectType)
                    EffectType = effectType;
                else if (value.Value is int effectIntValue)
                    EffectType = (EffectType)effectIntValue;
                break;
            case "EffectName":
                EffectName = value.Value?.ToString() ?? string.Empty;
                break;
            case "BlendMode":
                if (value.Value is BlendMode blendMode)
                    BlendMode = blendMode;
                else if (value.Value is int blendIntValue)
                    BlendMode = (BlendMode)blendIntValue;
                break;
            case "IsEnabled":
                if (value.Value is bool boolValue)
                    IsEnabled = boolValue;
                break;
            case "Duration":
                if (value.Value is float floatValue)
                    Duration = floatValue;
                else if (value.Value is double doubleValue)
                    Duration = (float)doubleValue;
                break;
            case "Priority":
                if (value.Value is int priorityIntValue)
                    Priority = priorityIntValue;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }

        OnResourceChanged();
    }

    /// <summary>
    /// Loads the effect resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Copy stream to memory stream for processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        ReadFromStream(memoryStream);
    }

    private void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);

        // Handle empty stream gracefully
        if (stream.Length == 0)
        {
            // Initialize with default values for empty stream
            EffectType = EffectType.None;
            BlendMode = BlendMode.Normal;
            Parameters = new List<EffectParameter>();
            Textures = new List<EffectTexture>();
            return;
        }

        // Read and validate magic bytes
        if (stream.Length < 4)
        {
            throw new InvalidDataException("Invalid effect resource format");
        }

        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException("Invalid effect resource format");
        }

        // Read effect data
        EffectType = (EffectType)reader.ReadInt32();
        var nameLength = reader.ReadInt32();
        EffectName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
        BlendMode = (BlendMode)reader.ReadInt32();
        IsEnabled = reader.ReadBoolean();
        Duration = reader.ReadSingle();
        Priority = reader.ReadInt32();

        // Read parameters
        var parameterCount = reader.ReadInt32();
        _parameters.Clear();
        for (int i = 0; i < parameterCount; i++)
        {
            var paramNameLength = reader.ReadInt32();
            var paramName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(paramNameLength));
            var paramValue = reader.ReadSingle();
            _parameters.Add(new EffectParameter(paramName, "float", paramValue));
        }

        // Read textures
        var textureCount = reader.ReadInt32();
        _textures.Clear();
        for (int i = 0; i < textureCount; i++)
        {
            var textureNameLength = reader.ReadInt32();
            var textureName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(textureNameLength));
            var textureSlot = reader.ReadInt32();
            _textures.Add(new EffectTexture(textureName, (uint)textureSlot, 0));
        }
    }

    private void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        // Write magic bytes
        writer.Write(MagicBytes);

        // Write effect data
        writer.Write((int)EffectType);
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(EffectName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write((int)BlendMode);
        writer.Write(IsEnabled);
        writer.Write(Duration);
        writer.Write(Priority);

        // Write parameters
        writer.Write(_parameters.Count);
        foreach (var param in _parameters)
        {
            var paramNameBytes = System.Text.Encoding.UTF8.GetBytes(param.Name);
            writer.Write(paramNameBytes.Length);
            writer.Write(paramNameBytes);
            writer.Write((float)param.Value);
        }

        // Write textures
        writer.Write(_textures.Count);
        foreach (var texture in _textures)
        {
            var textureNameBytes = System.Text.Encoding.UTF8.GetBytes(texture.TextureName);
            writer.Write(textureNameBytes.Length);
            writer.Write(textureNameBytes);
            writer.Write((int)texture.TextureIndex);
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _disposed = true;
        }
    }
}
