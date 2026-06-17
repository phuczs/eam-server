using EAM.Application.Interfaces.Repositories;
using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Infrastructure.Persistence.Repository
{
    public sealed class ExternalIdentityRepository : IExternalIdentityRepository
    {
        private readonly EamDbContext _db;

        public ExternalIdentityRepository(EamDbContext db) => _db = db;

        public Task<ExternalIdentity?> FindByProviderSubjectAsync(
            string provider, string subjectId, CancellationToken ct = default)
            => _db.ExternalIdentities
                  .Include(e => e.User)
                  .FirstOrDefaultAsync(
                      e => e.Provider == provider && e.ProviderSubjectId == subjectId, ct);
         public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);
    }


}
