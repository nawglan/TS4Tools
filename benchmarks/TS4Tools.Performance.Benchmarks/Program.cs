using BenchmarkDotNet.Running;

namespace TS4Tools.Performance.Benchmarks;

/// <summary>
/// Performance benchmark runner for TS4Tools.
/// This establishes baseline performance measurements for Phase 4.13.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
