using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
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

        // Register Redis connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(options.RedisConnectionString));

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

            // Configure serialization with source generators for optimal performance
            // This avoids reflection at runtime
            var jsonOptions = new JsonSerializerOptions
            {
                TypeInfoResolverChain = { NitroCacheJsonContext.Default }
            };
        });

        // Register cache serializer for Redis (L2)
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
