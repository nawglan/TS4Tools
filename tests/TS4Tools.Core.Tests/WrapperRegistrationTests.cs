using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Registry tests against the real TS4Tools.Wrappers assembly.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 129-154 (+TextResources.txt, 105 lines)
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ImageResource.cs lines 71-96 (+ImageResources.txt, 48 lines)
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs lines 277-282 (0x00B2D882)
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs lines 329-335
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/DataResource/DataResource.cs lines 806-812 (0x545AC67A)
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/UserCAStPresetResource/UserCAStPresetResource.cs lines 239-246 (0x0591B1AF)
/// </summary>
public class WrapperRegistrationTests
{
    private static readonly Assembly WrappersAssembly = typeof(TextResourceFactory).Assembly;

    private static ResourceHandlerRegistry CreateWrappersRegistry()
    {
        var registry = new ResourceHandlerRegistry();
        registry.DiscoverHandlers(WrappersAssembly);
        return registry;
    }

    /// <summary>
    /// Factories that legitimately have no registration. Adding to this list requires a legacy justification.
    /// </summary>
    private static readonly Type[] IntentionallyUnregistered =
    [
        // Legacy ObjectCatalogResource is an abstract base with no AResourceHandler
        // (legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectCatalogResource.cs lines 30-342);
        // concrete catalog types register their own IDs.
        typeof(TS4Tools.Wrappers.CatalogResource.ObjectCatalogResourceFactory),
    ];

    private static readonly uint[] ThumbnailOverlap = [0x3C1AF1F2, 0x5B282D45, 0xCD9DE247];

    [Fact]
    public void EmbeddedTypeLists_MatchLegacyLineCounts()
    {
        // TextResources.txt has 105 lines, ImageResources.txt 48, each starting with a unique type id.
        TextResourceFactory.SupportedTypes.Should().HaveCount(105);
        ImageResourceFactory.SupportedTypes.Should().HaveCount(48);
        TextResourceFactory.SupportedTypes.Should().Contain([0x0069453Eu, 0x03B33DDFu, 0x02D5DF13u]);
        ImageResourceFactory.SupportedTypes.Should().Contain([0x0580A2B4u, 0x2F7D0002u, 0xFCEAB65Bu]);
    }

    [SkippableFact]
    public void EmbeddedTypeLists_AreVerbatimCopiesOfLegacy()
    {
        static string Embedded(string name)
        {
            using var s = WrappersAssembly.GetManifestResourceStream(name)!;
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }

        var root = FindRepoRoot();
        var legacy = Path.Combine(root, "legacy_references", "Sims4Tools", "s4pi Wrappers");
        Skip.IfNot(Directory.Exists(legacy), "legacy references not available");

        Embedded(TextResourceFactory.TypeListResourceName)
            .Should().Be(File.ReadAllText(Path.Combine(legacy, "TextResource", "TextResources.txt")));
        Embedded(ImageResourceFactory.TypeListResourceName)
            .Should().Be(File.ReadAllText(Path.Combine(legacy, "ImageResource", "ImageResources.txt")));
    }

    [Fact]
    public void AllTextTypes_ResolveToTextFactory()
    {
        var registry = CreateWrappersRegistry();

        foreach (var type in TextResourceFactory.SupportedTypes)
            registry.GetFactory(type).Should().BeOfType<TextResourceFactory>($"0x{type:X8} is in TextResources.txt");
    }

    [Fact]
    public void JazzTuning_ResolvesToText()
    {
        var registry = CreateWrappersRegistry();

        registry.GetFactory(0x02D5DF13).Should().BeOfType<TextResourceFactory>();
    }

    [Fact]
    public void AllImageTypes_ResolveToImageFactory_ExceptThumbnailOverlap()
    {
        var registry = CreateWrappersRegistry();

        foreach (var type in ImageResourceFactory.SupportedTypes)
        {
            if (ThumbnailOverlap.Contains(type))
                registry.GetFactory(type).Should().BeOfType<ThumbnailResourceFactory>(
                    $"0x{type:X8} is also claimed by ThumbnailResourceHandler, whose attribute claim outranks the list");
            else
                registry.GetFactory(type).Should().BeOfType<ImageResourceFactory>($"0x{type:X8} is in ImageResources.txt");
        }
    }

    [Fact]
    public void ThumbnailOverlap_ImageClaimIsKeptAsFallback()
    {
        var registry = CreateWrappersRegistry();

        foreach (var type in ThumbnailOverlap)
        {
            registry.DisableWrapper(type, typeof(ThumbnailResourceFactory));
            registry.GetFactory(type).Should().BeOfType<ImageResourceFactory>();
        }
    }

    [Fact]
    public void Dst_ResolvesToImageFactory()
    {
        var registry = CreateWrappersRegistry();

        registry.GetFactory(0x00B2D882).Should().BeOfType<ImageResourceFactory>();
        registry.CreateNewResource(0x00B2D882).Should().BeOfType<ImageResource>();
    }

