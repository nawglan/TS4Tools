using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for ClipResource parsing and serialization.
/// </summary>
public class ClipResourceTests
{
    /// <summary>
    /// Creates a minimal valid clip for version 14.
    /// </summary>
    private static byte[] CreateMinimalClipV14()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version and flags
        writer.Write(14u); // Version
        writer.Write(0u);  // Flags

        // Duration
        writer.Write(2.5f);

        // Initial offset quaternion (identity)
        writer.Write(0f); // X
        writer.Write(0f); // Y
        writer.Write(0f); // Z
        writer.Write(1f); // W

        // Initial offset translation
        writer.Write(1.0f); // X
        writer.Write(2.0f); // Y
        writer.Write(3.0f); // Z

        // Version 5+: Reference namespace hash
        writer.Write(0x12345678u);

        // Version 10+: Surface hashes
        writer.Write(0xAABBCCDDu); // SurfaceNamespaceHash
        writer.Write(0xDEADBEEFu); // SurfaceJointNameHash

        // Version 11+: Surface child namespace hash
        writer.Write(0xCAFEBABEu);

        // Version 7+: Clip name
        WriteString32(writer, "TestClip");

        // Rig namespace
        WriteString32(writer, "TestRig");

        // Version 4+: Explicit namespaces (0 count)
        writer.Write(0);

        // IK configuration (0 count)
        writer.Write(0);

        // Clip events (0 count)
        writer.Write(0);

