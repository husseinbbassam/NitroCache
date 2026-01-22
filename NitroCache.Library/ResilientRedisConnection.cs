using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace NitroCache.Library;

/// <summary>
/// Resilient Redis connection wrapper with circuit breaker pattern
/// Automatically downgrades to L1-only mode when Redis fails
/// </summary>
public class ResilientRedisConnection
{
    private readonly ILogger<ResilientRedisConnection> _logger;
    private readonly ResiliencePipeline _circuitBreaker;
    private IConnectionMultiplexer? _connection;
    private bool _isInDegradedMode;

    public ResilientRedisConnection(
        string connectionString,
        ILogger<ResilientRedisConnection> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure circuit breaker
        _circuitBreaker = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                // Open circuit after 3 consecutive failures
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _isInDegradedMode = true;
                    _logger.LogWarning(
                        "Circuit breaker opened. Redis connection failed. " +
                        "Downgrading to L1-only (in-memory) cache mode for {Duration} seconds. " +
                        "Exception: {Exception}",
                        30, args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _isInDegradedMode = false;
                    _logger.LogInformation(
                        "Circuit breaker closed. Redis connection restored. " +
                        "Resuming normal hybrid cache operation.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker half-open. Testing Redis connection...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Try to establish initial connection
        TryConnect(connectionString);
    }

    /// <summary>
    /// Indicates whether the service is in degraded mode (L1-only)
    /// </summary>
    public bool IsInDegradedMode => _isInDegradedMode;

    /// <summary>
    /// Gets the Redis connection if available, null if in degraded mode
    /// </summary>
    public IConnectionMultiplexer? Connection => _connection;

    /// <summary>
    /// Attempts to connect to Redis with circuit breaker protection
    /// </summary>
    private void TryConnect(string connectionString)
    {
        try
        {
            _connection = _circuitBreaker.Execute(() =>
            {
                _logger.LogInformation("Attempting to connect to Redis: {ConnectionString}", connectionString);
                return ConnectionMultiplexer.Connect(connectionString);
            });

            _logger.LogInformation("Successfully connected to Redis");
        }
        catch (Exception ex)
        {
            _isInDegradedMode = true;
            _logger.LogWarning(ex,
                "Failed to connect to Redis. Operating in L1-only (in-memory) cache mode. " +
                "Will retry connection automatically.");
        }
    }

    /// <summary>
    /// Executes an action with circuit breaker protection
    /// Returns default value if circuit is open
    /// </summary>
    public async Task<T?> ExecuteAsync<T>(Func<IConnectionMultiplexer, Task<T>> action, T? defaultValue = default)
    {
        if (_connection == null)
        {
            _logger.LogDebug("Redis connection not available, returning default value");
            return defaultValue;
        }

        try
        {
            return await _circuitBreaker.ExecuteAsync(async ct => await action(_connection), CancellationToken.None);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogDebug("Circuit breaker is open, Redis unavailable. Using L1 cache only.");
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error executing Redis operation. Falling back to L1 cache.");
            return defaultValue;
        }
    }

    /// <summary>
    /// Executes an action with circuit breaker protection
    /// </summary>
    public async Task ExecuteAsync(Func<IConnectionMultiplexer, Task> action)
    {
        if (_connection == null)
        {
            _logger.LogDebug("Redis connection not available, skipping operation");
            return;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(async ct => await action(_connection), CancellationToken.None);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogDebug("Circuit breaker is open, Redis unavailable. Skipping operation.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error executing Redis operation.");
        }
    }
}
