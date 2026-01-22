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

namespace NitroCache.Benchmarks;

/// <summary>
/// Benchmark comparing No-Cache vs Redis-only vs Hybrid-Cache (L1/L2)
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
    private readonly TestData _testData = new(1, "Benchmark Product", "High-performance test data");

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
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Baseline: No caching, direct data access
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<TestData> NoCache()
    {
        // Simulate database query (10ms latency)
        await Task.Delay(10);
        return _testData;
    }

    /// <summary>
    /// Redis-only caching (L2 only)
    /// </summary>
    [Benchmark]
    public async Task<TestData?> RedisOnlyCache()
    {
        if (_redisCache == null) throw new InvalidOperationException("Redis cache not initialized");

        var cachedData = await _redisCache.GetStringAsync(TestKey);
        if (cachedData != null)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TestData>(cachedData);
        }

        // Simulate database query
        await Task.Delay(10);
        
        var jsonData = System.Text.Json.JsonSerializer.Serialize(_testData);
        await _redisCache.SetStringAsync(TestKey, jsonData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return _testData;
    }

    /// <summary>
    /// Hybrid cache (L1 in-memory + L2 Redis)
    /// </summary>
    [Benchmark]
    public async Task<TestData?> HybridCache()
    {
        if (_hybridCacheService == null) throw new InvalidOperationException("Hybrid cache not initialized");

        return await _hybridCacheService.GetOrSetAsync(
            TestKey,
            async ct =>
            {
                // Simulate database query
                await Task.Delay(10, ct);
                return _testData;
            },
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Hybrid cache with cache hit (best case - L1 hit)
    /// </summary>
    [Benchmark]
    public async Task<TestData?> HybridCacheWarmL1()
    {
        if (_hybridCacheService == null) throw new InvalidOperationException("Hybrid cache not initialized");

        // Pre-warm the cache
        await _hybridCacheService.GetOrSetAsync(
            TestKey + ":warm",
            async ct =>
            {
                await Task.Delay(10, ct);
                return _testData;
            },
            TimeSpan.FromMinutes(5));

        // Now measure the cache hit
        return await _hybridCacheService.GetOrSetAsync(
            TestKey + ":warm",
            async ct =>
            {
                await Task.Delay(10, ct);
                return _testData;
            },
            TimeSpan.FromMinutes(5));
    }

    public record TestData(int Id, string Name, string Description);
}
