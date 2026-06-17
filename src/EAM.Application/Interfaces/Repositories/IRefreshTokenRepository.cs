using EAM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        /// <summary>Returns the active token that matches <paramref name="tokenHash"/>, with its User loaded.</summary>
        Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken ct = default);

        /// <summary>Returns all non-expired, non-revoked tokens for the given user.</summary>
        Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Stages a new refresh token for insertion (persisted on next SaveChanges).</summary>
        void Add(RefreshToken token);
          Task<int> SaveChangesAsync(CancellationToken ct = default);
    }

}
