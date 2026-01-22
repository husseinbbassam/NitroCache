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
        
        // Cache with tags for invalidation
        // Note: We can't include Category tag here without fetching the product first
        // So we only use Product_ID and All_Products tags
        var product = await _cacheService.GetOrSetWithTagsAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for product {ProductId}, fetching from database", id);
                return await _database.GetProductByIdAsync(id, ct);
            },
            new[] { "All_Products", $"Product_{id}" },
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
        
        var products = await _cacheService.GetOrSetWithTagsAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for all products, fetching from database");
                return await _database.GetAllProductsAsync(ct);
            },
            new[] { "All_Products" },
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
        
        var products = await _cacheService.GetOrSetWithTagsAsync(
            cacheKey,
            async ct =>
            {
                _logger.LogInformation("Cache miss for category {Category}, fetching from database", category);
                return await _database.GetProductsByCategoryAsync(category, ct);
            },
            new[] { "All_Products", $"Category_{category}" },
            TimeSpan.FromMinutes(3),
            cancellationToken);

        return products ?? new List<Product>();
    }

    /// <summary>
    /// Invalidates cache for a specific product and its category
    /// </summary>
    public async Task InvalidateProductCacheAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating cache for product {ProductId}", id);
        
        // Invalidate by product tag
        await _cacheService.RemoveByTagAsync($"Product_{id}", cancellationToken);
    }

    /// <summary>
    /// Invalidates all product caches using tag-based invalidation
    /// </summary>
    public async Task InvalidateAllProductCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating all product caches");
        
        // Invalidate all entries tagged with "All_Products"
        await _cacheService.RemoveByTagAsync("All_Products", cancellationToken);
    }

    /// <summary>
    /// Invalidates all cache entries for a specific category
    /// </summary>
    public async Task InvalidateCategoryCacheAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating cache for category {Category}", category);
        
        // Invalidate all entries tagged with the category
        await _cacheService.RemoveByTagAsync($"Category_{category}", cancellationToken);
    }
}
