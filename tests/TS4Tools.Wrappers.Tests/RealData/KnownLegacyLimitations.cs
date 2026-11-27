// Source: legacy_references/Sims4Tools/s4pi Wrappers (each entry cites the legacy code that makes byte-identical round-trip impossible)

namespace TS4Tools.Wrappers.Tests.RealData;

/// <summary>
/// Places where the legacy s4pi wrapper itself does not reproduce the original bytes, and the port follows it.
/// </summary>
/// <remarks>
/// Every entry cites the legacy code responsible. An entry here states that the port is faithful and the
/// difference is inherited from legacy - it is not a place to park unexplained failures. Entries are as
/// narrow as possible: <see cref="LegacyRewrite"/> predicts legacy's exact output, and a key list limits an
/// accepted mismatch to the resources it was verified on.
/// </remarks>
internal static class KnownLegacyLimitations
{
    /// <summary>One documented limitation.</summary>
    /// <param name="Reason">Explanation with legacy file/line references.</param>
    /// <param name="Instances">When set, only these instances may mismatch (must still be re-parse stable).</param>
    /// <param name="AcceptParseFailure">Legacy fails to parse these resources too.</param>
    internal sealed record Limitation(string Reason, IReadOnlySet<ulong>? Instances = null, bool AcceptParseFailure = false);

    private static readonly Dictionary<uint, Limitation> Entries = new()
    {
        [0x033260E3] = new(
            "TkMk (`RCOLResources.txt`): the 18 resources of this type in ClientFullBuild0 are not RCOL data (they start " +
            "with 0x6A10BC4A and contain no TkMk tag). Legacy `GenericRCOLResource.cs` lines 75-80 fails the same way " +
            "(negative/huge chunk count); parse failures of this type are accepted.",
            AcceptParseFailure: true),
    };

    public static IReadOnlyDictionary<uint, Limitation> All => Entries;

    public static string? Reason(uint resourceType) => Entries.TryGetValue(resourceType, out var r) ? r.Reason : null;

    /// <summary>Whether a re-parse-stable mismatch of this resource is a documented legacy limitation.</summary>
    public static bool AcceptsMismatch(ResourceKey key) =>
        Entries.TryGetValue(key.ResourceType, out var l) && !l.AcceptParseFailure &&
        (l.Instances is null || l.Instances.Contains(key.Instance));

    /// <summary>Whether a parse failure of this type is a documented legacy limitation.</summary>
    public static bool AcceptsParseFailure(uint resourceType) =>
        Entries.TryGetValue(resourceType, out var l) && l.AcceptParseFailure;

    /// <summary>Description of <see cref="LegacyRewrite"/> for the report.</summary>
    public const string RcolRewriteReason =
        "RCOL family, single-chunk resources: legacy always recomputes the one index entry as Position = 0x2C + " +
        "externalCount * 16, Length = rest of the stream, and replaces a chunk TGI type of 0 with the type for the " +
        "chunk's tag (`GenericRCOLResource.cs` lines 84-94), then writes those values (UnParse, line 118 ff.). The " +
        "harness applies exactly these changes to the original before comparing (every standalone GEOM has type 0; " +
        "some mod BONDs store a short length).";

    /// <summary>
    /// Predicts legacy's output for a resource whose original bytes legacy is known to normalize, or returns
    /// <paramref name="original"/> unchanged. <paramref name="written"/> supplies the one value legacy derives
    /// itself (the chunk type looked up from its tag).
    /// </summary>
    public static byte[] LegacyRewrite(string family, byte[] original, byte[] written)
    {
        // Source: GenericRCOLResource.cs lines 72-94
        if (family != "Rcol" || original.Length < 0x2C || written.Length < 0x20)
            return original;

        int countResources = BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(0x0C));
        int countChunks = BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(0x10));
        if (countChunks != 1 || countResources < 0 || countResources > (original.Length - 0x2C) / 16)
            return original;

        var expected = (byte[])original.Clone();
        int indexAt = 0x24 + (countResources * 16);
        uint position = 0x2C + ((uint)countResources * 16);
        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(indexAt), position);
        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(indexAt + 4), (int)(original.Length - position));

        // Chunk TGI is ITG at 0x14: instance (8), type at 0x1C, group.
        if (BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(0x1C)) == 0)
            written.AsSpan(0x1C, 4).CopyTo(expected.AsSpan(0x1C));

        return expected;
    }
}
