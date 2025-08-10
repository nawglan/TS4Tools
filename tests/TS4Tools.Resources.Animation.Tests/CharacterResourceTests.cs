using FluentAssertions;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class CharacterResourceTests : IDisposable
{
    private readonly CharacterResource _characterResource;

    public CharacterResourceTests()
    {
        _characterResource = new CharacterResource();
    }

    public void Dispose()
    {
        _characterResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var resource = new CharacterResource();

        // Assert
        resource.Should().NotBeNull();
        resource.CharacterType.Should().Be(CharacterType.None);
        resource.CharacterName.Should().BeEmpty();
        resource.AgeCategory.Should().Be(AgeCategory.None);
        resource.Gender.Should().Be(Gender.None);
        resource.Species.Should().Be(Species.None);
        resource.SupportsMorphing.Should().BeFalse();
        resource.Priority.Should().Be(0);
        resource.CharacterParts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new CharacterResource(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Constructor_WithStream_ShouldInitializeFromStream()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var resource = new CharacterResource(stream);
        resource.Should().NotBeNull();
    }

    [Fact]
    public void CharacterType_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedType = CharacterType.CasPart;

        // Act
        _characterResource.CharacterType = expectedType;

        // Assert
        _characterResource.CharacterType.Should().Be(expectedType);
    }

    [Fact]
    public void CharacterName_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const string expectedName = "SimCharacter";

        // Act
        _characterResource.CharacterName = expectedName;

        // Assert
        _characterResource.CharacterName.Should().Be(expectedName);
    }

    [Fact]
    public void AgeCategory_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedAge = AgeCategory.Adult;

        // Act
        _characterResource.AgeCategory = expectedAge;

        // Assert
        _characterResource.AgeCategory.Should().Be(expectedAge);
    }

    [Fact]
    public void Gender_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedGender = Gender.Female;

        // Act
        _characterResource.Gender = expectedGender;

        // Assert
        _characterResource.Gender.Should().Be(expectedGender);
    }

    [Fact]
    public void Species_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedSpecies = Species.Human;

        // Act
        _characterResource.Species = expectedSpecies;

        // Assert
        _characterResource.Species.Should().Be(expectedSpecies);
    }

    [Fact]
    public void SupportsMorphing_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const bool expectedSupportsMorphing = true;

        // Act
        _characterResource.SupportsMorphing = expectedSupportsMorphing;

        // Assert
        _characterResource.SupportsMorphing.Should().Be(expectedSupportsMorphing);
    }

    [Fact]
    public void Priority_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const int expectedPriority = 5;

        // Act
        _characterResource.Priority = expectedPriority;

        // Assert
        _characterResource.Priority.Should().Be(expectedPriority);
    }

    [Fact]
    public void CharacterParts_WhenInitialized_ShouldBeEmpty()
    {
        // Arrange & Act & Assert
        _characterResource.CharacterParts.Should().NotBeNull();
        _characterResource.CharacterParts.Should().BeEmpty();
    }

    [Fact]
    public void AddCharacterPart_WithValidPart_ShouldAddToCollection()
    {
        // Arrange
        var part = new CharacterPart(123, PartCategory.Hair, "TestHair", AgeCategory.Adult, Gender.Unisex, Species.Human, 0);

        // Act
        _characterResource.AddCharacterPart(part);

        // Assert
        _characterResource.CharacterParts.Should().Contain(part);
        _characterResource.CharacterParts.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveCharacterPart_WithExistingPart_ShouldRemoveFromCollection()
    {
        // Arrange
        var part = new CharacterPart(123, PartCategory.Hair, "TestHair", AgeCategory.Adult, Gender.Unisex, Species.Human, 0);
        _characterResource.AddCharacterPart(part);

        // Act
        var result = _characterResource.RemoveCharacterPart(part);

        // Assert
        result.Should().BeTrue();
        _characterResource.CharacterParts.Should().NotContain(part);
        _characterResource.CharacterParts.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCharacterPart_WithNonExistingPart_ShouldReturnFalse()
    {
        // Arrange
        var part = new CharacterPart(123, PartCategory.Hair, "TestHair", AgeCategory.Adult, Gender.Unisex, Species.Human, 0);

        // Act
        var result = _characterResource.RemoveCharacterPart(part);

        // Assert
        result.Should().BeFalse();
        _characterResource.CharacterParts.Should().BeEmpty();
    }

    [Fact]
    public void ClearCharacterParts_WithExistingParts_ShouldClearAllParts()
    {
        // Arrange
        var part1 = new CharacterPart(123, PartCategory.Hair, "Hair1", AgeCategory.Adult, Gender.Unisex, Species.Human, 0);
        var part2 = new CharacterPart(124, PartCategory.Top, "Shirt1", AgeCategory.Adult, Gender.Unisex, Species.Human, 1);

        _characterResource.AddCharacterPart(part1);
        _characterResource.AddCharacterPart(part2);

        // Act
        _characterResource.ClearCharacterParts();

        // Assert
        _characterResource.CharacterParts.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        await FluentActions.Awaiting(() => _characterResource.LoadFromStreamAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithEmptyStream_ShouldHandleGracefully()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _characterResource.LoadFromStreamAsync(stream))
            .Should().NotThrowAsync();
    }

    [Fact]
    public void AsBytes_ShouldReturnValidByteArray()
    {
        // Arrange
        _characterResource.CharacterType = CharacterType.CasPart;
        _characterResource.CharacterName = "TestCharacter";

        // Act
        var bytes = _characterResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Act
        var fields = _characterResource.ContentFields;

        // Assert
        fields.Should().NotBeNull();
        fields.Should().Contain("CharacterType");
        fields.Should().Contain("CharacterName");
        fields.Should().Contain("AgeCategory");
        fields.Should().Contain("Gender");
        fields.Should().Contain("Species");
        fields.Should().Contain("SupportsMorphing");
        fields.Should().Contain("Priority");
        fields.Should().Contain("CharacterParts");
    }

    [Fact]
    public void Indexer_WithValidFieldName_ShouldReturnCorrectValue()
    {
        // Arrange
        _characterResource.CharacterType = CharacterType.CasPart;

        // Act
        var value = _characterResource["CharacterType"];

        // Assert
        value.Value.Should().Be(CharacterType.CasPart);
    }

    [Fact]
    public void Indexer_WithInvalidFieldName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Action act = () => _ = _characterResource["InvalidField"];
        act.Should().Throw<ArgumentException>()
           .WithMessage("Unknown field: InvalidField*")
           .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void ResourceChanged_WhenPartAdded_ShouldFireEvent()
    {
        // Arrange
        var eventFired = false;
        _characterResource.ResourceChanged += (_, _) => eventFired = true;

        var part = new CharacterPart(123, PartCategory.Hair, "TestHair", AgeCategory.Adult, Gender.Unisex, Species.Human, 0);

        // Act
        _characterResource.AddCharacterPart(part);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldDisposeResourceCorrectly()
    {
        // Arrange
        var resource = new CharacterResource();

        // Act
        resource.Dispose();

        // Assert - Should throw when accessing disposed resource's Stream
        Action act = () => _ = resource.Stream;
        act.Should().Throw<ObjectDisposedException>();
    }
}
