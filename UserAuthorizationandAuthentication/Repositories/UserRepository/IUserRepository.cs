using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Repositories.GenericRepository;

namespace UserAuthorizationandAuthentication.Repositories.UserRepository
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUserNameAsync(string userName);
        Task AddRefreshTokenAsync(long userId, RefreshToken token);
        Task<RefreshToken> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);

    }
}
