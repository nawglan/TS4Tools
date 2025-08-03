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

using System.Collections;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Default resource implementation that can handle any resource type.
/// Provides basic stream access without specific resource type knowledge.
/// </summary>
internal sealed class DefaultResource : IResource, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly int _apiVersion;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResource"/> class.
    /// </summary>
    /// <param name="apiVersion">API version</param>
    /// <param name="stream">Resource data stream</param>
    public DefaultResource(int apiVersion, Stream? stream = null)
    {
        _apiVersion = apiVersion;
        
        if (stream != null)
        {
            _stream = new MemoryStream();
            stream.CopyTo(_stream);
            _stream.Position = 0;
        }
        else
        {
            _stream = new MemoryStream();
        }
    }
    
    /// <inheritdoc />
    public int RequestedApiVersion => _apiVersion;
    
    /// <inheritdoc />
    public int RecommendedApiVersion => _apiVersion;
    
    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => Array.Empty<string>();
    
    /// <inheritdoc />
    public Stream Stream => _stream;
    
    /// <inheritdoc />
    public byte[] AsBytes => _stream.ToArray();
    
    /// <inheritdoc />
    public event EventHandler? ResourceChanged;
    
    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => throw new NotSupportedException("Default resource does not support content field access by string index");
        set => throw new NotSupportedException("Default resource does not support content field access by string index");
    }
    
    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => throw new NotSupportedException("Default resource does not support content field access by integer index");
        set => throw new NotSupportedException("Default resource does not support content field access by integer index");
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _stream?.Dispose();
    }
}
