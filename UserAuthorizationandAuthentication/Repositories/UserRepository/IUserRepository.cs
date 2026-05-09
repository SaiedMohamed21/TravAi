using TravAi.Models.Auth;
using TravAi.Repositories.GenericRepository;

namespace TravAi.Repositories.UserRepository
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUserNameAsync(string userName);
        Task AddRefreshTokenAsync(long userId, RefreshToken token);
        Task<RefreshToken> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task<User> GetFullUserByIdAsync(long id);
    }
}
