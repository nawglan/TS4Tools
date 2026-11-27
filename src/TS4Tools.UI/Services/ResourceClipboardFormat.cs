namespace TS4Tools.UI.Services;

/// <summary>
/// A copied resource: its key and name, and its (uncompressed) data.
/// </summary>
/// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 96-101 (MyDataFormat: tgin + data)</remarks>
public sealed record CopiedResource(Tgin Tgin, byte[] Data);

/// <summary>
/// Binary clipboard payload for copied resources.
/// </summary>
/// <remarks>
/// Legacy s4pe puts a BinaryFormatter-serialized MyDataFormat (one resource, "x-application/s3pe.singleFile")
/// or List&lt;MyDataFormat&gt; ("x-application/s3pe.batch") on the clipboard
/// (legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1484-1538). BinaryFormatter is neither portable
/// nor safe, so the same content (TGIN + data per resource) is written in a small explicit format:
/// magic "TS4R", version, count, then per resource type/group/instance, UTF-8 name and data.
/// </remarks>
public static class ResourceClipboardFormat
{
    private const uint Magic = 0x52345354; // "TS4R"
    private const int Version = 1;

    /// <summary>Upper bound on resources in one payload.</summary>
    public const int MaxResources = 100_000;

    /// <summary>Upper bound on a single resource's data size.</summary>
    public const int MaxResourceSize = 256 * 1024 * 1024;

    private const int MaxNameBytes = 64 * 1024;

    /// <summary>Serializes copied resources.</summary>
    public static byte[] Serialize(IReadOnlyList<CopiedResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(Magic);
            w.Write(Version);
            w.Write(resources.Count);
            foreach (var r in resources)
            {
                w.Write(r.Tgin.ResourceType);
                w.Write(r.Tgin.ResourceGroup);
                w.Write(r.Tgin.Instance);
                var name = Encoding.UTF8.GetBytes(r.Tgin.ResourceName ?? string.Empty);
                w.Write(r.Tgin.ResourceName != null);
                w.Write(name.Length);
                w.Write(name);
                w.Write(r.Data.Length);
                w.Write(r.Data);
            }
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes copied resources; returns null if the payload is not valid.
    /// </summary>
    public static IReadOnlyList<CopiedResource>? TryDeserialize(byte[]? payload)
    {
        if (payload == null || payload.Length < 12) return null;
        try
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var r = new BinaryReader(ms, Encoding.UTF8);
            if (r.ReadUInt32() != Magic || r.ReadInt32() != Version) return null;

            var count = r.ReadInt32();
            if (count < 0 || count > MaxResources) return null;

            var list = new List<CopiedResource>(Math.Min(count, 1024));
            for (var i = 0; i < count; i++)
            {
                var type = r.ReadUInt32();
                var group = r.ReadUInt32();
                var instance = r.ReadUInt64();
                var hasName = r.ReadBoolean();
                var nameLength = r.ReadInt32();
                if (nameLength < 0 || nameLength > MaxNameBytes || nameLength > ms.Length - ms.Position) return null;
                var name = Encoding.UTF8.GetString(r.ReadBytes(nameLength));
                var dataLength = r.ReadInt32();
                if (dataLength < 0 || dataLength > MaxResourceSize || dataLength > ms.Length - ms.Position) return null;
                var data = r.ReadBytes(dataLength);
                list.Add(new CopiedResource(new Tgin(type, group, instance, hasName ? name : null), data));
            }
            return list;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }
}
