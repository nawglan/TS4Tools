using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Represents a BC4A5044 Clip Header resource for The Sims 4.
/// </summary>
public sealed class ClipHeaderResource : IClipHeaderResource
{
    private Stream? _stream;
    private readonly List<string> _explicitNamespaces = new();
    private readonly List<SlotAssignment> _slotAssignments = new();
    private readonly List<ClipEvent> _events = new();
    private readonly List<ClipData> _clipData = new();
    private readonly List<string> _contentFields = new();

    /// <summary>Gets or sets the version of the clip format.</summary>
    public uint Version { get; set; }
    /// <summary>Gets or sets the flags associated with this clip.</summary>
    public uint Flags { get; set; }
    /// <summary>Gets or sets the duration of the animation clip in seconds.</summary>
    public float Duration { get; set; }
    /// <summary>Gets or sets the initial offset quaternion (rotation).</summary>
    public string? InitialOffsetQ { get; set; } = string.Empty;
    /// <summary>Gets or sets the initial offset translation (position).</summary>
    public string? InitialOffsetT { get; set; } = string.Empty;
    /// <summary>Gets or sets the reference namespace hash.</summary>
    public uint ReferenceNamespaceHash { get; set; }
    /// <summary>Gets or sets the surface namespace hash.</summary>
    public uint SurfaceNamespaceHash { get; set; }
    /// <summary>Gets or sets the surface joint name hash.</summary>
    public uint SurfaceJointNameHash { get; set; }
    /// <summary>Gets or sets the surface child namespace hash.</summary>
    public uint SurfaceChildNamespaceHash { get; set; }
    /// <summary>Gets or sets the name of the animation clip.</summary>
    public string? ClipName { get; set; } = string.Empty;
    /// <summary>Gets or sets the rig name if available.</summary>
    public string? RigName { get; set; } = string.Empty;
    /// <summary>Gets or sets the actor name for this clip.</summary>
    public string? ActorName { get; set; } = string.Empty;
    /// <summary>Gets or sets the rig hash reference.</summary>
    public ulong Rig { get; set; }
    /// <summary>Gets or sets the slot assignment count.</summary>
    public int SlotAssignmentCount { get; set; }
    /// <summary>Gets or sets the event count.</summary>
    public int EventCount { get; set; }
    /// <summary>Gets or sets the clip data count.</summary>
    public int ClipDataCount { get; set; }
    /// <summary>Gets or sets whether this clip has valid data.</summary>
    public bool HasValidData { get; set; }

    /// <summary>Gets the complete JSON representation of this clip.</summary>
    public string? JsonData
    {
        get => ToJsonString();
        set { /* Read-only for now */ }
    }

    /// <summary>Gets the explicit namespaces used in this clip.</summary>
    public IReadOnlyList<string> ExplicitNamespaces => _explicitNamespaces.AsReadOnly();

    /// <summary>Gets the resource content as a Stream.</summary>
    public Stream Stream => ResourceStream;
    
