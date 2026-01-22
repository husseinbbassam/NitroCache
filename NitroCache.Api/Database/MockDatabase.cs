using NitroCache.Api.Models;

namespace NitroCache.Api.Database;

/// <summary>
/// Mock database for demonstration purposes
/// Simulates slow database operations
/// </summary>
public class MockDatabase
{
    private readonly List<Product> _products;
    private readonly Random _random = new();

    public MockDatabase()
    {
        _products = GenerateMockProducts();
    }

    /// <summary>
    /// Simulates a slow database query
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Simulate database latency (100-500ms)
        await Task.Delay(_random.Next(100, 500), cancellationToken);
        
        return _products.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Simulates a slow database query for all products
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        // Simulate database latency (200-800ms)
        await Task.Delay(_random.Next(200, 800), cancellationToken);
        
        return _products.ToList();
    }

    /// <summary>
    /// Simulates getting products by category
    /// </summary>
    public async Task<List<Product>> GetProductsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        // Simulate database latency (150-600ms)
        await Task.Delay(_random.Next(150, 600), cancellationToken);
        
        return _products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private List<Product> GenerateMockProducts()
    {
        var categories = new[] { "Electronics", "Clothing", "Books", "Home & Garden", "Sports" };
        var products = new List<Product>();

        for (int i = 1; i <= 100; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Description = $"This is a detailed description of product {i}. It provides excellent value and quality.",
                Price = Math.Round((decimal)(_random.NextDouble() * 500 + 10), 2),
                Category = categories[_random.Next(categories.Length)],
                StockQuantity = _random.Next(0, 1000),
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
            });
        }

        return products;
    }
}
