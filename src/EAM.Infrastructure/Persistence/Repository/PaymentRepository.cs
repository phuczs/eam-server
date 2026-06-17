

using EAM.Application.Interfaces.Repositories;
using EAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace EAM.Infrastructure.Persistence.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly EamDbContext _context;

        public PaymentRepository(EamDbContext context) {
            _context = context;
        }

        public async Task<(IEnumerable<Payment> Items, int Total)> GetPagedByUserIdAsync(Guid userId, int skip, int take)
        {
            var query = _context.Payments.AsNoTracking().Where(p => p.UserId == userId);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.CreatedAt)
                                   .Skip(skip)
                                   .Take(take)
                                   .ToListAsync();

            return (items, total);
        }
    }
}
