namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>In-process cache for frequently read static lookups.</summary>
public interface ILookupCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
    void Invalidate(string key);
}
