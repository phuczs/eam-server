using Microsoft.Extensions.Caching.Memory;
using EAM.Application.Interfaces.Infrastructures;

namespace EAM.Infrastructure.Caching;

/// <summary>
/// RAM cache for frequently read static lookups. Caches the in-flight <see cref="Task{T}"/>
/// itself to prevent cache stampede (dog-piling) under concurrent first-access.
/// </summary>
public class MemoryLookupCache : ILookupCache
{
    private readonly IMemoryCache _cache;

    public MemoryLookupCache(IMemoryCache cache) => _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out Task<T>? cachedTask) && cachedTask is not null)
            return await cachedTask;

        var newTask = factory();

        var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(ttl);
        _cache.Set(key, newTask, options);

        try
        {
            return await newTask;
        }
        catch
        {
            // Don't cache a failed lookup — evict so the next caller retries.
            _cache.Remove(key);
            throw;
        }
    }

    public void Invalidate(string key) => _cache.Remove(key);
}
