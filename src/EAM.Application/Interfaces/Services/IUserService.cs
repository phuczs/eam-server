using EAM.Application.Common;

namespace EAM.Application.Interfaces.Services;

/// <summary>
/// User (person) management distinct from the account credential. Representative stub —
/// replace the <c>object</c> placeholders with typed DTOs when the domain lands.
/// </summary>
public interface IUserService
{
    Task<PagedResult<object>> QueryAsync(PageRequest page, CancellationToken ct = default);

    Task<object?> GetAsync(Guid id, CancellationToken ct = default);

    Task<Guid> CreateAsync(object request, CancellationToken ct = default);

    Task UpdateAsync(Guid id, object request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
