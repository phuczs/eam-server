using EAM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<(IEnumerable<Payment> Items, int Total)> GetPagedByUserIdAsync(Guid userId, int skip, int take);
    }
}
