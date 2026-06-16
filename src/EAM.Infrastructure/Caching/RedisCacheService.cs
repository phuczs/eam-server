using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using EAM.Application.Interfaces.Infrastructures;
using StackExchange.Redis;

namespace EAM.Infrastructure.Caching;

/// <summary>
/// Distributed cache + lock over Redis, with atomic unlock (Lua check-and-delete).
/// <see cref="IConnectionMultiplexer"/> is optional: when Redis isn't configured the
/// lock degrades to a no-op so the app still runs locally.
/// </summary>
public class RedisCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;

    // Atomic check-and-delete prevents releasing a lock another owner re-acquired.
    private const string UnlockLuaScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _redis = redis;
    }

    // ── Basic operations ──
    public async Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        await _cache.GetStringAsync(key, ct);

    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default) =>
        _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _cache.RemoveAsync(key, ct);

    // ── Generic (JSON) operations ──
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var json = await GetAsync(key, ct);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        return SetAsync(key, json, ttl, ct);
    }

    // ── Distributed lock ──
    public async Task<IAsyncDisposable?> AcquireLockAsync(string resource, TimeSpan expiry, CancellationToken ct = default)
    {
        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            var token = Guid.NewGuid().ToString("N");
            var lockKey = $"lock:{resource}";

            var acquired = await db.StringSetAsync(lockKey, token, expiry, When.NotExists, CommandFlags.None);
            if (!acquired)
                throw new InvalidOperationException($"Could not acquire distributed lock for resource: {resource}.");

            return new RedisLock(db, lockKey, token);
        }

        return new NoopLock();
    }

    private sealed class RedisLock : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLock(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _db.ScriptEvaluateAsync(
                    UnlockLuaScript,
                    new RedisKey[] { _key },
                    new RedisValue[] { _token });
            }
            catch (RedisException)
            {
                // Best-effort release — the lock expires on its own anyway.
            }
        }
    }

    private sealed class NoopLock : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
