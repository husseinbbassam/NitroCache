using System.Text.Json.Serialization;

namespace NitroCache.Library;

/// <summary>
/// Base class for cacheable entities with tag support
/// </summary>
public class CachedEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tags associated with this cache entry for invalidation purposes
    /// </summary>
    [JsonIgnore]
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Generic wrapper for cached data with metadata
/// </summary>
/// <typeparam name="T">The type of data being cached</typeparam>
public class CachedData<T>
{
    /// <summary>
    /// The actual cached data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// When this cache entry was created
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tags for invalidation
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// JSON serializer context for NitroCache entities using source generators
/// This provides ultra-fast serialization without reflection
/// </summary>
[JsonSerializable(typeof(CachedEntity))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class NitroCacheJsonContext : JsonSerializerContext
{
}
