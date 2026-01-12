namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating RcolResource instances.
/// Registers for all standalone RCOL resource types.
/// Source: RCOLResources.txt from legacy s4pi (types marked with Y for standalone)
/// </summary>
[ResourceHandler(RcolConstants.Modl)]        // 0x01661233 - MODL
[ResourceHandler(RcolConstants.Matd)]        // 0x01D0E75D - MATD
[ResourceHandler(RcolConstants.Mlod)]        // 0x01D10F34 - MLOD
[ResourceHandler(RcolConstants.Mtst)]        // 0x02019972 - MTST
[ResourceHandler(RcolConstants.Tree)]        // 0x021D7E8C - TREE
[ResourceHandler(RcolConstants.TkMk)]        // 0x033260E3 - TkMk
[ResourceHandler(RcolConstants.SlotAdjusts)] // 0x0355E0A6 - Slot Adjusts
[ResourceHandler(RcolConstants.Lite)]        // 0x03B4C61D - LITE
[ResourceHandler(RcolConstants.Anim)]        // 0x63A33EA7 - ANIM
[ResourceHandler(RcolConstants.Vpxy)]        // 0x736884F1 - VPXY
[ResourceHandler(RcolConstants.Rslt)]        // 0xD3044521 - RSLT
[ResourceHandler(RcolConstants.Ftpt)]        // 0xD382BF57 - FTPT
// Jazz State Machine types (from JazzResource.cs)
[ResourceHandler(RcolConstants.JazzStateMachine)]        // 0x02D5DF13 - S_SM
[ResourceHandler(RcolConstants.JazzStopAnimationNode)]   // 0x0344D438 - Stop
public sealed class RcolResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new RcolResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new RcolResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
