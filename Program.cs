using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Animation;

namespace DITest;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnimationResources();

        using var provider = services.BuildServiceProvider();

        // Test that concrete types can be resolved
        var animationFactory = provider.GetService<AnimationResourceFactory>();
        var characterFactory = provider.GetService<CharacterResourceFactory>();
        var rigFactory = provider.GetService<RigResourceFactory>();

        Console.WriteLine($"AnimationResourceFactory: {(animationFactory != null ? "✓" : "✗")}");
        Console.WriteLine($"CharacterResourceFactory: {(characterFactory != null ? "✓" : "✗")}");
        Console.WriteLine($"RigResourceFactory: {(rigFactory != null ? "✓" : "✗")}");

        // Test that interface registrations work
        var factories = provider.GetServices<TS4Tools.Core.Resources.IResourceFactory>();
        Console.WriteLine($"IResourceFactory count: {factories.Count()}");

        Console.WriteLine("DI registration test completed successfully!");
    }
}