        // Codec data length (0 = no codec data)
        writer.Write(0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a clip with IK slot assignments.
    /// </summary>
    private static byte[] CreateClipWithIkAssignments()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version 4 (minimal version with explicit namespaces and IK)
        writer.Write(4u);  // Version
        writer.Write(0u);  // Flags
        writer.Write(1.0f); // Duration

        // Quaternion
        writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
        // Translation
        writer.Write(0f); writer.Write(0f); writer.Write(0f);

        // Rig namespace
        WriteString32(writer, "IKTestRig");

        // Explicit namespaces (1)
        writer.Write(1);
        WriteString32(writer, "ExplicitNS");

        // IK configuration (2 slot assignments)
        writer.Write(2); // Count

        // Slot 1
        writer.Write((ushort)1); // ChainId
        writer.Write((ushort)0); // SlotId
        WriteString32(writer, "TargetObject1");
        WriteString32(writer, "TargetJoint1");

        // Slot 2
        writer.Write((ushort)2); // ChainId
        writer.Write((ushort)1); // SlotId
        WriteString32(writer, "TargetObject2");
        WriteString32(writer, "TargetJoint2");

        // Events (0)
        writer.Write(0);

        // Codec data (0)
        writer.Write(0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a clip with events.
    /// </summary>
    private static byte[] CreateClipWithEvents()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version 7 (has clip name)
        writer.Write(7u);
        writer.Write(0u);
        writer.Write(3.0f);

        // Quaternion and translation
        writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
        writer.Write(0f); writer.Write(0f); writer.Write(0f);

        // Version 5+: Reference namespace hash
        writer.Write(0x11111111u);

        // Version 7+: Clip name
        WriteString32(writer, "EventClip");

        // Rig namespace
        WriteString32(writer, "EventRig");

        // Explicit namespaces (0)
        writer.Write(0);

        // IK configuration (0)
        writer.Write(0);

        // Events (2)
        writer.Write(2);

        // Event 1: Censor event
        writer.Write((uint)ClipEventType.Censor); // Type
        writer.Write(16u); // Size (12 base + 4 type-specific)
        writer.Write(0u); // Unknown1
        writer.Write(0u); // Unknown2
        writer.Write(1.5f); // Timecode
        writer.Write(0.75f); // Unknown3 (censor-specific)

        // Event 2: Sound event (128 byte fixed string)
        writer.Write((uint)ClipEventType.Sound); // Type
        writer.Write(140u); // Size (12 base + 128 type-specific)
        writer.Write(1u); // Unknown1
        writer.Write(2u); // Unknown2
        writer.Write(0.5f); // Timecode
        // Fixed 128-byte sound name
        byte[] soundName = new byte[128];
        System.Text.Encoding.ASCII.GetBytes("test_sound").CopyTo(soundName, 0);
        writer.Write(soundName);

        // Codec data (0)
        writer.Write(0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a clip with S3CLIP codec data.
    /// </summary>
    private static byte[] CreateClipWithCodecData()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Minimal version 4 header
        writer.Write(4u);  // Version
        writer.Write(0u);  // Flags
        writer.Write(1.0f); // Duration
        writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
        writer.Write(0f); writer.Write(0f); writer.Write(0f);
        WriteString32(writer, "CodecRig");
        writer.Write(0); // Explicit namespaces
        writer.Write(0); // IK
        writer.Write(0); // Events

        // Codec data
        byte[] codecData = CreateMinimalCodecData();
        writer.Write(codecData.Length);
        writer.Write(codecData);

        return ms.ToArray();
    }

    private static byte[] CreateMinimalCodecData()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Format token (8 bytes): "_pilc3S_"
        writer.Write(System.Text.Encoding.ASCII.GetBytes("_pilc3S_"));

        // Version
        writer.Write(1u);

        // Flags
        writer.Write(0u);

        // Tick length
        writer.Write(0.033333f);

        // Num ticks
        writer.Write((ushort)30);

        // Padding
        writer.Write((ushort)0);

        // Channel count
        writer.Write(0u);

        // F1 palette size
        writer.Write(0u);

        // Offsets (all pointing past header to "empty" data)
        uint headerEnd = 48u;
        writer.Write(headerEnd); // Channel data offset
        writer.Write(headerEnd + 1); // F1 palette offset
        writer.Write(headerEnd + 2); // Name offset
        writer.Write(headerEnd + 4); // Source asset name offset

        // Minimal string data
        writer.Write((byte)'T');
        writer.Write((byte)0); // Null terminator for empty palette
        writer.Write((byte)0); // Name: empty
        writer.Write((byte)0);
        writer.Write((byte)0); // Source: empty
        writer.Write((byte)0);

        return ms.ToArray();
    }

    private static void WriteString32(BinaryWriter writer, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value ?? string.Empty);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    [Fact]
    public void Parse_MinimalClipV14_ParsesCorrectly()
    {
        // Arrange
        byte[] data = CreateMinimalClipV14();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        // Act
        var clip = new ClipResource(key, data);

        // Assert
        clip.IsValid.Should().BeTrue();
        clip.Version.Should().Be(14);
        clip.Duration.Should().BeApproximately(2.5f, 0.001f);
        clip.ClipName.Should().Be("TestClip");
        clip.RigNamespace.Should().Be("TestRig");
        clip.InitialOffsetQ.W.Should().Be(1f);
        clip.InitialOffsetT.X.Should().Be(1f);
        clip.ReferenceNamespaceHash.Should().Be(0x12345678u);
        clip.SurfaceNamespaceHash.Should().Be(0xAABBCCDDu);
        clip.SurfaceJointNameHash.Should().Be(0xDEADBEEFu);
        clip.SurfaceChildNamespaceHash.Should().Be(0xCAFEBABEu);
    }

    [Fact]
    public void Parse_ClipWithIkAssignments_ParsesSlotAssignments()
    {
        // Arrange
        byte[] data = CreateClipWithIkAssignments();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        // Act
        var clip = new ClipResource(key, data);

        // Assert
        clip.IsValid.Should().BeTrue();
        clip.SlotAssignments.Count.Should().Be(2);

        clip.SlotAssignments.Assignments[0].ChainId.Should().Be(1);
        clip.SlotAssignments.Assignments[0].SlotId.Should().Be(0);
        clip.SlotAssignments.Assignments[0].TargetObjectNamespace.Should().Be("TargetObject1");
        clip.SlotAssignments.Assignments[0].TargetJointName.Should().Be("TargetJoint1");

        clip.SlotAssignments.Assignments[1].ChainId.Should().Be(2);
        clip.SlotAssignments.Assignments[1].SlotId.Should().Be(1);
        clip.SlotAssignments.Assignments[1].TargetObjectNamespace.Should().Be("TargetObject2");

        clip.ExplicitNamespaces.Should().Contain("ExplicitNS");
    }

    [Fact]
    public void Parse_ClipWithEvents_ParsesEventTypes()
    {
        // Arrange
        byte[] data = CreateClipWithEvents();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        // Act
        var clip = new ClipResource(key, data);

        // Assert
        clip.IsValid.Should().BeTrue();
        clip.ClipEvents.Count.Should().Be(2);

        // Censor event
        clip.ClipEvents[0].Should().BeOfType<ClipEventCensor>();
        clip.ClipEvents[0].TypeId.Should().Be(ClipEventType.Censor);
        clip.ClipEvents[0].Timecode.Should().BeApproximately(1.5f, 0.001f);
        ((ClipEventCensor)clip.ClipEvents[0]).Unknown3.Should().BeApproximately(0.75f, 0.001f);

        // Sound event
        clip.ClipEvents[1].Should().BeOfType<ClipEventSound>();
        clip.ClipEvents[1].TypeId.Should().Be(ClipEventType.Sound);
        clip.ClipEvents[1].Timecode.Should().BeApproximately(0.5f, 0.001f);
        ((ClipEventSound)clip.ClipEvents[1]).SoundName.Should().Be("test_sound");
    }

    [Fact]
    public void Parse_ClipWithCodecData_ParsesCodecHeader()
    {
        // Arrange
        byte[] data = CreateClipWithCodecData();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        // Act
        var clip = new ClipResource(key, data);

        // Assert
        clip.IsValid.Should().BeTrue();
        clip.CodecData.IsValid.Should().BeTrue();
        clip.CodecData.FormatToken.Should().Be("_pilc3S_");
        clip.CodecData.NumTicks.Should().Be(30);
        clip.CodecData.TickLength.Should().BeApproximately(0.033333f, 0.0001f);
    }

    [Fact]
    public void Serialize_MinimalClip_RoundTrips()
    {
        // Arrange
        byte[] originalData = CreateMinimalClipV14();
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);
        var clip = new ClipResource(key, originalData);

        // Act
        byte[] serialized = clip.Data.ToArray();
        var reparsed = new ClipResource(key, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.Version.Should().Be(clip.Version);
        reparsed.Duration.Should().Be(clip.Duration);
        reparsed.ClipName.Should().Be(clip.ClipName);
        reparsed.RigNamespace.Should().Be(clip.RigNamespace);
    }

    [Fact]
    public void Parse_EmptyData_HandlesGracefully()
    {
        // Arrange
        var key = new ResourceKey(ClipResource.TypeId, 0, 0);

        // Act
        var clip = new ClipResource(key, ReadOnlyMemory<byte>.Empty);

        // Assert
        clip.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClipEventFactory_CreatesCorrectTypes()
    {
        // Test the factory method creates correct derived types
        ClipEvent.Create(ClipEventType.Sound, 140, new byte[152], 0).Should().BeOfType<ClipEventSound>();
        ClipEvent.Create(ClipEventType.Effect, 280, new byte[292], 0).Should().BeOfType<ClipEventEffect>();
        ClipEvent.Create(ClipEventType.Censor, 16, new byte[28], 0).Should().BeOfType<ClipEventCensor>();
        ClipEvent.Create(ClipEventType.Script, 24, new byte[36], 0).Should().BeOfType<ClipEventScript>();
        ClipEvent.Create(ClipEventType.Snap, 20, new byte[32], 0).Should().BeOfType<ClipEventSnap>();
        ClipEvent.Create(ClipEventType.DoubleModifierSound, 144, new byte[156], 0).Should().BeOfType<ClipEventDoubleModifierSound>();
        ClipEvent.Create((ClipEventType)999, 16, new byte[28], 0).Should().BeOfType<ClipEventUnknown>();
    }
}
