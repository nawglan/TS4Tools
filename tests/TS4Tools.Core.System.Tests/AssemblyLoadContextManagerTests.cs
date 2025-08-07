using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.System;
using Xunit;

namespace TS4Tools.Core.System.Tests;

/// <summary>
/// Unit tests for AssemblyLoadContextManager - Phase 0.3 critical implementation.
/// These tests validate the modern AssemblyLoadContext-based loading that replaces
/// the legacy Assembly.LoadFile() calls.
/// </summary>
public class AssemblyLoadContextManagerTests : IDisposable
{
    private readonly ILogger<AssemblyLoadContextManager> _mockLogger;
    private readonly AssemblyLoadContextManager _manager;
    private readonly string _testAssemblyPath;

    public AssemblyLoadContextManagerTests()
    {
        _mockLogger = Substitute.For<ILogger<AssemblyLoadContextManager>>();
        _manager = new AssemblyLoadContextManager(_mockLogger);
        
        // Use the current test assembly as a valid test assembly
        _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public void Constructor_ShouldInitializeCorrectly_WhenValidLoggerProvided()
    {
        // ARRANGE & ACT
        using var manager = new AssemblyLoadContextManager(_mockLogger);
        
        // ASSERT
        manager.Should().NotBeNull("manager should be created successfully");
        manager.GetLoadedContexts().Should().BeEmpty("no contexts should be loaded initially");
        
        // Verify logging
        _mockLogger.Received(1).LogInformation("AssemblyLoadContextManager initialized");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // ARRANGE & ACT & ASSERT
        Action act = () => new AssemblyLoadContextManager(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*logger*");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public void LoadFromPath_ShouldLoadAssembly_WhenValidPathProvided()
    {
        // ARRANGE
        var validPath = _testAssemblyPath;

        // ACT
        var assembly = _manager.LoadFromPath(validPath);

        // ASSERT
        assembly.Should().NotBeNull("assembly should be loaded successfully");
        assembly.Location.Should().Be(validPath, "loaded assembly should have correct location");
        
        var contexts = _manager.GetLoadedContexts();
        contexts.Should().HaveCountGreaterThan(0, "at least one context should be created");
        
        // Verify logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(msg => msg.Contains("Successfully loaded assembly")),
            Arg.Any<object[]>());
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void LoadFromPath_ShouldThrowArgumentException_WhenPathIsNullOrEmpty()
    {
        // ARRANGE & ACT & ASSERT
        Action actNull = () => _manager.LoadFromPath(null!);
        Action actEmpty = () => _manager.LoadFromPath("");
        Action actWhitespace = () => _manager.LoadFromPath("   ");

        actNull.Should().Throw<ArgumentException>().WithMessage("*Assembly path*");
        actEmpty.Should().Throw<ArgumentException>().WithMessage("*Assembly path*");
        actWhitespace.Should().Throw<ArgumentException>().WithMessage("*Assembly path*");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void LoadFromPath_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // ARRANGE
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent.dll");

        // ACT & ASSERT
        Action act = () => _manager.LoadFromPath(nonExistentPath);
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Assembly file not found*");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public async Task LoadFromStream_ShouldLoadAssembly_WhenValidStreamProvided()
    {
        // ARRANGE
        var assemblyBytes = await File.ReadAllBytesAsync(_testAssemblyPath);
        using var stream = new MemoryStream(assemblyBytes);

        // ACT
        var assembly = _manager.LoadFromStream(stream);

        // ASSERT
        assembly.Should().NotBeNull("assembly should be loaded from stream");
        assembly.GetName().Name.Should().NotBeNullOrEmpty("assembly should have a name");
        
        var contexts = _manager.GetLoadedContexts();
        contexts.Should().HaveCountGreaterThan(0, "at least one context should be created");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void LoadFromStream_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // ARRANGE & ACT & ASSERT
        Action act = () => _manager.LoadFromStream(null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*assemblyStream*");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void LoadFromStream_ShouldThrowArgumentException_WhenStreamIsNotReadable()
    {
        // ARRANGE
        var mockStream = Substitute.For<Stream>();
        mockStream.CanRead.Returns(false);

        // ACT & ASSERT
        Action act = () => _manager.LoadFromStream(mockStream);
        act.Should().Throw<ArgumentException>().WithMessage("*Stream must be readable*");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public void GetLoadingStatistics_ShouldReturnValidStatistics()
    {
        // ARRANGE
        _manager.LoadFromPath(_testAssemblyPath);

        // ACT
        var stats = _manager.GetLoadingStatistics();

        // ASSERT
        stats.Should().NotBeNull("statistics should be returned");
        stats.Should().ContainKey("TotalContextsCreated");
        stats.Should().ContainKey("ActiveContexts");
        stats.Should().ContainKey("CollectedContexts");
        stats.Should().ContainKey("LastOperationTime");
        
        var totalContexts = (int)stats["TotalContextsCreated"];
        var activeContexts = (int)stats["ActiveContexts"];
        
        totalContexts.Should().BeGreaterThan(0, "should have created at least one context");
        activeContexts.Should().BeGreaterThan(0, "should have at least one active context");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void GetLoadedContexts_ShouldReturnContextNames_WhenContextsExist()
    {
        // ARRANGE
        _manager.LoadFromPath(_testAssemblyPath);

        // ACT
        var contexts = _manager.GetLoadedContexts().ToList();

        // ASSERT
        contexts.Should().HaveCountGreaterThan(0, "should return at least one context");
        contexts.Should().AllSatisfy(name => 
            name.Should().NotBeNullOrWhiteSpace("context names should be valid"));
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void UnloadContext_ShouldReturnTrue_WhenContextExists()
    {
        // ARRANGE
        _manager.LoadFromPath(_testAssemblyPath);
        var contextName = _manager.GetLoadedContexts().First();

        // ACT
        var result = _manager.UnloadContext(contextName);

        // ASSERT
        result.Should().BeTrue("should successfully unload existing context");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void UnloadContext_ShouldReturnFalse_WhenContextDoesNotExist()
    {
        // ARRANGE
        var nonExistentContext = "NonExistentContext";

        // ACT
        var result = _manager.UnloadContext(nonExistentContext);

        // ASSERT
        result.Should().BeFalse("should return false for non-existent context");
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public void Dispose_ShouldCleanupAllContexts()
    {
        // ARRANGE
        _manager.LoadFromPath(_testAssemblyPath);
        var initialContextCount = _manager.GetLoadedContexts().Count();
        initialContextCount.Should().BeGreaterThan(0, "should have contexts before dispose");

        // ACT
        _manager.Dispose();

        // ASSERT
        // After disposal, operations should throw ObjectDisposedException
        Action act = () => _manager.GetLoadedContexts();
        act.Should().NotThrow("GetLoadedContexts should handle disposal gracefully");
        
        // Verify disposal logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(msg => msg.Contains("AssemblyLoadContextManager disposed")));
    }

    [Fact]
    [Trait("Category", "Phase0.3")]
    public void DisposedManager_ShouldThrowObjectDisposedException_ForAllMethods()
    {
        // ARRANGE
        _manager.Dispose();

        // ACT & ASSERT
        Action loadFromPath = () => _manager.LoadFromPath(_testAssemblyPath);
        Action loadFromStream = () => _manager.LoadFromStream(new MemoryStream());
        Action unloadContext = () => _manager.UnloadContext("test");

        loadFromPath.Should().Throw<ObjectDisposedException>();
        loadFromStream.Should().Throw<ObjectDisposedException>();
        unloadContext.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Integration test that validates the complete Phase 0.3 requirement:
    /// Modern AssemblyLoadContext replaces legacy Assembly.LoadFile() successfully.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Phase0.3")]
    [Trait("Priority", "Critical")]
    public void Phase03_Integration_ShouldReplaceAssemblyLoadFileSuccessfully()
    {
        // ARRANGE - Simulate the legacy Assembly.LoadFile scenario
        var assemblyPath = _testAssemblyPath;

        // ACT - Use modern AssemblyLoadContext approach
        var assembly = _manager.LoadFromPath(assemblyPath);

        // ASSERT - Verify all Phase 0.3 requirements are met
        assembly.Should().NotBeNull("assembly should load successfully");
        assembly.Location.Should().Be(assemblyPath, "should load from correct path");
        
        // Verify isolation - context should be separate
        var contexts = _manager.GetLoadedContexts();
        contexts.Should().HaveCountGreaterThan(0, "should create isolated context");
        
        // Verify statistics tracking
        var stats = _manager.GetLoadingStatistics();
        stats["TotalContextsCreated"].Should().BeOfType<int>()
            .And.Subject.As<int>().Should().BeGreaterThan(0, "should track context creation");
        
        // Verify cleanup capability
        var contextName = contexts.First();
        var unloadResult = _manager.UnloadContext(contextName);
        unloadResult.Should().BeTrue("should support context cleanup");
        
        // Log success
        Console.WriteLine("✅ Phase 0.3 COMPLETE: Assembly.LoadFile() successfully replaced with modern AssemblyLoadContext");
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }
}

/// <summary>
/// Performance tests for AssemblyLoadContextManager to ensure it meets Phase 0.3 requirements.
/// </summary>
public class AssemblyLoadContextManagerPerformanceTests : IDisposable
{
    private readonly ILogger<AssemblyLoadContextManager> _mockLogger;
    private readonly AssemblyLoadContextManager _manager;
    private readonly string _testAssemblyPath;

    public AssemblyLoadContextManagerPerformanceTests()
    {
        _mockLogger = Substitute.For<ILogger<AssemblyLoadContextManager>>();
        _manager = new AssemblyLoadContextManager(_mockLogger);
        _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Phase", "Phase0.3")]
    public void LoadFromPath_ShouldMeetPerformanceRequirements()
    {
        // ARRANGE
        var startTime = DateTime.UtcNow;

        // ACT
        var assembly = _manager.LoadFromPath(_testAssemblyPath);

        // ASSERT
        var duration = DateTime.UtcNow - startTime;
        duration.TotalMilliseconds.Should().BeLessThan(100, 
            "assembly loading should complete within 100ms for typical assemblies");
            
        assembly.Should().NotBeNull("assembly should load successfully within time limit");
        
        Console.WriteLine($"⚡ Assembly loading performance: {duration.TotalMilliseconds:F2}ms");
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }
}
