using NitroCache.Api.Database;
using NitroCache.Api.Models;
using NitroCache.Library;

namespace NitroCache.Api.Services;

/// <summary>
/// Product service that uses caching for database operations
/// </summary>
public class ProductService
{
    private readonly MockDatabase _database;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        MockDatabase database,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a product by ID with caching
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";
        
        _logger.LogInformation("Getting product {ProductId} (will use cache if available)", id);
        
        var product = await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for product {ProductId}, fetching from database", id);
                return await _database.GetProductByIdAsync(id, ct);
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return product;
    }

    /// <summary>
    /// Gets all products with caching
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "products:all";
        
        _logger.LogInformation("Getting all products (will use cache if available)");
        
        var products = await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for all products, fetching from database");
                return await _database.GetAllProductsAsync(ct);
            },
            TimeSpan.FromMinutes(2),
            cancellationToken);

        return products ?? new List<Product>();
    }

    /// <summary>
    /// Gets products by category with caching
    /// </summary>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:category:{category}";
        
        _logger.LogInformation("Getting products for category {Category} (will use cache if available)", category);
        
        var products = await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for category {Category}, fetching from database", category);
                return await _database.GetProductsByCategoryAsync(category, ct);
            },
            TimeSpan.FromMinutes(3),
            cancellationToken);

        return products ?? new List<Product>();
    }

    /// <summary>
    /// Invalidates cache for a specific product
    /// </summary>
    public async Task InvalidateProductCacheAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";
        
        _logger.LogInformation("Invalidating cache for product {ProductId}", id);
        
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    /// <summary>
    /// Invalidates all product caches
    /// </summary>
    public async Task InvalidateAllProductCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating all product caches");
        
        await _cacheService.RemoveAsync("products:all", cancellationToken);
    }
}
