using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;

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
    private readonly ResilientRedisConnection? _resilientRedis;
    
    // In-memory tag-to-keys mapping for tag-based invalidation
    // Key: tag name, Value: set of cache keys
    private readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> _tagToKeys = new();
    
    // Key-to-tags mapping for cleanup
    // Key: cache key, Value: set of tags
    private readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> _keyToTags = new();

    public HybridCacheService(
        HybridCache hybridCache,
        ILogger<HybridCacheService> logger,
        ResilientRedisConnection? resilientRedis = null)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resilientRedis = resilientRedis;
        _defaultExpiration = TimeSpan.FromMinutes(5);

        // Log initial cache mode
        if (_resilientRedis?.IsInDegradedMode == true)
        {
            _logger.LogWarning(
                "NitroCache starting in DEGRADED MODE (L1-only). Redis is unavailable. " +
                "Only in-memory caching is active.");
        }
        else
        {
            _logger.LogInformation(
                "NitroCache starting in NORMAL MODE. Hybrid cache (L1 + L2) is active.");
        }
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

            if (_resilientRedis?.IsInDegradedMode == true)
            {
                _logger.LogDebug("Operating in L1-only mode for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Getting or creating cache entry for key: {Key}", key);
            }

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
    public async Task<T?> GetOrSetWithTagsAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        IEnumerable<string> tags,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(tags);

        var tagList = tags.ToList();
        if (tagList.Count == 0)
        {
            // If no tags provided, use regular method
            return await GetOrSetAsync(key, factory, expiration, cancellationToken);
        }

        try
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = expiration ?? _defaultExpiration,
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };

            _logger.LogDebug("Getting or creating cache entry for key: {Key} with tags: {Tags}", 
                key, string.Join(", ", tagList));

            // Register tags for this key
            RegisterKeyTags(key, tagList);

            var result = await _hybridCache.GetOrCreateAsync(
                key,
                async cancel => await factory(cancel),
                options,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Cache entry retrieved/created for key: {Key} with tags", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or setting cache with tags for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Registers tags for a cache key
    /// </summary>
    private void RegisterKeyTags(string key, List<string> tags)
    {
        // Add key to each tag's set
        foreach (var tag in tags)
        {
            var keySet = _tagToKeys.GetOrAdd(tag, _ => new ConcurrentHashSet<string>());
            keySet.Add(key);
        }

        // Add tags to key's set
        var tagSet = _keyToTags.GetOrAdd(key, _ => new ConcurrentHashSet<string>());
        foreach (var tag in tags)
        {
            tagSet.Add(tag);
        }
    }

    /// <summary>
    /// Unregisters tags for a cache key
    /// </summary>
    private void UnregisterKeyTags(string key)
    {
        if (_keyToTags.TryRemove(key, out var tags))
        {
            foreach (var tag in tags)
            {
                if (_tagToKeys.TryGetValue(tag, out var keySet))
                {
                    keySet.Remove(key);
                    
                    // Clean up empty tag sets
                    if (keySet.Count == 0)
                    {
                        _tagToKeys.TryRemove(tag, out _);
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            _logger.LogDebug("Removing cache entry for key: {Key}", key);
            
            // Unregister tags for this key
            UnregisterKeyTags(key);
            
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
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);

        try
        {
            _logger.LogInformation("Invalidating all cache entries with tag: {Tag}", tag);

            if (_tagToKeys.TryGetValue(tag, out var keys))
            {
                var keyList = keys.ToList();
                _logger.LogDebug("Found {Count} cache entries to invalidate for tag: {Tag}", keyList.Count, tag);

                // Remove all keys associated with this tag
                foreach (var key in keyList)
                {
                    await RemoveAsync(key, cancellationToken);
                }

                _logger.LogInformation("Invalidated {Count} cache entries for tag: {Tag}", keyList.Count, tag);
            }
            else
            {
                _logger.LogDebug("No cache entries found for tag: {Tag}", tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by tag: {Tag}", tag);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var tagList = tags.ToList();
        if (tagList.Count == 0)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Invalidating cache entries for tags: {Tags}", string.Join(", ", tagList));

            // Collect all unique keys from all tags
            var allKeys = new HashSet<string>();
            foreach (var tag in tagList)
            {
                if (_tagToKeys.TryGetValue(tag, out var keys))
                {
                    foreach (var key in keys)
                    {
                        allKeys.Add(key);
                    }
                }
            }

            _logger.LogDebug("Found {Count} unique cache entries to invalidate", allKeys.Count);

            // Remove all keys
            foreach (var key in allKeys)
            {
                await RemoveAsync(key, cancellationToken);
            }

            _logger.LogInformation("Invalidated {Count} cache entries for {TagCount} tags", 
                allKeys.Count, tagList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by tags: {Tags}", string.Join(", ", tagList));
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