    /// <summary>Gets the resource content as a byte array.</summary>
    public byte[] AsBytes 
    { 
        get 
        {
            using var stream = ResourceStream;
            if (stream is MemoryStream ms)
                return ms.ToArray();
                
            var bytes = new byte[stream.Length];
            stream.Position = 0;
            var totalRead = 0;
            while (totalRead < bytes.Length)
            {
                var bytesRead = stream.Read(bytes, totalRead, bytes.Length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }
            return bytes;
        }
    }
    
    /// <summary>Raised if the resource is changed.</summary>
    public event EventHandler? ResourceChanged
    {
        add { /* Event not currently used */ }
        remove { /* Event not currently used */ }
    }

    /// <summary>Gets the version of the API in use.</summary>
    public int RequestedApiVersion => 1;
    /// <summary>Gets the best supported version of the API available.</summary>
    public int RecommendedApiVersion => 1;

    /// <summary>Gets the list of content field names.</summary>
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();
    
    /// <summary>Gets or sets the value of a content field by index.</summary>
    /// <param name="index">The index of the field.</param>
    /// <returns>The typed value of the field.</returns>
    public TypedValue this[int index] 
    { 
        get 
        {
            var value = GetPropertyByIndex(index);
            return value != null ? new TypedValue(value.GetType(), value) : new TypedValue(typeof(object), null);
        }
        set => SetPropertyByIndex(index, value.Value); 
    }
    
    /// <summary>Gets or sets the value of a content field by name.</summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>The typed value of the field.</returns>
    public TypedValue this[string name] 
    { 
        get 
        {
            var value = GetPropertyAsObject(name);
            return value != null ? new TypedValue(value.GetType(), value) : new TypedValue(typeof(object), null);
        }
        set => SetProperty(name, value.Value); 
    }

    /// <summary>Initializes a new instance of the ClipHeaderResource class from a stream.</summary>
    /// <param name="stream">The stream containing the resource data.</param>
    public ClipHeaderResource(Stream stream)
    {
        InitializeContentFields();
        ReadFromStream(stream);
    }

    /// <summary>Initializes a new instance of the ClipHeaderResource class.</summary>
    public ClipHeaderResource()
    {
        InitializeContentFields();
        _stream = new MemoryStream();
        HasValidData = false;
    }

    private void InitializeContentFields()
    {
        _contentFields.AddRange(new[]
        {
            nameof(Version), nameof(Flags), nameof(Duration),
            nameof(ClipName), nameof(RigName), nameof(ActorName),
            nameof(InitialOffsetQ), nameof(InitialOffsetT),
            nameof(ReferenceNamespaceHash), nameof(SurfaceNamespaceHash),
            nameof(SurfaceJointNameHash), nameof(SurfaceChildNamespaceHash),
            nameof(Rig), nameof(HasValidData), nameof(JsonData)
        });
    }

    private object? GetPropertyByIndex(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            return null;
        return GetPropertyAsObject(_contentFields[index]);
    }

    private void SetPropertyByIndex(int index, object? value)
    {
        if (index < 0 || index >= _contentFields.Count)
            return;
        SetProperty(_contentFields[index], value);
    }

    private object? GetPropertyAsObject(string name) => name switch
    {
        nameof(Version) => Version,
        nameof(Flags) => Flags,
        nameof(Duration) => Duration,
        nameof(InitialOffsetQ) => InitialOffsetQ,
        nameof(InitialOffsetT) => InitialOffsetT,
        nameof(ReferenceNamespaceHash) => ReferenceNamespaceHash,
        nameof(SurfaceNamespaceHash) => SurfaceNamespaceHash,
        nameof(SurfaceJointNameHash) => SurfaceJointNameHash,
        nameof(SurfaceChildNamespaceHash) => SurfaceChildNamespaceHash,
        nameof(ClipName) => ClipName,
        nameof(RigName) => RigName,
        nameof(ActorName) => ActorName,
        nameof(Rig) => Rig,
        nameof(HasValidData) => HasValidData,
        nameof(JsonData) => JsonData,
        _ => null
    };

    /// <summary>Gets a specific property from the clip data.</summary>
    /// <param name="name">The name of the property to retrieve.</param>
    /// <returns>The property value as a string, or null if not found.</returns>
    public string? GetProperty(string name) => name switch
    {
        nameof(Version) => Version.ToString(),
        nameof(Flags) => Flags.ToString(),
        nameof(Duration) => Duration.ToString(),
        nameof(InitialOffsetQ) => InitialOffsetQ,
        nameof(InitialOffsetT) => InitialOffsetT,
        nameof(ReferenceNamespaceHash) => ReferenceNamespaceHash.ToString(),
        nameof(SurfaceNamespaceHash) => SurfaceNamespaceHash.ToString(),
        nameof(SurfaceJointNameHash) => SurfaceJointNameHash.ToString(),
        nameof(SurfaceChildNamespaceHash) => SurfaceChildNamespaceHash.ToString(),
        nameof(ClipName) => ClipName,
        nameof(RigName) => RigName,
        nameof(ActorName) => ActorName,
        nameof(Rig) => Rig.ToString(),
        nameof(HasValidData) => HasValidData.ToString(),
        nameof(JsonData) => JsonData,
        _ => null
    };

    /// <summary>Sets a property in the clip data.</summary>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value to set.</param>
    public void SetProperty(string name, object? value)
    {
        switch (name)
        {
            case nameof(Version):
                if (value is uint uintVal) Version = uintVal;
                else if (value is int intVal && intVal >= 0) Version = (uint)intVal;
                break;
            case nameof(Flags):
                if (value is uint flagsVal) Flags = flagsVal;
                else if (value is int intFlags && intFlags >= 0) Flags = (uint)intFlags;
                break;
            case nameof(Duration):
                if (value is float floatVal) Duration = floatVal;
                else if (value is double doubleVal) Duration = (float)doubleVal;
                else if (value is int intVal) Duration = intVal;
                break;
            case nameof(InitialOffsetQ):
                if (value is string qVal) InitialOffsetQ = qVal;
                break;
            case nameof(InitialOffsetT):
                if (value is string tVal) InitialOffsetT = tVal;
                break;
            case nameof(ReferenceNamespaceHash):
                if (value is uint refVal) ReferenceNamespaceHash = refVal;
                else if (value is int intRef && intRef >= 0) ReferenceNamespaceHash = (uint)intRef;
                break;
            case nameof(SurfaceNamespaceHash):
                if (value is uint surfVal) SurfaceNamespaceHash = surfVal;
                else if (value is int intSurf && intSurf >= 0) SurfaceNamespaceHash = (uint)intSurf;
                break;
            case nameof(SurfaceJointNameHash):
                if (value is uint jointVal) SurfaceJointNameHash = jointVal;
                else if (value is int intJoint && intJoint >= 0) SurfaceJointNameHash = (uint)intJoint;
                break;
            case nameof(SurfaceChildNamespaceHash):
                if (value is uint childVal) SurfaceChildNamespaceHash = childVal;
                else if (value is int intChild && intChild >= 0) SurfaceChildNamespaceHash = (uint)intChild;
                break;
            case nameof(ClipName):
                if (value is string clipVal) ClipName = clipVal;
                break;
            case nameof(RigName):
                if (value is string rigVal) RigName = rigVal;
                break;
            case nameof(ActorName):
                if (value is string actorVal) ActorName = actorVal;
                break;
        }
    }

    /// <summary>Gets the resource stream.</summary>
    public Stream ResourceStream
    {
        get
        {
            if (_stream == null)
                return new MemoryStream();

            _stream.Position = 0;
            return _stream;
        }
    }

    /// <summary>Gets the JSON string representation of this clip.</summary>
    /// <returns>A JSON string containing all clip data.</returns>
    public string ToJsonString()
    {
        var data = new
        {
            Version,
            Flags,
            Duration,
            ClipName,
            RigName,
            ActorName,
            InitialOffsetQ,
            InitialOffsetT,
            ReferenceNamespaceHash = $"0x{ReferenceNamespaceHash:X8}",
            SurfaceNamespaceHash = $"0x{SurfaceNamespaceHash:X8}",
            SurfaceJointNameHash = $"0x{SurfaceJointNameHash:X8}",
            SurfaceChildNamespaceHash = $"0x{SurfaceChildNamespaceHash:X8}",
            SlotAssignments = _slotAssignments.Select(s => new
            {
                ContainerSlotName = s.ContainerSlotName,
                ContainerSlotType = s.ContainerSlotType,
                ActorSlotName = s.ActorSlotName,
                ActorSlotType = s.ActorSlotType
            }).ToArray(),
            Events = _events.Select(e => new
            {
                Name = e.Name,
                StartTime = e.StartTime,
                Duration = e.Duration,
                EventType = e.EventType,
                EventData = e.EventData
            }).ToArray(),
            Clip = new
            {
                Data = _clipData.Select(c => new
                {
                    ChannelName = c.ChannelName,
                    NumTicks = c.NumTicks,
                    TicksPerFrame = c.TicksPerFrame
                }).ToArray()
            },
            ExplicitNamespaces = _explicitNamespaces.ToArray(),
            HasValidData
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private void ReadFromStream(Stream stream)
    {
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        try
        {
            // Binary format based on ClipResource.Parse from Sims4Tools
            Version = reader.ReadUInt32();
            Flags = reader.ReadUInt32();
            Duration = reader.ReadSingle();

            // Read Quaternion (4 floats)
            var qx = reader.ReadSingle();
            var qy = reader.ReadSingle();
            var qz = reader.ReadSingle();
            var qw = reader.ReadSingle();
            InitialOffsetQ = $"{qx},{qy},{qz},{qw}";

            // Read Vector3 (3 floats)
            var vx = reader.ReadSingle();
            var vy = reader.ReadSingle();
            var vz = reader.ReadSingle();
            InitialOffsetT = $"{vx},{vy},{vz}";

            // Version-dependent fields
            if (Version >= 5)
                ReferenceNamespaceHash = reader.ReadUInt32();

            if (Version >= 10)
            {
                SurfaceNamespaceHash = reader.ReadUInt32();
                SurfaceJointNameHash = reader.ReadUInt32();
            }

            if (Version >= 11)
                SurfaceChildNamespaceHash = reader.ReadUInt32();

            if (Version >= 7)
                ClipName = ReadString32(reader);

            RigName = ReadString32(reader);

            if (Version >= 4)
            {
                var namespaceCount = reader.ReadUInt32();
                _explicitNamespaces.Clear();
                
                // Sanity check namespace count
                if (namespaceCount > 1000)
                {
                    namespaceCount = 0; // Skip this section
                }
                
                for (int i = 0; i < namespaceCount; i++)
                {
                    var ns = ReadString32(reader);
                    if (!string.IsNullOrEmpty(ns))
                        _explicitNamespaces.Add(ns);
                }
            }

            // Read slot assignments
            ReadSlotAssignments(reader);

            // Read events  
            ReadEvents(reader);

            // Read remaining clip data
            ReadClipData(reader);
            ReadClipData(reader);

            // Extract actor name from clip name
            if (!string.IsNullOrEmpty(ClipName))
            {
                var parts = ClipName.Split('_');
                if (parts.Length > 0)
                    ActorName = parts[^1];
            }

            HasValidData = true;
        }
        catch (Exception)
        {
            HasValidData = false;
        }

        // Copy to internal stream
        stream.Position = 0;
        _stream = new MemoryStream();
        stream.CopyTo(_stream);
        _stream.Position = 0;
    }

    private string ReadString32(BinaryReader reader)
    {
        if (reader.BaseStream.Position >= reader.BaseStream.Length - 4)
            return string.Empty;

        try
        {
            var length = reader.ReadUInt32();
            if (length == 0 || length > 1024)
                return string.Empty;

            if (reader.BaseStream.Position + length > reader.BaseStream.Length)
                return string.Empty;

            var bytes = reader.ReadBytes((int)length);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
        catch
        {
            return string.Empty;
        }
    }

    private void ReadSlotAssignments(BinaryReader reader)
    {
        if (reader.BaseStream.Position >= reader.BaseStream.Length - 4)
            return;

        try
        {
            var slotCount = reader.ReadUInt32();
            SlotAssignmentCount = (int)slotCount;
            _slotAssignments.Clear();

            if (slotCount > 1000) // Sanity check
                return;

            for (int i = 0; i < slotCount && reader.BaseStream.Position < reader.BaseStream.Length - 16; i++)
            {
                var slot = new SlotAssignment
                {
                    ContainerSlotName = ReadString32(reader),
                    ContainerSlotType = reader.ReadUInt32(),
                    ActorSlotName = ReadString32(reader),
                    ActorSlotType = reader.ReadUInt32()
                };

                _slotAssignments.Add(slot);
            }
        }
        catch
        {
            // Skip if failed to parse
        }
    }

    private void ReadEvents(BinaryReader reader)
    {
        if (reader.BaseStream.Position >= reader.BaseStream.Length - 4)
            return;

        try
        {
            var eventCount = reader.ReadUInt32();
            EventCount = (int)eventCount;
            _events.Clear();

            if (eventCount > 1000) // Sanity check
                return;

            for (int i = 0; i < eventCount && reader.BaseStream.Position < reader.BaseStream.Length - 32; i++)
            {
                var evt = new ClipEvent
                {
                    Name = ReadString32(reader),
                    StartTime = reader.ReadSingle(),
                    Duration = reader.ReadSingle(),
                    EventType = reader.ReadUInt32(),
                    EventData = ReadString32(reader)
                };

                _events.Add(evt);
            }
        }
        catch
        {
            // Skip if failed to parse
        }
    }

    private void ReadClipData(BinaryReader reader)
    {
        if (reader.BaseStream.Position >= reader.BaseStream.Length - 16)
            return;

        try
        {
            // Try to read basic clip data structure
            var clipDataCount = reader.ReadUInt32();
            ClipDataCount = (int)clipDataCount;

            if (clipDataCount > 1000) // Sanity check
                return;

            for (int i = 0; i < clipDataCount && reader.BaseStream.Position < reader.BaseStream.Length - 20; i++)
            {
                var clipData = new ClipData
                {
                    ChannelName = ReadString32(reader),
                    NumTicks = reader.ReadUInt32(),
                    TicksPerFrame = reader.ReadSingle()
                };

                // Skip actual tick data for now as it can be very large
                var tickDataSize = clipData.NumTicks * 12; // Estimate: 3 floats per tick
                if (reader.BaseStream.Position + tickDataSize < reader.BaseStream.Length)
                {
                    reader.BaseStream.Seek(tickDataSize, SeekOrigin.Current);
                }

                _clipData.Add(clipData);
            }
        }
        catch
        {
            // Skip if failed to parse
        }
    }

    /// <summary>Releases all resources used by the ClipHeaderResource.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}

/// <summary>
/// Represents a slot assignment in an animation clip.
/// </summary>
public class SlotAssignment
{
    /// <summary>Gets or sets the container slot name.</summary>
    public string ContainerSlotName { get; set; } = string.Empty;
    /// <summary>Gets or sets the container slot type.</summary>
    public uint ContainerSlotType { get; set; }
    /// <summary>Gets or sets the actor slot name.</summary>
    public string ActorSlotName { get; set; } = string.Empty;
    /// <summary>Gets or sets the actor slot type.</summary>
    public uint ActorSlotType { get; set; }
}

/// <summary>
/// Represents an event in an animation clip.
/// </summary>
public class ClipEvent
{
    /// <summary>Gets or sets the event name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the start time.</summary>
    public float StartTime { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public float Duration { get; set; }
    /// <summary>Gets or sets the event type.</summary>
    public uint EventType { get; set; }
    /// <summary>Gets or sets the event data.</summary>
    public string EventData { get; set; } = string.Empty;
}

/// <summary>
/// Represents clip data containing animation channels.
/// </summary>
public class ClipData
{
    /// <summary>Gets or sets the channel name.</summary>
    public string ChannelName { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of ticks.</summary>
    public uint NumTicks { get; set; }
    /// <summary>Gets or sets the ticks per frame.</summary>
    public float TicksPerFrame { get; set; }
}
