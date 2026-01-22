namespace NitroCache.Library;

/// <summary>
/// Configuration options for NitroCache
/// </summary>
public class NitroCacheOptions
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// L1 (in-memory) cache expiration time
    /// </summary>
    public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum size for local cache in MB
    /// </summary>
    public int MaximumLocalCacheSizeMB { get; set; } = 512;
}
