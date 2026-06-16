namespace EAM.Application.Common;

/// <summary>Standard server-side paging envelope.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
}
