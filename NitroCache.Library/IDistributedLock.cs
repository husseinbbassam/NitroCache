namespace NitroCache.Library;

/// <summary>
/// Interface for distributed lock operations
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Acquires a distributed lock and executes the action
    /// </summary>
    /// <param name="resource">The resource name to lock</param>
    /// <param name="expiryTime">Lock expiration time</param>
    /// <param name="action">Action to execute while holding the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if lock was acquired and action executed, false otherwise</returns>
    Task<bool> ExecuteWithLockAsync(
        string resource,
        TimeSpan expiryTime,
        Func<Task> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a distributed lock and executes the function
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="resource">The resource name to lock</param>
    /// <param name="expiryTime">Lock expiration time</param>
    /// <param name="func">Function to execute while holding the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the function execution</returns>
    Task<(bool Success, T? Result)> ExecuteWithLockAsync<T>(
        string resource,
        TimeSpan expiryTime,
        Func<Task<T>> func,
        CancellationToken cancellationToken = default);
}
