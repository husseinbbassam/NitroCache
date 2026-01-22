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
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

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
