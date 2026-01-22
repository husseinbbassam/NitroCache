using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace NitroCache.Library;

/// <summary>
/// Hybrid cache service implementation using Microsoft.Extensions.Caching.Hybrid
/// Provides L1 (in-memory) and L2 (Redis) caching with stampede protection
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly HybridCache _hybridCache;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public HybridCacheService(
        HybridCache hybridCache,
        ILogger<HybridCacheService> logger)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultExpiration = TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = expiration ?? _defaultExpiration,
                LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 cache
            };

            _logger.LogDebug("Getting or creating cache entry for key: {Key}", key);

            // HybridCache.GetOrCreateAsync provides built-in stampede protection
            // If multiple requests hit the same key, only one will execute the factory
            var result = await _hybridCache.GetOrCreateAsync(
                key,
                async cancel => await factory(cancel),
                options,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Cache entry retrieved/created for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or setting cache for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            _logger.LogDebug("Removing cache entry for key: {Key}", key);
            await _hybridCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache entry removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> RefreshAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        try
        {
            _logger.LogDebug("Refreshing cache entry for key: {Key}", key);
            
            // Remove the existing cache entry
            await RemoveAsync(key, cancellationToken);
            
            // Create a new cache entry
            var result = await GetOrSetAsync(key, factory, expiration, cancellationToken);
            
            _logger.LogDebug("Cache entry refreshed for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for key: {Key}", key);
            throw;
        }
    }
}
