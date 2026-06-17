using EAM.Domain.Entities;


namespace EAM.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(int skip, int take);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        //Check NRIC
        Task<bool> IsOfficialIdExistsAsync(string officialId);
        //Combine to Transaction
        Task ExecuteInTransactionAsync(Func<Task> action);
        //Search + Filter
        Task<(IEnumerable<User> Items, int Total)> SearchPagedAsync(
                string? keyword,
                string? status,
                int? minAge,
                int? maxAge,
                int skip,
                int take);
        //For DashBoard
        Task<int> GetCountByStatusAsync(string status);
        Task<List<User>> GetPendingClosureUsersAsync(int top);
        Task<List<UserException>> GetExceptionAlertsAsync();
        Task<(IEnumerable<User> Items, int Total)> GetPagedPendingClosureAsync(int skip, int take);

    }
}
