using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace NitroCache.Benchmarks;

/// <summary>
/// Benchmark comparing Mock Database (100ms) vs Redis-only vs In-Memory cache
/// Tests with 10KB payload to simulate real-world scenarios
/// Note: HybridCache requires ASP.NET Core host for keyed services support
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CachingBenchmarks
{
    private ServiceProvider? _serviceProvider;
    private IMemoryCache? _memoryCache;
    private IDistributedCache? _redisCache;
    private const string TestKey = "benchmark:test:1";
    private TestData? _testData;
    private string? _serializedData;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Configure Memory cache (L1)
        services.AddMemoryCache();

        // Configure Redis-only cache for comparison
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "Benchmark:";
        });

        _serviceProvider = services.BuildServiceProvider();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
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
    /// In-memory cache (L1 only) with 10KB payload - fastest scenario
    /// </summary>
    [Benchmark]
    public async Task<TestData?> InMemoryCache_10KB()
    {
        if (_memoryCache == null) throw new InvalidOperationException("Memory cache not initialized");

        // Try to get from memory cache
        if (_memoryCache.TryGetValue(TestKey, out TestData? cachedValue))
        {
            return cachedValue;
        }

        // Simulate database query
        await Task.Delay(100);
        
        // Cache the result
        _memoryCache.Set(TestKey, _testData, TimeSpan.FromMinutes(5));

        return _testData;
    }

    public record TestData(
        int Id, 
        string Name, 
        string Description, 
        string Category, 
        decimal Price, 
        List<KeyValuePair<string, string>> Metadata);
}
