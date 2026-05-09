using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Models.Auth;
using TravAi.Repositories.UserRepository;
using TravAi.Models.Enums;
using TravAi.Models.Common;


namespace TravAi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ISecurityService _securityService;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, ISecurityService securityService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _securityService = securityService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                throw new AppException("Email already registered.");
            }

            var existingUserByUserName = await _userRepository.GetByUserNameAsync(request.UserName);
            if (existingUserByUserName != null)
            {
                throw new AppException("Username already taken.");
            }

            Gender genderEnum;
            if (!Enum.TryParse(request.Gender, true, out genderEnum))
            {
                genderEnum = Gender.Male; // Default or throw error? Using Male as safe default or strictly require valid input.
                // Or better, make Gender nullable in Model and DTO? It is nullable string in DTO.
                // User Model has "Gender Gender", not nullable.
                // Let's assume validation happens or default to Male for now if invalid/missing, 
                // OR technically, if request.Gender is null, we can't parse it.
                // The User Model has "Gender Gender", which is non-nullable struct.
                // Let's modify logic to default to Male if parsing fails. 
            }

            var newUser = new User
            {
                UserName = request.UserName,
                Name = request.Name,
                Email = request.Email,
                PasswordHash = _securityService.HashPassword(request.Password),
                Role = Models.Enums.UserRole.User,
                DateOfBirth = request.DateOfBirth,
                Gender = Enum.TryParse<Gender>(request.Gender, true, out var g) ? g : Gender.Male, // Default to Male if invalid/null
                ProfilePic = request.ProfilePic,
                Status = Models.Enums.UserStatus.Pending,
                IsBanned = false,
                CreatedAt = DateTime.UtcNow,
                UserPhones = new List<UserPhone>()
            };

            // Add Primary Phone
            newUser.UserPhones.Add(new UserPhone
            {
                PhoneNumber = request.PhoneNumber,
                PhoneVerified = false
            });

            // Add Secondary Phone if provided
            if (!string.IsNullOrEmpty(request.SecondaryPhoneNumber))
            {
                newUser.UserPhones.Add(new UserPhone
                {
                    PhoneNumber = request.SecondaryPhoneNumber,
                    PhoneVerified = false
                });
            }

            await _userRepository.AddAsync(newUser);
            return await GenerateTokens(newUser);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Verify password
            if (!_securityService.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Incorrect password");
            }

            // Generate tokens
            return await GenerateTokens(user);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
        {
            // Validate old token
            var principal = GetPrincipalFromExpiredToken(token);
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user
            if (!long.TryParse(userId, out var userIdLong))
            {
                throw new UnauthorizedException("Invalid user ID format");
            }
            var user = await _userRepository.GetByIdAsync(userIdLong);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid user");
            }

            // Validate refresh token
            var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken == null || storedToken.UserId != userIdLong || storedToken.IsExpired)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            // Revoke old refresh token
            await _userRepository.RevokeRefreshTokenAsync(refreshToken);

            // Generate new tokens
            return await GenerateTokens(user);
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            await _userRepository.RevokeRefreshTokenAsync(refreshToken);
            return true;
        }

        public async Task<UserDto> GetProfileAsync(long userId)
        {
            var user = await _userRepository.GetFullUserByIdAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

            // We need to include UserPhones. Generic repo might not include it.
            // But let's assume we can get it from the user object if it was included.
            // Actually, base GenericRepository probably doesn't include it. 
            // I'll check IUserRepository to see if I need a specialized GetById with Includes.

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role.ToString(),
                ProfilePic = user.ProfilePic,
                PassportNumber = user.PassportNumber,
                Nationality = user.Nationality,
                PhoneNumber = user.UserPhones?.FirstOrDefault()?.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender.ToString(),
                Status = user.Status.ToString(),
                WalletBalance = user.WalletBalance,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateProfileAsync(long userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetFullUserByIdAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

            if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.UserName)) user.UserName = dto.UserName;
            if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
            if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth;
            
            if (!string.IsNullOrWhiteSpace(dto.Gender))
            {
                if (Enum.TryParse<Gender>(dto.Gender, true, out var g))
                    user.Gender = g;
            }

            if (!string.IsNullOrWhiteSpace(dto.ProfilePic)) user.ProfilePic = dto.ProfilePic;
            if (!string.IsNullOrWhiteSpace(dto.Nationality)) user.Nationality = dto.Nationality;
            if (!string.IsNullOrWhiteSpace(dto.PassportNumber)) user.PassportNumber = dto.PassportNumber;

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existingPhone = user.UserPhones?.FirstOrDefault();
                if (existingPhone != null)
                {
                    existingPhone.PhoneNumber = dto.PhoneNumber;
                }
                else
                {
                    user.UserPhones ??= new List<UserPhone>();
                    user.UserPhones.Add(new UserPhone
                    {
                        UserId = userId,
                        PhoneNumber = dto.PhoneNumber,
                        PhoneVerified = false
                    });
                }
            }

            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
        }

        public async Task ChangePasswordAsync(long userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

            if (!_securityService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedException("Current password is incorrect");

            user.PasswordHash = _securityService.HashPassword(dto.NewPassword);
            _userRepository.Update(user);
        }

        #region Helper Methods

        private async Task<AuthResponse> GenerateTokens(User user)
        {
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            await _userRepository.AddRefreshTokenAsync(user.Id, new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1" // Should replace with real IP in production
            });

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                TokenExpiration = token.ValidTo,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        private JwtSecurityToken GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()), // Add role claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["JWT:Secret"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JWT:TokenValidityInMinutes"])),
                signingCredentials: creds
            );
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
                ValidateLifetime = false // We want to validate expired tokens
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }

        #endregion
    }
}
