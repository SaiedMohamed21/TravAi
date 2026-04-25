using Microsoft.EntityFrameworkCore;
using UserAuthorizationandAuthentication.Models.Auth;
using UserAuthorizationandAuthentication.Repositories.GenericRepository;
using UserAuthorizationandAuthentication.Data;

namespace UserAuthorizationandAuthentication.Repositories.UserRepository
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUserNameAsync(string userName)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task AddRefreshTokenAsync(long userId, RefreshToken token)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            user.RefreshTokens.Add(token);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                _context.RefreshTokens.Remove(refreshToken);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User> GetFullUserByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.UserPhones)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
