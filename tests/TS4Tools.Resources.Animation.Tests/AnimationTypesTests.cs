using FluentAssertions;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class AnimationTypesTests
{
    [Fact]
    public void AnimationType_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<AnimationType>().Should().Contain(new[]
        {
            nameof(AnimationType.None),
            nameof(AnimationType.Clip),
            nameof(AnimationType.Pose),
            nameof(AnimationType.IkConfiguration),
            nameof(AnimationType.TrackMask),
            nameof(AnimationType.Rig),
            nameof(AnimationType.Bone),
            nameof(AnimationType.Outfit),
            nameof(AnimationType.CasPart)
        });
    }

    [Fact]
    public void CharacterType_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<CharacterType>().Should().Contain(new[]
        {
            nameof(CharacterType.None),
            nameof(CharacterType.CasPart),
            nameof(CharacterType.Outfit),
            nameof(CharacterType.Bone),
            nameof(CharacterType.SkinTone),
            nameof(CharacterType.Preset),
            nameof(CharacterType.SimModifier),
            nameof(CharacterType.DeformerMap),
            nameof(CharacterType.AnimalCoat)
        });
    }

    [Fact]
    public void AgeCategory_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<AgeCategory>().Should().Contain(new[]
        {
            nameof(AgeCategory.None),
            nameof(AgeCategory.Baby),
            nameof(AgeCategory.Toddler),
            nameof(AgeCategory.Child),
            nameof(AgeCategory.Teen),
            nameof(AgeCategory.YoungAdult),
            nameof(AgeCategory.Adult),
            nameof(AgeCategory.Elder),
            nameof(AgeCategory.All)
        });
    }

    [Fact]
    public void Gender_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<Gender>().Should().Contain(new[]
        {
            nameof(Gender.None),
            nameof(Gender.Male),
            nameof(Gender.Female),
            nameof(Gender.Unisex)
        });
    }

    [Fact]
    public void Species_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<Species>().Should().Contain(new[]
        {
            nameof(Species.None),
            nameof(Species.Human),
            nameof(Species.Cat),
            nameof(Species.Dog),
            nameof(Species.Horse),
            nameof(Species.All)
        });
    }

    [Fact]
    public void PartCategory_EnumValues_ShouldBeCorrect()
    {
        // Act & Assert
        Enum.GetNames<PartCategory>().Should().Contain(new[]
        {
            nameof(PartCategory.None),
            nameof(PartCategory.Hair),
            nameof(PartCategory.Head),
            nameof(PartCategory.Body),
            nameof(PartCategory.Top),
            nameof(PartCategory.Bottom),
            nameof(PartCategory.Shoes),
            nameof(PartCategory.Accessories),
            nameof(PartCategory.Makeup),
            nameof(PartCategory.Eyebrows),
            nameof(PartCategory.Eyes),
            nameof(PartCategory.Skin),
            nameof(PartCategory.Facial),
            nameof(PartCategory.Occult)
        });
    }

    [Theory]
    [InlineData(AnimationType.Clip, 1)]
    [InlineData(AnimationType.Pose, 2)]
    [InlineData(AnimationType.IkConfiguration, 3)]
    [InlineData(AnimationType.TrackMask, 4)]
    [InlineData(AnimationType.Rig, 5)]
    [InlineData(AnimationType.Bone, 6)]
    [InlineData(AnimationType.Outfit, 7)]
    [InlineData(AnimationType.CasPart, 8)]
    public void AnimationType_Values_ShouldHaveExpectedOrder(AnimationType type, int expectedValue)
    {
        // Act & Assert
        ((int)type).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(AgeCategory.Baby, 1)]
    [InlineData(AgeCategory.Toddler, 2)]
    [InlineData(AgeCategory.Child, 4)]
    [InlineData(AgeCategory.Teen, 8)]
    [InlineData(AgeCategory.YoungAdult, 16)]
    [InlineData(AgeCategory.Adult, 32)]
    [InlineData(AgeCategory.Elder, 64)]
    public void AgeCategory_Values_ShouldFollowLifeStageOrder(AgeCategory age, int expectedValue)
    {
        // Act & Assert - Age categories use bit flags, not sequential values
        ((int)age).Should().Be(expectedValue);
    }

    [Fact]
    public void Vector3_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const float x = 1.0f;
        const float y = 2.0f;
        const float z = 3.0f;

        // Act
        var vector = new Vector3(x, y, z);

        // Assert
        vector.X.Should().Be(x);
        vector.Y.Should().Be(y);
        vector.Z.Should().Be(z);
    }

    [Fact]
    public void Vector3_Zero_ShouldReturnZeroVector()
    {
        // Act
        var zero = Vector3.Zero;

        // Assert
        zero.X.Should().Be(0.0f);
        zero.Y.Should().Be(0.0f);
        zero.Z.Should().Be(0.0f);
    }

    [Fact]
    public void Vector4_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const float x = 1.0f;
        const float y = 2.0f;
        const float z = 3.0f;
        const float w = 4.0f;

        // Act
        var vector = new Vector4(x, y, z, w);

        // Assert
        vector.X.Should().Be(x);
        vector.Y.Should().Be(y);
        vector.Z.Should().Be(z);
        vector.W.Should().Be(w);
    }

    [Fact]
    public void Vector4_Zero_ShouldReturnZeroVector()
    {
        // Act
        var zero = Vector4.Zero;

        // Assert
        zero.X.Should().Be(0.0f);
        zero.Y.Should().Be(0.0f);
        zero.Z.Should().Be(0.0f);
        zero.W.Should().Be(0.0f);
    }

    [Fact]
    public void Quaternion_Identity_ShouldReturnIdentityQuaternion()
    {
        // Act
        var identity = Quaternion.Identity;

        // Assert
        identity.X.Should().Be(0.0f);
        identity.Y.Should().Be(0.0f);
        identity.Z.Should().Be(0.0f);
        identity.W.Should().Be(1.0f);
    }

    [Fact]
    public void Quaternion_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const float x = 0.1f;
        const float y = 0.2f;
        const float z = 0.3f;
        const float w = 0.9f;

        // Act
        var quaternion = new Quaternion(x, y, z, w);

        // Assert
        quaternion.X.Should().Be(x);
        quaternion.Y.Should().Be(y);
        quaternion.Z.Should().Be(z);
        quaternion.W.Should().Be(w);
    }

    [Fact]
    public void Keyframe_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const float time = 1.5f;
        var value = new Vector3(1, 2, 3);
        const InterpolationType interpolation = InterpolationType.Linear;

        // Act
        var keyframe = new Keyframe<Vector3>(time, value, interpolation);

        // Assert
        keyframe.Time.Should().Be(time);
        keyframe.Value.Should().Be(value);
        keyframe.Interpolation.Should().Be(interpolation);
    }

    [Fact]
    public void AnimationTrack_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const string boneName = "TestBone";
        const TrackType trackType = TrackType.Position;

        // Act
        var track = new AnimationTrack<Vector3>(boneName, trackType);

        // Assert
        track.BoneName.Should().Be(boneName);
        track.TrackType.Should().Be(trackType);
        track.Keyframes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AnimationTrack_AddKeyframe_ShouldAddToCollection()
    {
        // Arrange
        var track = new AnimationTrack<Vector3>("TestBone", TrackType.Position);
        var keyframe = new Keyframe<Vector3>(1.0f, Vector3.Zero, InterpolationType.Linear);

        // Act
        track.AddKeyframe(keyframe);

        // Assert
        track.Keyframes.Should().Contain(keyframe);
        track.Keyframes.Should().HaveCount(1);
    }

    [Fact]
    public void CharacterPart_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const uint instanceId = 12345;
        const PartCategory category = PartCategory.Hair;
        const string name = "TestHair";
        const AgeCategory age = AgeCategory.Adult;
        const Gender gender = Gender.Unisex;
        const Species species = Species.Human;
        const int sortPriority = 10;

        // Act
        var part = new CharacterPart(instanceId, category, name, age, gender, species, sortPriority);

        // Assert
        part.InstanceId.Should().Be(instanceId);
        part.Category.Should().Be(category);
        part.Name.Should().Be(name);
        part.AgeCategory.Should().Be(age);
        part.Gender.Should().Be(gender);
        part.Species.Should().Be(species);
        part.SortPriority.Should().Be(sortPriority);
    }

    [Fact]
    public void Bone_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const string name = "TestBone";
        const string parentName = "ParentBone";
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity.ToVector4();
        var scale = new Vector3(1, 1, 1);

        // Act
        var bone = new Bone(name, parentName, position, rotation, scale);

        // Assert
        bone.Name.Should().Be(name);
        bone.ParentName.Should().Be(parentName);
        bone.Position.Should().Be(position);
        bone.Rotation.Should().Be(rotation);
        bone.Scale.Should().Be(scale);
    }

    [Fact]
    public void BoneNode_AddChild_ShouldAddToChildren()
    {
        // Arrange
        var parent = new BoneNode("Parent", Vector3.Zero, Quaternion.Identity);
        var child = new BoneNode("Child", new Vector3(1, 0, 0), Quaternion.Identity);

        // Act
        parent.AddChild(child);

        // Assert
        parent.Children.Should().Contain(child);
        child.Parent.Should().Be(parent);
    }
}
