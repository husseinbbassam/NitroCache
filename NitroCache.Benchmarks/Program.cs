using BenchmarkDotNet.Running;

namespace NitroCache.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("   NitroCache Performance Benchmarks");
        Console.WriteLine("==============================================");
        Console.WriteLine();
        Console.WriteLine("This benchmark compares:");
        Console.WriteLine("  1. No Cache (Baseline) - Direct database access");
        Console.WriteLine("  2. Redis Only - L2 distributed cache only");
        Console.WriteLine("  3. Hybrid Cache - L1 (in-memory) + L2 (Redis)");
        Console.WriteLine();
        Console.WriteLine("Prerequisites:");
        Console.WriteLine("  - Redis server must be running on localhost:6379");
        Console.WriteLine("  - Run: docker run -d -p 6379:6379 redis:latest");
        Console.WriteLine();
        Console.WriteLine("==============================================");
        Console.WriteLine();

        var summary = BenchmarkRunner.Run<CachingBenchmarks>();
        
        Console.WriteLine();
        Console.WriteLine("Benchmark completed! Check the results above.");
    }
}
