using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.Services;
using System.Security.Claims;

namespace UserAuthorizationandAuthentication.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(new ApiResponse<AuthResponse>(response, "User registered successfully."));
            }
            catch (Exception ex) // Catch generic Exception/ApplicationException
            {
                return BadRequest(new ApiResponse<object>(false, ex.Message));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(new ApiResponse<AuthResponse>(response, "Login successful."));
            }
            catch (Exception ex)
            {
                // You might want to distinguish 404 vs 401 here or let logic handle it. 
                // For security, often generic "Invalid credentials" is best, but here we pass message.
                return Unauthorized(new ApiResponse<object>(false, ex.Message));
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
                return Ok(new ApiResponse<AuthResponse>(response, "Token refreshed successfully."));
            }
            catch (Exception ex)
            {
                return Unauthorized(new ApiResponse<object>(false, ex.Message));
            }
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return Ok(new ApiResponse<object>("Token revoked successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(false, ex.Message));
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
                var profile = await _authService.GetProfileAsync(userId);
                return Ok(new ApiResponse<UserDto>(profile, "Profile retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(false, ex.Message));
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
                await _authService.UpdateProfileAsync(userId, request);
                return Ok(new ApiResponse<object>("Profile updated successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(false, ex.Message));
            }
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                var userId = long.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
                await _authService.ChangePasswordAsync(userId, request);
                return Ok(new ApiResponse<object>("Password changed successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(false, ex.Message));
            }
        }
    }
}



