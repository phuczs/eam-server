using EAM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Repositories
{
    public interface IExternalIdentityRepository
    {
        /// <summary>
        /// Returns the identity (with its <see cref="User"/> eager-loaded) that matches
        /// the given provider + subject ID, or <c>null</c> if none exists.
        /// </summary>
        Task<ExternalIdentity?> FindByProviderSubjectAsync(
            string provider, string subjectId, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}