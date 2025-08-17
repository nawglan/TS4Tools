using FluentAssertions;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class AnimationResourceTests : IDisposable
{
    private readonly AnimationResource _animationResource;

    public AnimationResourceTests()
    {
        _animationResource = new AnimationResource();
    }

    public void Dispose()
    {
        _animationResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var resource = new AnimationResource();

        // Assert
        resource.Should().NotBeNull();
        resource.AnimationType.Should().Be(AnimationType.None);
        resource.AnimationName.Should().BeEmpty();
        resource.Duration.Should().Be(0.0f);
        resource.FrameRate.Should().Be(30.0f);
        resource.PlayMode.Should().Be(AnimationPlayMode.Once);
        resource.BlendMode.Should().Be(AnimationBlendMode.Replace);
        resource.IsLooped.Should().BeFalse();
        resource.Priority.Should().Be(0);
        resource.Tracks.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => { var _ = new AnimationResource(null!); };
        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Constructor_WithStream_ShouldInitializeFromStream()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var resource = new AnimationResource(stream);
        resource.Should().NotBeNull();
    }

    [Fact]
    public void AnimationType_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedType = AnimationType.Clip;

        // Act
        _animationResource.AnimationType = expectedType;

        // Assert
        _animationResource.AnimationType.Should().Be(expectedType);
    }

    [Fact]
    public void AnimationName_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const string expectedName = "WalkCycle";

        // Act
        _animationResource.AnimationName = expectedName;

        // Assert
        _animationResource.AnimationName.Should().Be(expectedName);
    }

    [Fact]
    public void Duration_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const float expectedDuration = 2.5f;

        // Act
        _animationResource.Duration = expectedDuration;

        // Assert
        _animationResource.Duration.Should().Be(expectedDuration);
    }

    [Fact]
    public void FrameRate_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const float expectedFrameRate = 60.0f;

        // Act
        _animationResource.FrameRate = expectedFrameRate;

        // Assert
        _animationResource.FrameRate.Should().Be(expectedFrameRate);
    }

    [Fact]
    public void PlayMode_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedPlayMode = AnimationPlayMode.Loop;

        // Act
        _animationResource.PlayMode = expectedPlayMode;

        // Assert
        _animationResource.PlayMode.Should().Be(expectedPlayMode);
    }

    [Fact]
    public void BlendMode_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedBlendMode = AnimationBlendMode.Add;

        // Act
        _animationResource.BlendMode = expectedBlendMode;

        // Assert
        _animationResource.BlendMode.Should().Be(expectedBlendMode);
    }

    [Fact]
    public void IsLooped_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const bool expectedIsLooped = true;

        // Act
        _animationResource.IsLooped = expectedIsLooped;

        // Assert
        _animationResource.IsLooped.Should().Be(expectedIsLooped);
    }

    [Fact]
    public void Priority_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        const int expectedPriority = 10;

        // Act
        _animationResource.Priority = expectedPriority;

        // Assert
        _animationResource.Priority.Should().Be(expectedPriority);
    }

    [Fact]
    public void Tracks_WhenInitialized_ShouldBeEmpty()
    {
        // Arrange & Act & Assert
        _animationResource.Tracks.Should().NotBeNull();
        _animationResource.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void AddTrack_WithValidTrack_ShouldAddToCollection()
    {
        // Arrange
        var keyframes = new List<AnimationKeyframe>
        {
            new(0.0f, 1.0f, InterpolationType.Linear),
            new(1.0f, 2.0f, InterpolationType.Linear)
        };
        var track = new AnimationTrack("TestBone", "Position.X", keyframes);

        // Act
        _animationResource.AddTrack(track);

        // Assert
        _animationResource.Tracks.Should().Contain(track);
        _animationResource.Tracks.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTrack_WithExistingTrack_ShouldRemoveFromCollection()
    {
        // Arrange
        var keyframes = new List<AnimationKeyframe>
        {
            new(0.0f, 1.0f, InterpolationType.Linear)
        };
        var track = new AnimationTrack("TestBone", "Position.X", keyframes);
        _animationResource.AddTrack(track);

        // Act
        var result = _animationResource.RemoveTrack(track);

        // Assert
        result.Should().BeTrue();
        _animationResource.Tracks.Should().NotContain(track);
        _animationResource.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTrack_WithNonExistingTrack_ShouldReturnFalse()
    {
        // Arrange
        var keyframes = new List<AnimationKeyframe>
        {
            new(0.0f, 1.0f, InterpolationType.Linear)
        };
        var track = new AnimationTrack("TestBone", "Position.X", keyframes);

        // Act
        var result = _animationResource.RemoveTrack(track);

        // Assert
        result.Should().BeFalse();
        _animationResource.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void ClearTracks_WithExistingTracks_ShouldClearAllTracks()
    {
        // Arrange
        var keyframes = new List<AnimationKeyframe>
        {
            new(0.0f, 1.0f, InterpolationType.Linear)
        };
        var track1 = new AnimationTrack("Bone1", "Position.X", keyframes);
        var track2 = new AnimationTrack("Bone2", "Position.Y", keyframes);

        _animationResource.AddTrack(track1);
        _animationResource.AddTrack(track2);

        // Act
        _animationResource.ClearTracks();

        // Assert
        _animationResource.Tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        await FluentActions.Awaiting(() => _animationResource.LoadFromStreamAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithEmptyStream_ShouldHandleGracefully()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _animationResource.LoadFromStreamAsync(stream))
            .Should().NotThrowAsync();
    }

    [Fact]
    public void AsBytes_ShouldReturnValidByteArray()
    {
        // Arrange
        _animationResource.AnimationType = AnimationType.Clip;
        _animationResource.AnimationName = "TestAnimation";

        // Act
        var bytes = _animationResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Stream_ShouldReturnValidStream()
    {
        // Act
        var stream = _animationResource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream.Should().BeOfType<MemoryStream>();
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Act
        var fields = _animationResource.ContentFields;

        // Assert
        fields.Should().NotBeNull();
        fields.Should().Contain("AnimationType");
        fields.Should().Contain("AnimationName");
        fields.Should().Contain("Duration");
        fields.Should().Contain("FrameRate");
        fields.Should().Contain("PlayMode");
        fields.Should().Contain("BlendMode");
        fields.Should().Contain("IsLooped");
        fields.Should().Contain("Priority");
        fields.Should().Contain("Tracks");
    }

    [Fact]
    public void Indexer_WithValidFieldName_ShouldReturnCorrectValue()
    {
        // Arrange
        _animationResource.AnimationType = AnimationType.Clip;

        // Act
        var value = _animationResource["AnimationType"];

        // Assert
        value.Value.Should().Be(AnimationType.Clip);
    }

    [Fact]
    public void Indexer_WithInvalidFieldName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Action act = () => _ = _animationResource["InvalidField"];
        act.Should().Throw<ArgumentException>()
           .WithMessage("Unknown field: InvalidField*")
           .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void ResourceChanged_WhenTrackAdded_ShouldFireEvent()
    {
        // Arrange
        var eventFired = false;
        _animationResource.ResourceChanged += (_, _) => eventFired = true;

        var keyframes = new List<AnimationKeyframe>
        {
            new(0.0f, 1.0f, InterpolationType.Linear)
        };
        var track = new AnimationTrack("TestBone", "Position.X", keyframes);

        // Act
        _animationResource.AddTrack(track);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldDisposeResourceCorrectly()
    {
        // Arrange
        var resource = new AnimationResource();

        // Act
        resource.Dispose();

        // Assert - Should not throw when accessing disposed resource's Stream
        Action act = () => _ = resource.Stream;
        act.Should().Throw<ObjectDisposedException>();
    }
}
