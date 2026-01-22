# NitroCache

A high-performance caching solution built with .NET 9 and C# 13, demonstrating **Hybrid Caching (L1/L2)** and **Performance Optimization**.

## 🚀 Features

- **Hybrid Caching**: L1 (in-memory) + L2 (Redis) for ultra-fast data access
- **Tag-based Invalidation**: Invalidate multiple cache entries at once using tags (NEW!)
- **Cache Stampede Protection**: Built-in locking mechanism prevents thundering herd
- **Distributed Locking**: RedLock.net implementation for multi-instance synchronization
- **Source Generators**: System.Text.Json source generators for optimal serialization
- **Circuit Breaker Resilience**: Automatically downgrades to L1-only mode when Redis fails (NEW!)
- **Generic API**: Clean `ICacheService` interface for easy integration
- **Benchmarks**: BenchmarkDotNet comparison of No-Cache vs Redis-only vs Hybrid

## 📦 Project Structure

```
NitroCache/
├── NitroCache.Library/       # Core caching library
│   ├── ICacheService.cs      # Generic cache interface
│   ├── HybridCacheService.cs # Hybrid cache implementation
│   ├── IDistributedLock.cs   # Distributed lock interface
│   └── RedisDistributedLock.cs # RedLock implementation
├── NitroCache.Api/           # Demo Product Catalog API
│   ├── Models/               # Product models with JSON source generators
│   ├── Services/             # Product service with caching
│   └── Database/             # Mock database
└── NitroCache.Benchmarks/    # Performance benchmarks
    └── CachingBenchmarks.cs  # BenchmarkDotNet tests
```

## 🛠️ Prerequisites

- **.NET 9 SDK** or later
- **Docker** (for Redis)
- **Redis Server** running on `localhost:6379`

## 🏃 Quick Start

### 1. Start Redis

```bash
docker run -d -p 6379:6379 redis:latest
```

### 2. Run the API

```bash
cd NitroCache.Api
dotnet run
```

The API will be available at `http://localhost:5040` (or `https://localhost:7191`)

### 3. Test the Endpoints

```bash
# Get all products (with caching)
curl http://localhost:5040/api/products

# Get a specific product
curl http://localhost:5040/api/products/1

# Get products by category
curl http://localhost:5040/api/products/category/Electronics

# Invalidate cache for a product
curl -X DELETE http://localhost:5040/api/products/1/cache

# Invalidate all products in a category (tag-based invalidation)
curl -X DELETE http://localhost:5040/api/products/category/Electronics/cache

# Invalidate all product caches
curl -X DELETE http://localhost:5040/api/products/cache

# Health check
curl http://localhost:5040/health
```

### 4. Run Benchmarks

```bash
cd NitroCache.Benchmarks
dotnet run -c Release
```

## 📊 Performance Results

The benchmarks compare three scenarios with a **10KB payload**:

1. **Mock Database (100ms latency)**: Simulates real database query
2. **Redis Only**: L2 distributed cache only
3. **Hybrid Cache (RAM hit)**: Best case with L1 cache hit

Expected results:
- **Hybrid Cache (RAM hit)**: Sub-millisecond access (~0.1-0.5ms) - **200x faster than database!**
- **Redis Only**: ~2-5ms
- **Mock Database**: ~100ms (baseline)

### Benchmark Results

Run benchmarks with:
```bash
cd NitroCache.Benchmarks
dotnet run -c Release
```

Performance characteristics:
- **L1 (RAM) Hit**: < 0.5ms - Fastest possible access
- **L2 (Redis) Hit**: 1-3ms - Fast distributed access
- **Database Miss**: 100ms+ - Slow but authoritative

## 🔧 Configuration

Configure Redis in `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

Or programmatically:

```csharp
services.AddNitroCache(options =>
{
    options.RedisConnectionString = "localhost:6379";
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.LocalCacheExpiration = TimeSpan.FromMinutes(1);
    options.MaximumLocalCacheSizeMB = 512;
});
```

## 💡 Usage Examples

### Basic Caching

```csharp
public class MyService
{
    private readonly ICacheService _cacheService;

