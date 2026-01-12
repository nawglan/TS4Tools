using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="AbstractCatalogResource"/> base class.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/AbstractCatalogResource.cs
/// - Abstract base class for complex catalog resources (COBJ, CCOL)
/// - Contains aural materials, placement flags, colors, and object behavior fields
/// - Tests use CobjResource as the concrete implementation (simplest derived type)
/// </summary>
public class AbstractCatalogResourceTests
{
    private static readonly ResourceKey TestKey = new(CobjResource.TypeId, 0, 0);

    #region Constants Tests

    [Fact]
    public void DefaultVersion_IsExpected()
    {
        AbstractCatalogResource.DefaultVersion.Should().Be(0x19);
    }

    [Fact]
    public void FnvOffsetBasis_IsExpected()
    {
        AbstractCatalogResource.FnvOffsetBasis.Should().Be(0x811C9DC5);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void CreateEmpty_Version_IsDefault()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(AbstractCatalogResource.DefaultVersion);
    }

    [Fact]
    public void CreateEmpty_CommonBlock_IsInitialized()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.CommonBlock.Should().NotBeNull();
    }

    [Fact]
    public void CreateEmpty_AuralMaterials_HaveFnvBasis()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.AuralMaterialsVersion.Should().Be(0x1);
        resource.AuralMaterials1.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        resource.AuralMaterials2.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        resource.AuralMaterials3.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
    }

    [Fact]
    public void CreateEmpty_AuralProperties_HaveDefaults()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.AuralPropertiesVersion.Should().Be(0x2);
        resource.AuralQuality.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        resource.AuralAmbientObject.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
    }

    [Fact]
    public void CreateEmpty_Colors_IsEmpty()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Colors.Should().NotBeNull();
        resource.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void CreateEmpty_FallbackObjectKey_IsEmpty()
    {
        var resource = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.FallbackObjectKey.Should().Be(TgiReference.Empty);
    }

    #endregion

    #region Aural Properties Version Tests

    [Fact]
    public void RoundTrip_AuralPropertiesVersion1_OnlyHasQuality()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AuralPropertiesVersion = 1;
        original.AuralQuality = 0xAABBCCDD;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.AuralPropertiesVersion.Should().Be(1);
        parsed.AuralQuality.Should().Be(0xAABBCCDD);
        // AuralAmbientObject should remain default (not parsed in version 1)
    }

    [Fact]
    public void RoundTrip_AuralPropertiesVersion2_HasAmbientObject()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AuralPropertiesVersion = 2;
        original.AuralQuality = 0xAABBCCDD;
        original.AuralAmbientObject = 0x11223344;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.AuralPropertiesVersion.Should().Be(2);
        parsed.AuralQuality.Should().Be(0xAABBCCDD);
        parsed.AuralAmbientObject.Should().Be(0x11223344);
    }

    [Fact]
    public void RoundTrip_AuralPropertiesVersion3_HasAmbienceFields()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AuralPropertiesVersion = 3;
        original.AuralQuality = 0xAABBCCDD;
        original.AuralAmbientObject = 0x11223344;
        original.AmbienceFileInstanceId = 0x1234567890ABCDEF;
        original.IsOverrideAmbience = 1;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.AuralPropertiesVersion.Should().Be(3);
        parsed.AmbienceFileInstanceId.Should().Be(0x1234567890ABCDEF);
        parsed.IsOverrideAmbience.Should().Be(1);
    }

    [Fact]
    public void RoundTrip_AuralPropertiesVersion4_HasUnknown01()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AuralPropertiesVersion = 4;
        original.Unknown01 = 0xAB;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.AuralPropertiesVersion.Should().Be(4);
        parsed.Unknown01.Should().Be(0xAB);
    }

    #endregion

    #region Placement Fields Tests

    [Fact]
    public void RoundTrip_PlacementFlags_PreservesValues()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.PlacementFlagsHigh = 0xAAAAAAAA;
        original.PlacementFlagsLow = 0xBBBBBBBB;
        original.SlotTypeSet = 0xCCCCCCCCDDDDDDDD;
        original.SlotDecoSize = 0x12;
        original.CatalogGroup = 0xEEEEEEEEFFFFFFFF;
        original.StateUsage = 0x34;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.PlacementFlagsHigh.Should().Be(0xAAAAAAAA);
        parsed.PlacementFlagsLow.Should().Be(0xBBBBBBBB);
        parsed.SlotTypeSet.Should().Be(0xCCCCCCCCDDDDDDDD);
        parsed.SlotDecoSize.Should().Be(0x12);
        parsed.CatalogGroup.Should().Be(0xEEEEEEEEFFFFFFFF);
        parsed.StateUsage.Should().Be(0x34);
    }

    #endregion

    #region Object Behavior Fields Tests

    [Fact]
    public void RoundTrip_ObjectBehavior_PreservesValues()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.FenceHeight = 0x12345678;
        original.IsStackable = 1;
        original.CanItemDepreciate = 1;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.FenceHeight.Should().Be(0x12345678);
        parsed.IsStackable.Should().Be(1);
        parsed.CanItemDepreciate.Should().Be(1);
    }

    #endregion

    #region Version-Dependent Fields Tests

    [Fact]
    public void RoundTrip_Version0x19_HasFallbackObjectKey()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x19;
        original.FallbackObjectKey = new TgiReference(0x1234567890ABCDEF, 0xAABBCCDD, 0x11223344);

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.Version.Should().Be(0x19);
        parsed.FallbackObjectKey.Instance.Should().Be(0x1234567890ABCDEF);
        parsed.FallbackObjectKey.Type.Should().Be(0xAABBCCDD);
        parsed.FallbackObjectKey.Group.Should().Be(0x11223344);
    }

    [Fact]
    public void RoundTrip_VersionBelow0x19_NoFallbackObjectKey()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x18;
        // FallbackObjectKey won't be serialized

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.Version.Should().Be(0x18);
        // Should remain at default empty value
        parsed.FallbackObjectKey.Should().Be(TgiReference.Empty);
    }

    #endregion

    #region Colors Tests

    [Fact]
    public void RoundTrip_MultipleColors_PreservesAll()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFFF0000); // Red
        original.Colors.Add(0xFF00FF00); // Green
        original.Colors.Add(0xFF0000FF); // Blue
        original.Colors.Add(0xFFFFFFFF); // White

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.Colors.Count.Should().Be(4);
        parsed.Colors[0].Should().Be(0xFFFF0000);
        parsed.Colors[1].Should().Be(0xFF00FF00);
        parsed.Colors[2].Should().Be(0xFF0000FF);
        parsed.Colors[3].Should().Be(0xFFFFFFFF);
    }

    #endregion

    #region Unused Fields Tests

    [Fact]
    public void RoundTrip_UnusedFields_PreservesValues()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unused0 = 0x11111111;
        original.Unused1 = 0x22222222;
        original.Unused2 = 0x33333333;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.Unused0.Should().Be(0x11111111);
        parsed.Unused1.Should().Be(0x22222222);
        parsed.Unused2.Should().Be(0x33333333);
    }

    #endregion
}
