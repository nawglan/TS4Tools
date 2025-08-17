using FluentAssertions;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Unit tests for AssemblyInfo record functionality.
/// </summary>
public sealed class AssemblyInfoTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var fullName = "TestAssembly, Version=1.0.0.0";
        var location = "/path/to/assembly.dll";
        var exportedTypes = new List<string> { "TestType1", "TestType2" };
        var referencedAssemblies = new List<string> { "System.Core", "mscorlib" };
        var properties = new Dictionary<string, string> { ["Key1"] = "Value1", ["Key2"] = "Value2" };

        // Act
        var assemblyInfo = new AssemblyInfo(
            fullName,
            location,
            exportedTypes,
            referencedAssemblies,
            properties);

        // Assert
        assemblyInfo.FullName.Should().Be(fullName);
        assemblyInfo.Location.Should().Be(location);
        assemblyInfo.ExportedTypes.Should().BeEquivalentTo(exportedTypes);
        assemblyInfo.ReferencedAssemblies.Should().BeEquivalentTo(referencedAssemblies);
        assemblyInfo.Properties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void Constructor_WithEmptyCollections_ShouldInitializeCorrectly()
    {
        // Arrange
        var fullName = "TestAssembly";
        var location = "";
        var exportedTypes = new List<string>();
        var referencedAssemblies = new List<string>();
        var properties = new Dictionary<string, string>();

        // Act
        var assemblyInfo = new AssemblyInfo(
            fullName,
            location,
            exportedTypes,
            referencedAssemblies,
            properties);

        // Assert
        assemblyInfo.FullName.Should().Be(fullName);
        assemblyInfo.Location.Should().BeEmpty();
        assemblyInfo.ExportedTypes.Should().BeEmpty();
        assemblyInfo.ReferencedAssemblies.Should().BeEmpty();
        assemblyInfo.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var fullName = "TestAssembly";
        var location = "/path/to/assembly.dll";
        var exportedTypes = new List<string> { "Type1" };
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var assemblyInfo1 = new AssemblyInfo(fullName, location, exportedTypes, referencedAssemblies, properties);
        var assemblyInfo2 = new AssemblyInfo(fullName, location, exportedTypes, referencedAssemblies, properties);

        // Act & Assert
        assemblyInfo1.Should().Be(assemblyInfo2);
        assemblyInfo1.GetHashCode().Should().Be(assemblyInfo2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentFullNames_ShouldNotBeEqual()
    {
        // Arrange
        var location = "/path/to/assembly.dll";
        var exportedTypes = new List<string> { "Type1" };
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var assemblyInfo1 = new AssemblyInfo("Assembly1", location, exportedTypes, referencedAssemblies, properties);
        var assemblyInfo2 = new AssemblyInfo("Assembly2", location, exportedTypes, referencedAssemblies, properties);

        // Act & Assert
        assemblyInfo1.Should().NotBe(assemblyInfo2);
    }

    [Fact]
    public void Equality_WithDifferentExportedTypes_ShouldNotBeEqual()
    {
        // Arrange
        var fullName = "TestAssembly";
        var location = "/path/to/assembly.dll";
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var exportedTypes1 = new List<string> { "Type1" };
        var exportedTypes2 = new List<string> { "Type2" };

        var assemblyInfo1 = new AssemblyInfo(fullName, location, exportedTypes1, referencedAssemblies, properties);
        var assemblyInfo2 = new AssemblyInfo(fullName, location, exportedTypes2, referencedAssemblies, properties);

        // Act & Assert
        assemblyInfo1.Should().NotBe(assemblyInfo2);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulRepresentation()
    {
        // Arrange
        var fullName = "TestAssembly, Version=1.0.0.0";
        var location = "/path/to/assembly.dll";
        var exportedTypes = new List<string> { "TestType" };
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var assemblyInfo = new AssemblyInfo(fullName, location, exportedTypes, referencedAssemblies, properties);

        // Act
        var stringRepresentation = assemblyInfo.ToString();

        // Assert
        stringRepresentation.Should().Contain(fullName);
        stringRepresentation.Should().Contain(location);
    }

    [Fact]
    public void Deconstruction_ShouldWorkCorrectly()
    {
        // Arrange
        var fullName = "TestAssembly";
        var location = "/path/to/assembly.dll";
        var exportedTypes = new List<string> { "Type1" };
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var assemblyInfo = new AssemblyInfo(fullName, location, exportedTypes, referencedAssemblies, properties);

        // Act
        var (deconstructedFullName, deconstructedLocation, deconstructedTypes, deconstructedRefs, deconstructedProps) = assemblyInfo;

        // Assert
        deconstructedFullName.Should().Be(fullName);
        deconstructedLocation.Should().Be(location);
        deconstructedTypes.Should().BeEquivalentTo(exportedTypes);
        deconstructedRefs.Should().BeEquivalentTo(referencedAssemblies);
        deconstructedProps.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void Collections_ShouldBeReadOnly()
    {
        // Arrange
        var exportedTypes = new List<string> { "Type1" };
        var referencedAssemblies = new List<string> { "System.Core" };
        var properties = new Dictionary<string, string> { ["Key"] = "Value" };

        var assemblyInfo = new AssemblyInfo("TestAssembly", "", exportedTypes, referencedAssemblies, properties);

        // Act & Assert
        assemblyInfo.ExportedTypes.Should().BeAssignableTo<IReadOnlyList<string>>();
        assemblyInfo.ReferencedAssemblies.Should().BeAssignableTo<IReadOnlyList<string>>();
        assemblyInfo.Properties.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
    }
}
