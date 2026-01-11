// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 671-746

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Parameter Definition block - defines a parameter in the state machine.
/// Resource Type: 0x02EEDB46
/// Tag: "S_PD"
/// </summary>
public sealed class JazzParameterDefinitionBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.ParameterDefinition;

    private const uint DefaultVersion = 0x0100;

    /// <inheritdoc/>
    public override string Tag => "S_PD";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0100).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Name hash for this parameter.</summary>
    public uint NameHash { get; set; }

    /// <summary>Default value for this parameter.</summary>
    public uint DefaultValue { get; set; }

    /// <summary>Creates an empty block.</summary>
    public JazzParameterDefinitionBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzParameterDefinitionBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 711-718
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        DefaultValue = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'P');
        writer.Write((byte)'D');

        // Source: JazzResource.cs lines 721-728
        writer.Write(Version);
        writer.Write(NameHash);
        writer.Write(DefaultValue);

        return ms.ToArray();
    }
}
