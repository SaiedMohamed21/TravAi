using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Services;
using TravAi.Services.Auth;
using TravAi.Data;
using System.Security.Claims;

namespace TravAi.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly TravAi.Services.FileStorage.IFileService _fileService;

        public UsersController(IAuthService authService, TravAi.Services.FileStorage.IFileService fileService)
        {
            _authService = authService;
            _fileService = fileService;
        }

        [Authorize]
        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = long.Parse(userIdStr);

            var imageUrl = await _fileService.SaveProfileImageAsync(file);
            
            // Link image to user profile automatically (Option A)
            await _authService.UpdateProfileAsync(userId, new UpdateProfileDto { ProfilePic = imageUrl });

            return Ok(new ApiResponse<string>(imageUrl, "Profile picture uploaded and updated successfully."));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(new ApiResponse<AuthResponse>(response, "User registered successfully."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(new ApiResponse<AuthResponse>(response, "Login successful."));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
            return Ok(new ApiResponse<AuthResponse>(response, "Token refreshed successfully."));
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            await _authService.RevokeTokenAsync(request.RefreshToken);
            return Ok(new ApiResponse<object>("Token revoked successfully."));
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            
            var userId = long.Parse(userIdStr);
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(new ApiResponse<UserDto>(profile, "Profile retrieved successfully."));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = long.Parse(userIdStr);
            await _authService.UpdateProfileAsync(userId, request);
            return Ok(new ApiResponse<object>("Profile updated successfully."));
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = long.Parse(userIdStr);
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new ApiResponse<object>("Password changed successfully."));
        }
    }
}
