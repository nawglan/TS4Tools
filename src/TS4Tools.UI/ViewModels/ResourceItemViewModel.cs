using TS4Tools;
using TS4Tools.Extensions;

namespace TS4Tools.UI.ViewModels;

public sealed class ResourceItemViewModel : ViewModelBase
{
    private static readonly Dictionary<uint, string> KnownTypes = new()
    {
        { 0x220557DA, "String Table (STBL)" },
        { 0x0166038C, "Name Map" },
        { 0x545AC67A, "SimData (DATA)" },
        { 0x01D10F34, "Model LOD (MLOD)" },
        { 0x015A1849, "Geometry (GEOM)" },
        { 0x00B2D882, "Image (DDS/DST)" },
        { 0x00B00000, "Image (PNG)" },
        { 0x034AEECB, "CAS Part" },
        { 0x319E4F1D, "Catalog Object (COBJ)" },
        { 0x0418FE2A, "Fence (CFEN)" },
        { 0x03B33DDF, "Tuning (XML)" },
        { 0x6017E896, "Tuning Instance (XML)" },
        { 0x73E93EEB, "Tuning Instance" }
    };

    public ResourceKey Key { get; }

    public uint FileSize { get; }

    /// <summary>
    /// Human-readable name for the instance, if available from NameMap.
    /// </summary>
    public string? InstanceName { get; }

    /// <summary>
    /// Whether this resource has a known instance name.
    /// </summary>
    public bool HasInstanceName => InstanceName != null;

    /// <summary>
    /// Short legacy tag for the resource type (e.g. "STBL"), or "UNKN".
    /// </summary>
    // Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs (s4pe Tag column)
    public string Tag => ExtList.GetTag(Key.ResourceType);

    /// <summary>
    /// Human-readable type name: a friendly name when available, otherwise the legacy
    /// Extensions.txt tag, otherwise "Unknown".
    /// </summary>
    public string TypeName => KnownTypes.TryGetValue(Key.ResourceType, out var name)
        ? name
        : ExtList.IsKnown(Key.ResourceType)
            ? $"{ExtList.GetTag(Key.ResourceType)} (0x{Key.ResourceType:X8})"
            : $"Unknown (0x{Key.ResourceType:X8})";

    /// <summary>
    /// Display string for the instance ID, showing name if available.
    /// </summary>
    public string InstanceDisplay => HasInstanceName
        ? $"{InstanceName}"
        : $"0x{Key.Instance:X16}";

    /// <summary>
    /// Full display key with type, group, and instance.
    /// </summary>
    public string DisplayKey => HasInstanceName
        ? $"G: 0x{Key.ResourceGroup:X8} | {InstanceName} (0x{Key.Instance:X16})"
        : $"G: 0x{Key.ResourceGroup:X8} | I: 0x{Key.Instance:X16}";

    public ResourceItemViewModel(ResourceKey key, uint fileSize, string? instanceName = null)
    {
        Key = key;
        FileSize = fileSize;
        InstanceName = instanceName;
    }
}
