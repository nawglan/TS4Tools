// Source: legacy_references/Sims4Tools/s4pi Wrappers/DefaultResource/DefaultResource.cs lines 26-60 and
// legacy_references/Sims4Tools/s4pi/Interfaces/AResourceHandler.cs lines 23-39 (shape of a legacy wrapper plugin:
// (int APIversion, Stream s) constructor, UnParse, OnResourceChanged, a string Value content field, and an
// AResourceHandler listing the wrapper's resource type strings)

using s4pi.Interfaces;

// Written like an s4pi plugin: only "using s4pi.Interfaces;", outside the TS4Tools namespaces.
namespace LegacyTestPlugin;

/// <summary>
/// A legacy-style wrapper for a 4-byte little-endian counter.
/// </summary>
public class CounterResource : AResource
{
    /// <summary>The (otherwise unused) resource type this plugin handles.</summary>
    public const uint TypeId = 0x7E570001;

    private const int recommendedApiVersion = 1;

    private uint counter;

    public CounterResource(int APIversion, Stream s) : base(APIversion, s)
    {
        if (stream == null)
        {
            stream = UnParse();
            OnResourceChanged(this, EventArgs.Empty);
        }
        stream.Position = 0;
        Parse(stream);
    }

    public override int RecommendedApiVersion => recommendedApiVersion;

    private void Parse(Stream s)
    {
        var r = new BinaryReader(s);
        counter = s.Length >= 4 ? r.ReadUInt32() : 0;
    }

    protected override Stream UnParse()
    {
        var ms = new MemoryStream();
        new BinaryWriter(ms).Write(counter);
        ms.Position = 0;
        return ms;
    }

    [ElementPriority(1)]
    public uint Counter
    {
        get => counter;
        set
        {
            if (counter == value) return;
            counter = value;
            OnResourceChanged(this, EventArgs.Empty);
        }
    }

    public string Value => ValueBuilder;
}

/// <summary>
/// The plugin's handler: maps <see cref="CounterResource"/> to its resource type, plus a type TS4Tools already
/// handles (STBL) and the default "*", which the bridge must not let the plugin take over.
/// </summary>
public class CounterResourceHandler : AResourceHandler
{
    public CounterResourceHandler()
    {
        Add(typeof(CounterResource), ["0x7E570001", "0x220557DA", "*"]);
    }
}
