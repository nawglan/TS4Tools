using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Animation;

namespace AnimationExample;

/// <summary>
/// Practical example demonstrating the Animation Resource System in action.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎬 TS4Tools Animation System Demo");
        Console.WriteLine("==================================\n");

        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddAnimationResources();

        using var provider = services.BuildServiceProvider();

        // Get the animation factories
        var animationFactory = provider.GetRequiredService<AnimationResourceFactory>();
        var rigFactory = provider.GetRequiredService<RigResourceFactory>();
        var characterFactory = provider.GetRequiredService<CharacterResourceFactory>();

        Console.WriteLine("✅ Animation system initialized successfully!");
        Console.WriteLine($"Animation Factory: {animationFactory.GetType().Name}");
        Console.WriteLine($"Rig Factory: {rigFactory.GetType().Name}");
        Console.WriteLine($"Character Factory: {characterFactory.GetType().Name}\n");

        // Create a simple character rig
        await DemonstrateRigCreation();
        
        // Create animation tracks with keyframes
        await DemonstrateAnimationTracks();
        
        // Show bone hierarchy operations
        await DemonstrateBoneHierarchy();
        
        // Test factory resource creation
        await DemonstrateResourceCreation(animationFactory, rigFactory, characterFactory);

        Console.WriteLine("\n🎉 Animation system demo completed successfully!");
    }

    static Task DemonstrateRigCreation()
    {
        Console.WriteLine("🦴 Creating Character Rig");
        Console.WriteLine("-------------------------");

        // Create bones for a simple character rig
        var bones = new List<Bone>
        {
            new("Root", null, Vector3.Zero, Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1)),
            new("Spine", "Root", new Vector3(0, 1, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1)),
            new("Chest", "Spine", new Vector3(0, 0.5f, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1)),
            new("Head", "Chest", new Vector3(0, 0.3f, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1)),
            new("LeftArm", "Chest", new Vector3(-0.2f, 0.1f, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1)),
            new("RightArm", "Chest", new Vector3(0.2f, 0.1f, 0), Quaternion.Identity.ToVector4(), new Vector3(1, 1, 1))
        };

        Console.WriteLine($"Created rig with {bones.Count} bones:");
        foreach (var bone in bones)
        {
            var parentInfo = bone.ParentName != null ? $" (parent: {bone.ParentName})" : " (root)";
            Console.WriteLine($"  • {bone.Name}{parentInfo}");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    static Task DemonstrateAnimationTracks()
    {
        Console.WriteLine("🎭 Creating Animation Tracks");
        Console.WriteLine("----------------------------");

        // Create position track for head bobbing animation
        var headPositionTrack = new AnimationTrack<Vector3>("Head", TrackType.Position);
        
        // Add keyframes for a simple head bob animation
        headPositionTrack.AddKeyframe(new Keyframe<Vector3>(0.0f, new Vector3(0, 0.3f, 0), InterpolationType.Linear));
        headPositionTrack.AddKeyframe(new Keyframe<Vector3>(0.5f, new Vector3(0, 0.35f, 0), InterpolationType.Linear));
        headPositionTrack.AddKeyframe(new Keyframe<Vector3>(1.0f, new Vector3(0, 0.3f, 0), InterpolationType.Linear));

        Console.WriteLine($"Head Position Track: {headPositionTrack.Keyframes.Count} keyframes");
        foreach (var keyframe in headPositionTrack.Keyframes)
        {
            Console.WriteLine($"  • Time: {keyframe.Time:F1}s, Position: ({keyframe.Value.X:F2}, {keyframe.Value.Y:F2}, {keyframe.Value.Z:F2})");
        }

        // Create rotation track for arm swinging
        var leftArmRotationTrack = new AnimationTrack<Quaternion>("LeftArm", TrackType.Rotation);
        
        // Add keyframes for arm swing
        var rotationKeyframes = new[]
        {
            new Keyframe<Quaternion>(0.0f, Quaternion.Identity, InterpolationType.Cubic),
            new Keyframe<Quaternion>(0.25f, new Quaternion(0, 0, 0.1f, 0.995f), InterpolationType.Cubic),
            new Keyframe<Quaternion>(0.75f, new Quaternion(0, 0, -0.1f, 0.995f), InterpolationType.Cubic),
            new Keyframe<Quaternion>(1.0f, Quaternion.Identity, InterpolationType.Cubic)
        };

        foreach (var keyframe in rotationKeyframes)
        {
            leftArmRotationTrack.AddKeyframe(keyframe);
        }

        Console.WriteLine($"\nLeft Arm Rotation Track: {leftArmRotationTrack.Keyframes.Count} keyframes");
        foreach (var keyframe in leftArmRotationTrack.Keyframes)
        {
            Console.WriteLine($"  • Time: {keyframe.Time:F2}s, Rotation: ({keyframe.Value.X:F2}, {keyframe.Value.Y:F2}, {keyframe.Value.Z:F2}, {keyframe.Value.W:F2})");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    static Task DemonstrateBoneHierarchy()
    {
        Console.WriteLine("🌳 Building Bone Hierarchy");
        Console.WriteLine("--------------------------");

        // Create bone nodes for hierarchical operations
        var root = new BoneNode("Root", Vector3.Zero, Quaternion.Identity);
        var spine = new BoneNode("Spine", new Vector3(0, 1, 0), Quaternion.Identity, root);
        var chest = new BoneNode("Chest", new Vector3(0, 0.5f, 0), Quaternion.Identity, spine);
        var head = new BoneNode("Head", new Vector3(0, 0.3f, 0), Quaternion.Identity, chest);
        var leftArm = new BoneNode("LeftArm", new Vector3(-0.2f, 0.1f, 0), Quaternion.Identity, chest);
        var rightArm = new BoneNode("RightArm", new Vector3(0.2f, 0.1f, 0), Quaternion.Identity, chest);

        // Display the hierarchy
        Console.WriteLine("Bone hierarchy:");
        PrintBoneHierarchy(root, 0);

        Console.WriteLine($"\nChest has {chest.Children.Count} child bones:");
        foreach (var child in chest.Children)
        {
            Console.WriteLine($"  • {child.Name}");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    static void PrintBoneHierarchy(BoneNode bone, int depth)
    {
        var indent = new string(' ', depth * 2);
        Console.WriteLine($"{indent}├─ {bone.Name}");
        
        foreach (var child in bone.Children)
        {
            PrintBoneHierarchy(child, depth + 1);
        }
    }

    static async Task DemonstrateResourceCreation(
        AnimationResourceFactory animationFactory, 
        RigResourceFactory rigFactory, 
        CharacterResourceFactory characterFactory)
    {
        Console.WriteLine("🏭 Testing Resource Factories");
        Console.WriteLine("-----------------------------");

        try
        {
            // Test animation resource creation
            var animationResource = await animationFactory.CreateResourceAsync(1);
            Console.WriteLine($"✅ Created AnimationResource: {animationResource.GetType().Name}");
            Console.WriteLine($"   Animation Type: {animationResource.AnimationType}");
            Console.WriteLine($"   Duration: {animationResource.Duration:F2} seconds");
            
            // Test rig resource creation
            var rigResource = await rigFactory.CreateResourceAsync(1);
            Console.WriteLine($"✅ Created RigResource: {rigResource.GetType().Name}");
            Console.WriteLine($"   Bone Count: {rigResource.BoneCount}");
            Console.WriteLine($"   Rig Name: {rigResource.RigName}");
            
            // Test character resource creation
            var characterResource = await characterFactory.CreateResourceAsync(1);
            Console.WriteLine($"✅ Created CharacterResource: {characterResource.GetType().Name}");
            Console.WriteLine($"   Age Category: {characterResource.AgeCategory}");
            Console.WriteLine($"   Species: {characterResource.Species}");

            // Show supported resource types
            Console.WriteLine($"\nSupported resource types:");
            Console.WriteLine($"  Animation Factory: {string.Join(", ", animationFactory.SupportedResourceTypes)}");
            Console.WriteLine($"  Rig Factory: {string.Join(", ", rigFactory.SupportedResourceTypes)}");
            Console.WriteLine($"  Character Factory: {string.Join(", ", characterFactory.SupportedResourceTypes)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during resource creation: {ex.Message}");
        }
    }
}
