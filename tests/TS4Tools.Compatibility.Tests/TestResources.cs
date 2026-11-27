// Source: legacy_references/Sims4Tools/s4pi Wrappers/DefaultResource/DefaultResource.cs lines 26-60
// (shape of a legacy wrapper + handler: (int APIversion, Stream s) constructor, UnParse, AResourceHandler type list)

using s4pi.Interfaces;

// Plain legacy-plugin style: only "using s4pi.Interfaces;", outside the TS4Tools namespaces.
namespace CompatTests;

/// <summary>
/// A legacy-style wrapper: a uint version followed by a TGIBlockList (count-prefixed, TGI order).
/// </summary>
public sealed class SampleResource : AResource
{
    public const uint TypeId = 0x1234ABCD;
    private const int recommendedApiVersion = 1;

    private uint version;
    private CountedTGIBlockList tgis;

    public SampleResource(int APIversion, Stream? s) : base(APIversion, s)
    {
        if (stream == null || stream.Length == 0)
        {
            version = 1;
            tgis = new CountedTGIBlockList(OnResourceChanged);
            stream = UnParse();
            OnResourceChanged(this, EventArgs.Empty);
        }
        stream.Position = 0;
        Parse(stream);
    }

    public bool IsDirty => dirty;

    public override int RecommendedApiVersion => recommendedApiVersion;

    [MemberNotNull(nameof(tgis))]
    private void Parse(Stream s)
    {
        using var r = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);
        version = r.ReadUInt32();
        int count = r.ReadInt32();
        tgis = new CountedTGIBlockList(OnResourceChanged, count, s);
    }

    protected override Stream UnParse()
    {
        var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(version);
            w.Write(tgis.Count);
        }
        tgis.UnParse(ms);
        ms.Position = 0;
        return ms;
    }

    [ElementPriority(1)]
    public uint Version
    {
        get => version;
        set
        {
            if (version != value)
            {
                version = value;
                OnResourceChanged(this, EventArgs.Empty);
            }
        }
    }

    [ElementPriority(2)]
    public CountedTGIBlockList TGIBlocks => tgis;

    public static byte[] Build(uint version, params (uint T, uint G, ulong I)[] keys)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(version);
        w.Write(keys.Length);
        foreach (var (t, g, i) in keys)
        {
            w.Write(t);
            w.Write(g);
            w.Write(i);
        }
        w.Flush();
        return ms.ToArray();
    }
}

public sealed class SampleResourceHandler : AResourceHandler
{
    public SampleResourceHandler()
    {
        Add(typeof(SampleResource), ["0x" + SampleResource.TypeId.ToString("X8", CultureInfo.InvariantCulture)]);
    }
}

/// <summary>
/// A TS4Tools-native factory, for testing the native bridge.
/// </summary>
public sealed class ReversingFactory : TS4Tools.IResourceFactory
{
    public TS4Tools.IResource Create(TS4Tools.ResourceKey key, ReadOnlyMemory<byte> data) => new ReversedResource(data.ToArray());

    public TS4Tools.IResource CreateEmpty(TS4Tools.ResourceKey key) => new ReversedResource([]);
}

/// <summary>
/// A native resource that stores its bytes reversed, so tests can tell it apart from the default wrapper.
/// </summary>
public sealed class ReversedResource(byte[] original) : TS4Tools.IResource
{
    private byte[] data = original.Reverse().ToArray();

    public ReadOnlyMemory<byte> Data => data.Reverse().ToArray();

    public bool IsDirty { get; private set; }

    public event EventHandler? Changed;

    public byte[] Stored => data;

    public void Set(byte[] value)
    {
        data = value.Reverse().ToArray();
        IsDirty = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { }
}
