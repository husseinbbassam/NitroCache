using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace NitroCache.Library;

/// <summary>
/// Distributed lock implementation using RedLock.net
/// Provides distributed locking across multiple server instances
/// </summary>
public class RedisDistributedLock : IDistributedLock, IDisposable
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly ILogger<RedisDistributedLock> _logger;

    public RedisDistributedLock(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisDistributedLock> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // Create RedLock factory with the Redis connection
        var endPoints = new List<RedLockMultiplexer>
        {
            new RedLockMultiplexer(connectionMultiplexer)
        };

        _lockFactory = RedLockFactory.Create(endPoints);
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteWithLockAsync(
        string resource,
        TimeSpan expiryTime,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            _logger.LogDebug("Attempting to acquire lock for resource: {Resource}", resource);

            await using var redLock = await _lockFactory.CreateLockAsync(
                resource,
                expiryTime,
                TimeSpan.FromSeconds(5), // wait time
                TimeSpan.FromSeconds(1), // retry time
                cancellationToken);

            if (redLock.IsAcquired)
            {
                _logger.LogDebug("Lock acquired for resource: {Resource}", resource);
                await action();
                _logger.LogDebug("Action completed for resource: {Resource}", resource);
                return true;
            }

            _logger.LogWarning("Failed to acquire lock for resource: {Resource}", resource);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing with lock for resource: {Resource}", resource);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, T? Result)> ExecuteWithLockAsync<T>(
        string resource,
        TimeSpan expiryTime,
        Func<Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            _logger.LogDebug("Attempting to acquire lock for resource: {Resource}", resource);

            await using var redLock = await _lockFactory.CreateLockAsync(
                resource,
                expiryTime,
                TimeSpan.FromSeconds(5), // wait time
                TimeSpan.FromSeconds(1), // retry time
                cancellationToken);

            if (redLock.IsAcquired)
            {
                _logger.LogDebug("Lock acquired for resource: {Resource}", resource);
                var result = await func();
                _logger.LogDebug("Function completed for resource: {Resource}", resource);
                return (true, result);
            }

            _logger.LogWarning("Failed to acquire lock for resource: {Resource}", resource);
            return (false, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing with lock for resource: {Resource}", resource);
            throw;
        }
    }

    public void Dispose()
    {
        (_lockFactory as IDisposable)?.Dispose();
    }
}