    [Fact]
    public void SimDataAndUserCAStPreset_AreRegistered()
    {
        var registry = CreateWrappersRegistry();

        registry.GetFactory(0x545AC67A).Should().BeOfType<SimDataResourceFactory>();
        registry.GetFactory(0x0591B1AF).Should().BeOfType<UserCAStPresetResourceFactory>();
    }

    [Fact]
    public void EveryWrapperFactory_HasARegistration()
    {
        // Guards against factories that exist but are never registered (the original bug).
        var factoryTypes = WrappersAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IResourceFactory).IsAssignableFrom(t))
            .Where(t => !IntentionallyUnregistered.Contains(t))
            .ToList();
        factoryTypes.Should().NotBeEmpty();

        var registry = CreateWrappersRegistry();
        var claimed = registry.TypeMap.Select(kv => kv.Value).ToHashSet();

        foreach (var type in factoryTypes)
        {
            bool marked = type.GetCustomAttributes<ResourceHandlerAttribute>().Any()
                || typeof(IResourceTypeProvider).IsAssignableFrom(type);
            marked.Should().BeTrue($"{type.FullName} implements IResourceFactory but has no [ResourceHandler] or IResourceTypeProvider");
            claimed.Should().Contain(type, $"{type.FullName} should claim at least one resource type");
        }
    }

    [Fact]
    public void EveryWrapperFactory_WinsAtLeastOneType()
    {
        var registry = CreateWrappersRegistry();
        var winners = registry.RegisteredTypes.Select(t => registry.GetFactory(t)!.GetType()).ToHashSet();

        foreach (var type in registry.TypeMap.Select(kv => kv.Value).Distinct())
            winners.Should().Contain(type, $"{type.FullName} is registered but never selected for any type");
    }

    [Fact]
    public void Discovery_IsIndependentOfScanOrder()
    {
        var a = CreateWrappersRegistry();
        var b = CreateWrappersRegistry();
        b.DiscoverHandlers(WrappersAssembly); // re-scan is a no-op

        a.TypeMap.Should().Equal(b.TypeMap);
        foreach (var type in a.RegisteredTypes)
            a.GetFactory(type)!.GetType().Should().Be(b.GetFactory(type)!.GetType());
    }

    [Fact]
    public void CreateDefault_FindsWrappersByName()
    {
        var registry = ResourceHandlerRegistry.CreateDefault();

        registry.GetFactory(0x03B33DDF).Should().BeOfType<TextResourceFactory>();
        registry.GetFactory(0x545AC67A).Should().BeOfType<SimDataResourceFactory>();
    }

    [Fact]
    public void Shared_ResolvesWrappers()
    {
        ResourceHandlerRegistry.Shared.GetFactory(0x0591B1AF).Should().BeOfType<UserCAStPresetResourceFactory>();
    }

    [Fact]
    public void LoadWrappersFromFolder_UsesLoadedAssemblyAndReportsNativeDll()
    {
        var dir = Directory.CreateTempSubdirectory("ts4tools-plugins-");
        try
        {
            File.Copy(WrappersAssembly.Location, Path.Combine(dir.FullName, "TS4Tools.Wrappers.dll"));
            File.WriteAllBytes(Path.Combine(dir.FullName, "native.dll"), [0x4D, 0x5A, 0, 1, 2, 3, 4, 5]);

            var registry = new ResourceHandlerRegistry();
            var errors = registry.LoadWrappersFromFolder(dir.FullName);

            errors.Should().ContainSingle();
            errors[0].Path.Should().EndWith("native.dll");
            errors[0].IsNotManagedAssembly.Should().BeTrue();

            // The already-loaded assembly is used, so factory types unify with the host's.
            registry.GetFactory(0x03B33DDF).Should().BeOfType<TextResourceFactory>();
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadWrappersFromFolder_MissingFolder_ReturnsError()
    {
        var registry = new ResourceHandlerRegistry();

        var errors = registry.LoadWrappersFromFolder(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        errors.Should().ContainSingle().Which.Message.Should().Contain("does not exist");
    }

    [Fact]
    public void SetDisabledWrappers_LegacyFormat_FallsBackToDefault()
    {
        var registry = CreateWrappersRegistry();
        string entry = $"0x545AC67A:{typeof(SimDataResourceFactory).FullName}\n";

        var unmatched = registry.SetDisabledWrappers([entry, "0x12345678:No.Such.Type", "garbage"]);

        unmatched.Should().Equal("0x12345678:No.Such.Type", "garbage");
        registry.GetFactory(0x545AC67A).Should().BeNull();
        registry.GetFactoryOrDefault(0x545AC67A).Should().BeOfType<DefaultResourceFactory>();
        registry.GetDisabledWrappers().Should().Equal($"0x545AC67A:{typeof(SimDataResourceFactory).FullName}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
