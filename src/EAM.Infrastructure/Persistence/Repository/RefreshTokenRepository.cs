using EAM.Application.Interfaces.Repositories;
using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Infrastructure.Persistence.Repository
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly EamDbContext _db;

        public RefreshTokenRepository(EamDbContext db) => _db = db;

        public Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken ct = default)
            => _db.RefreshTokens
                  .Include(r => r.User)
                  .FirstOrDefaultAsync(
                      r => r.TokenHash == tokenHash
                        && r.RevokedAtUtc == null
                        && r.ExpiresAtUtc > DateTime.UtcNow,
                      ct);

        public Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
            => _db.RefreshTokens
                  .Where(r => r.UserId == userId
                           && r.RevokedAtUtc == null
                           && r.ExpiresAtUtc > DateTime.UtcNow)
                  .ToListAsync(ct);
        
        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);
        public void Add(RefreshToken token) => _db.RefreshTokens.Add(token);
    }

}
