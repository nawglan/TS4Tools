using TS4Tools;

namespace TS4Tools.UI.ViewModels;

public sealed class ResourceItemViewModel : ViewModelBase
{
    private static readonly Dictionary<uint, string> KnownTypes = new()
    {
        { 0x220557DA, "String Table (STBL)" },
        { 0x0166038C, "Name Map" },
        { 0x545AC67A, "Audio (SNR)" },
        { 0x01D10F34, "Rig (BSRF)" },
        { 0x015A1849, "Geometry (GEOM)" },
        { 0x00B2D882, "Image (DST)" },
        { 0x00B00000, "Image (PNG)" },
        { 0x034AEECB, "CAS Part" },
        { 0x0418FE2A, "Catalog Object" },
        { 0x02D5DF13, "SimData" },
        { 0x6017E896, "Tuning (XML)" },
        { 0x73E93EEB, "Tuning Instance" }
    };

    public ResourceKey Key { get; }

    public string TypeName => KnownTypes.TryGetValue(Key.ResourceType, out var name)
        ? name
        : $"Unknown (0x{Key.ResourceType:X8})";

    public string DisplayKey => $"T: 0x{Key.ResourceType:X8} | G: 0x{Key.ResourceGroup:X8} | I: 0x{Key.Instance:X16}";

    public ResourceItemViewModel(ResourceKey key)
    {
        Key = key;
    }
}