    public async Task<MyData> GetDataAsync(int id)
    {
        return await _cacheService.GetOrSetAsync(
            $"mydata:{id}",
            async ct => await _database.GetDataAsync(id, ct),
            TimeSpan.FromMinutes(5));
    }
}
```

### Tag-based Invalidation (NEW!)

```csharp
public class ProductService
{
    private readonly ICacheService _cacheService;

    public async Task<Product?> GetProductAsync(int id)
    {
        // Cache with tags for group invalidation
        return await _cacheService.GetOrSetWithTagsAsync(
            $"product:{id}",
            async ct => await _database.GetProductAsync(id, ct),
            new[] { "All_Products", $"Product_{id}", $"Category_{categoryId}" },
            TimeSpan.FromMinutes(5));
    }

    public async Task InvalidateCategoryAsync(string category)
    {
        // Invalidate all products in this category at once!
        await _cacheService.RemoveByTagAsync($"Category_{category}");
    }
}
```

### Distributed Locking

```csharp
public class MyService
{
    private readonly IDistributedLock _distributedLock;

    public async Task<bool> PerformCriticalOperation()
    {
        return await _distributedLock.ExecuteWithLockAsync(
            "my-resource-lock",
            TimeSpan.FromSeconds(30),
            async () => {
                // Critical operation here
                await DoSomethingImportant();
            });
    }
}
```

## 🏗️ Technical Highlights

### 1. Hybrid Cache (L1/L2)

Uses Microsoft's `HybridCache` library (new in .NET 9):
- **L1 (In-Memory)**: Sub-millisecond access for frequently accessed data
- **L2 (Redis)**: Distributed cache for multi-instance consistency

### 2. Cache Stampede Protection

The `GetOrCreateAsync` method ensures only one request fetches data when cache expires:

```csharp
// If 1000 requests hit simultaneously, only 1 goes to DB
var result = await _hybridCache.GetOrCreateAsync(key, factory, options);
```

### 3. JSON Source Generators

Optimal serialization performance using C# 13 source generators:

```csharp
[JsonSerializable(typeof(Product))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ProductJsonContext : JsonSerializerContext { }
```

### 4. Tag-based Invalidation (NEW!)

Efficiently invalidate related cache entries:

```csharp
// When updating a product, invalidate all related caches
await _cacheService.RemoveByTagsAsync(new[] { 
    $"Product_{id}", 
    $"Category_{category}", 
    "All_Products" 
});
```

### 5. Circuit Breaker Resilience (NEW!)

Polly circuit breaker automatically handles Redis failures:

```csharp
// If Redis fails, automatically downgrade to L1-only mode
// Logs warning and continues serving from in-memory cache
// Automatically retries and restores L2 when Redis recovers
```

### 6. Distributed Locking

RedLock algorithm implementation for distributed systems:

```csharp
await using var redLock = await _lockFactory.CreateLockAsync(
    resource, expiryTime, waitTime, retryTime);
if (redLock.IsAcquired)
{
    // Perform operation
}
```

## 📈 Architecture Benefits

1. **Performance**: Sub-millisecond response times with L1 hits
2. **Scalability**: Redis L2 ensures consistency across instances
3. **Reliability**: 
   - Stampede protection prevents database overload
   - Circuit breaker ensures graceful degradation
   - Automatic fallback to L1-only mode
4. **Flexibility**: Tag-based invalidation for complex cache patterns
5. **Maintainability**: Clean interfaces and dependency injection
6. **Observability**: Built-in logging at all levels

## 🧪 Testing

Build and test the solution:

```bash
# Build
dotnet build

# Run API
cd NitroCache.Api
dotnet run

# Run benchmarks
cd NitroCache.Benchmarks
dotnet run -c Release
```

## 📝 License

This project is a demonstration of .NET 9 caching capabilities.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📚 References

- [Microsoft.Extensions.Caching.Hybrid](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
- [RedLock.net](https://github.com/samcook/RedLock.net)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
- [System.Text.Json Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)