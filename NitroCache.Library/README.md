# NitroCache.Library

A high-performance hybrid caching library for .NET 9+ with L1 (in-memory) and L2 (Redis) support.

## Features

- **Hybrid Caching**: Combines in-memory (L1) and distributed Redis (L2) caching for optimal performance
- **Tag-based Invalidation**: Invalidate multiple cache entries at once using tags
- **Cache Stampede Protection**: Built-in protection prevents thundering herd problems
- **Source Generators**: Uses System.Text.Json source generators for zero-reflection serialization
- **Resilient Architecture**: Circuit breaker pattern automatically downgrades to L1-only mode when Redis fails
- **Distributed Locking**: RedLock.net implementation for multi-instance coordination
- **Thread-Safe**: All operations are thread-safe and work correctly in concurrent scenarios

## Installation

```bash
dotnet add package NitroCache.Library
```

## Quick Start

### 1. Register NitroCache

```csharp
builder.Services.AddNitroCache(options =>
{
    options.RedisConnectionString = "localhost:6379";
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.LocalCacheExpiration = TimeSpan.FromMinutes(1);
    options.MaximumLocalCacheSizeMB = 512;
});
```

### 2. Use the Cache Service

```csharp
public class ProductService
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _repository;

    public ProductService(ICacheService cacheService, IProductRepository repository)
    {
        _cacheService = cacheService;
        _repository = repository;
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken ct = default)
    {
        return await _cacheService.GetOrSetAsync(
            $"product:{id}",
            async token => await _repository.GetByIdAsync(id, token),
            TimeSpan.FromMinutes(10),
            ct);
    }
}
```

### 3. Tag-based Invalidation

```csharp
// Cache with tags
await _cacheService.GetOrSetWithTagsAsync(
    $"product:{id}",
    async ct => await GetProductFromDb(id, ct),
    new[] { "All_Products", $"Product_{id}", $"Category_{category}" },
    TimeSpan.FromMinutes(5));

// Invalidate all products in a category
await _cacheService.RemoveByTagAsync($"Category_{category}");

// Invalidate multiple tags at once
await _cacheService.RemoveByTagsAsync(new[] { "Tag1", "Tag2", "Tag3" });
```

### 4. Distributed Locking

```csharp
public class OrderService
{
    private readonly IDistributedLock _distributedLock;

    public async Task<bool> ProcessOrderAsync(int orderId)
    {
        return await _distributedLock.ExecuteWithLockAsync(
            $"order-lock:{orderId}",
            TimeSpan.FromSeconds(30),
            async () =>
            {
                // Process order - only one instance will execute this
                await ProcessOrder(orderId);
            });
    }
}
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `RedisConnectionString` | Redis connection string | `localhost:6379` |
| `DefaultExpiration` | Default cache expiration for L2 | 5 minutes |
| `LocalCacheExpiration` | L1 (in-memory) cache expiration | 1 minute |
| `MaximumLocalCacheSizeMB` | Maximum L1 cache size in MB | 512 MB |

## Architecture

### Hybrid Cache (L1/L2)

```
┌─────────────┐
│   Request   │
└──────┬──────┘
       │
       v
┌─────────────┐
│  L1 Cache   │  ← In-Memory (Fastest, per-instance)
│ (RAM/Local) │
└──────┬──────┘
       │ Miss
       v
┌─────────────┐
│  L2 Cache   │  ← Redis (Fast, distributed)
│   (Redis)   │
└──────┬──────┘
       │ Miss
       v
┌─────────────┐
│  Database   │  ← Slow, but authoritative
└─────────────┘
```

### Resilience with Circuit Breaker

If Redis fails, NitroCache automatically:
1. Opens the circuit breaker
2. Logs a warning
3. Downgrades to L1-only mode
4. Continues serving cached data from memory
5. Periodically retries Redis connection
6. Automatically resumes L2 caching when Redis recovers

## Performance

Typical latencies (with warm cache):
- **L1 (RAM) Hit**: < 0.1ms (sub-millisecond)
- **L2 (Redis) Hit**: 1-3ms
- **Database Miss**: 10-100ms+

With a 10KB payload:
- **Mock Database (100ms)**: Baseline
- **Redis-only**: ~2-5ms
- **HybridCache (RAM)**: < 0.5ms (200x faster than database!)

## Thread Safety

All operations are thread-safe:
- Tag tracking uses `ConcurrentDictionary` and `ConcurrentHashSet`
- Cache stampede protection ensures only one factory execution per key
- Distributed locks coordinate across multiple instances

## Requirements

- .NET 9.0 or later
- Redis 5.0 or later (optional - works in L1-only mode without Redis)

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
