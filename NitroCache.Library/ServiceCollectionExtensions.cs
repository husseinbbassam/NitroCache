using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace NitroCache.Library;

/// <summary>
/// Extension methods for configuring NitroCache services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NitroCache services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddNitroCache(
        this IServiceCollection services,
        Action<NitroCacheOptions>? configure = null)
    {
        var options = new NitroCacheOptions();
        configure?.Invoke(options);

        // Register resilient Redis connection with circuit breaker
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ResilientRedisConnection>>();
            return new ResilientRedisConnection(options.RedisConnectionString, logger);
        });

        // Register Redis connection (may be null if in degraded mode)
        services.AddSingleton(sp =>
        {
            var resilientConnection = sp.GetRequiredService<ResilientRedisConnection>();
            return resilientConnection.Connection;
        });

        // Register HybridCache with configuration and source generator context
        services.AddHybridCache(hybridOptions =>
        {
            hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = options.DefaultExpiration,
                LocalCacheExpiration = options.LocalCacheExpiration
            };

            hybridOptions.MaximumPayloadBytes = 1024 * 1024; // 1MB max payload
            hybridOptions.MaximumKeyLength = 512;
        });

        // Register cache serializer for Redis (L2) - only if Redis is available
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.RedisConnectionString;
            redisOptions.InstanceName = "NitroCache:";
        });

        // Register our services
        services.AddSingleton<ICacheService, HybridCacheService>();
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();

        return services;
    }
}
