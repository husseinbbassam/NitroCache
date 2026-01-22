namespace NitroCache.Library;

/// <summary>
/// Generic cache service interface providing high-level caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value or sets it using the provided factory function
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached or newly created value</returns>
    Task<T?> GetOrSetAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> factory, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cached value or sets it using the provided factory function with tags for invalidation
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <param name="tags">Tags to associate with this cache entry for group invalidation</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached or newly created value</returns>
    Task<T?> GetOrSetWithTagsAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        IEnumerable<string> tags,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries associated with the specified tag
    /// </summary>
    /// <param name="tag">The tag to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries associated with any of the specified tags
    /// </summary>
    /// <param name="tags">The tags to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes a cached value by re-executing the factory function
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key to refresh</param>
    /// <param name="factory">Factory function to create the new value</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<T?> RefreshAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> factory, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default);
}
