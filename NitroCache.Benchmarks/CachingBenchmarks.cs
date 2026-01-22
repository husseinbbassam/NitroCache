using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NitroCache.Library;
using StackExchange.Redis;
using System.Text;

namespace NitroCache.Benchmarks;

/// <summary>
/// Benchmark comparing Mock Database (100ms) vs Redis-only vs Hybrid-Cache (L1/L2)
/// Tests with 10KB payload to simulate real-world scenarios
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CachingBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private ICacheService? _hybridCacheService;
    private IDistributedCache? _redisCache;
    private const string TestKey = "benchmark:test:1";
    private TestData? _testData;
    private string? _serializedData;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        // Configure NitroCache (Hybrid)
        services.AddNitroCache(options =>
        {
            options.RedisConnectionString = "localhost:6379";
            options.DefaultExpiration = TimeSpan.FromMinutes(5);
            options.LocalCacheExpiration = TimeSpan.FromMinutes(1);
        });

        // Configure Redis-only cache for comparison
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "Benchmark:";
        });

        _serviceProvider = services.BuildServiceProvider();
        _hybridCacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _redisCache = _serviceProvider.GetRequiredService<IDistributedCache>();

        // Create a ~10KB test data object
        _testData = CreateLargeTestData();
        _serializedData = System.Text.Json.JsonSerializer.Serialize(_testData);
    }

    /// <summary>
    /// Creates a test data object that is approximately 10KB in size
    /// </summary>
    private TestData CreateLargeTestData()
    {
        // Create a large string to reach ~10KB
        var largeDescription = new StringBuilder();
        var baseText = "This is a sample product description with many details about the product features, specifications, and benefits. ";
        
        // Repeat the text until we reach approximately 10KB
        while (largeDescription.Length < 10000)
        {
            largeDescription.Append(baseText);
        }

        return new TestData(
            Id: 1,
            Name: "High-Performance Benchmark Product",
            Description: largeDescription.ToString(),
            Category: "Electronics",
            Price: 999.99m,
            Metadata: Enumerable.Range(1, 100).Select(i => new KeyValuePair<string, string>($"key{i}", $"value{i}")).ToList()
        );
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Baseline: Mock database with 100ms simulated latency
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<TestData> MockDatabase_100ms()
    {
        // Simulate database query with 100ms latency
        await Task.Delay(100);
        return _testData!;
    }

    /// <summary>
    /// Redis-only caching (L2 only) with 10KB payload
    /// </summary>
    [Benchmark]
    public async Task<TestData?> RedisOnly_10KB()
    {
        if (_redisCache == null) throw new InvalidOperationException("Redis cache not initialized");

        var cachedData = await _redisCache.GetStringAsync(TestKey);
        if (cachedData != null)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TestData>(cachedData);
        }

        // Simulate database query
        await Task.Delay(100);
        
        await _redisCache.SetStringAsync(TestKey, _serializedData!, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return _testData;
    }

    /// <summary>
    /// Hybrid cache (L1 in-memory + L2 Redis) with 10KB payload - RAM hit scenario
    /// </summary>
    [Benchmark]
    public async Task<TestData?> HybridCache_RAMHit_10KB()
    {
        if (_hybridCacheService == null) throw new InvalidOperationException("Hybrid cache not initialized");

        // Pre-warm the cache to ensure L1 (RAM) hit
        await _hybridCacheService.GetOrSetAsync(
            TestKey + ":warm",
            async ct =>
            {
                await Task.Delay(100, ct);
                return _testData;
            },
            TimeSpan.FromMinutes(5));

        // Now measure the L1 (RAM) cache hit - this should be fastest
        return await _hybridCacheService.GetOrSetAsync(
            TestKey + ":warm",
            async ct =>
            {
                await Task.Delay(100, ct);
                return _testData;
            },
            TimeSpan.FromMinutes(5));
    }

    public record TestData(
        int Id, 
        string Name, 
        string Description, 
        string Category, 
        decimal Price, 
        List<KeyValuePair<string, string>> Metadata);
}
