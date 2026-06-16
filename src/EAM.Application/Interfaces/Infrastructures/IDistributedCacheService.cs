namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>Distributed cache + lock abstraction (Redis in production).</summary>
public interface IDistributedCacheService
{
    // ── Basic string operations ──
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);

    // ── Generic (JSON) operations ──
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    // ── Distributed lock ──
    /// <summary>
    /// Acquires a distributed lock. Returns an <see cref="IAsyncDisposable"/> so the lock
    /// is released asynchronously via network I/O.
    /// </summary>
    Task<IAsyncDisposable?> AcquireLockAsync(string resource, TimeSpan expiry, CancellationToken ct = default);
}
