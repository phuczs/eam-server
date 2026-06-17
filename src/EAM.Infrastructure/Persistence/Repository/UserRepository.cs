using EAM.Application.Interfaces.Repositories;
using EAM.Domain.Entities;
using Google;
using Microsoft.EntityFrameworkCore;


namespace EAM.Infrastructure.Persistence.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly EamDbContext _context;

        public UserRepository(EamDbContext context)
        {
            _context = context;
        }
        public async Task<(IEnumerable<User> Items, int Total)> GetPagedPendingClosureAsync(int skip, int take)
        {
            var query = _context.Users.AsNoTracking()
                .Where(u => u.AccountStatus == "pending_closure");

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(u => u.AccountPendingClosureAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, total);
        }
        //For dashboard
        public async Task<int> GetCountByStatusAsync(string status)
    => await _context.Users.CountAsync(u => u.AccountStatus == status);

        public async Task<List<User>> GetPendingClosureUsersAsync(int top)
            => await _context.Users
                .Where(u => u.AccountStatus == "pending_closure")
                .OrderByDescending(u => u.AccountPendingClosureAt)
                .Take(top)
                .ToListAsync();
        //Get exception
        public async Task<List<UserException>> GetExceptionAlertsAsync()
        {
            return await _context.Users
                .Where(u => u.AccountStatus == "active" && !u.BankAccounts.Any())
                .Select(u => new UserException { UserId = u.Id, Reason = "Missing Bank Account" })
                .Take(10)
                .ToListAsync();
        }
        //Search
        public async Task<(IEnumerable<User> Items, int Total)> SearchPagedAsync(
        string? keyword, 
        string? status, 
        int? minAge, 
        int? maxAge, 
        int skip, 
        int take)
    {
        var query = _context.Set<User>().AsNoTracking();

        // 1. Keyword Filter
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim().ToLower();
            query = query.Where(u => 
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.OfficialId != null && u.OfficialId.ToLower().EndsWith(term))
            );
        }

        //Status Filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusTerm = status.Trim().ToLower();
            query = query.Where(u => u.AccountStatus.ToLower() == statusTerm);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        if (minAge.HasValue)
        {
            var maxDobForMinAge = today.AddYears(-minAge.Value);
            query = query.Where(u => u.DateOfBirth <= maxDobForMinAge);
        }
        
        if (maxAge.HasValue)
        {

            var minDobForMaxAge = today.AddYears(-(maxAge.Value + 1));
            query = query.Where(u => u.DateOfBirth > minDobForMaxAge);
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        // execute queries
        var total = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();

        return (items, total);
    }
        //  check NRIC
        public async Task<bool> IsOfficialIdExistsAsync(string officialId)
        {
            return await _context.Set<User>().AnyAsync(u => u.OfficialId == officialId);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await action();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw; 
                }
            });
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Set<User>().FindAsync(id);
        }
        public async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(int skip, int take)
        {
            var query = _context.Set<User>().AsNoTracking();

            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();

            return (items, total);
        }
        public async Task<User> AddAsync(User user)
        {
            await _context.Set<User>().AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Set<User>().Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Set<User>().Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
