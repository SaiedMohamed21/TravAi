using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Models.Enums;
using TravAi.Data;
using TravAi.DTOs.AdminManagement;
using TravAi.Models.Auth;


namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/admin/users")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirlineAdminUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AirlineAdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/airline/admin/users - Get all users (Admin only)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.UserName,
                    Role = u.Role.ToString(),
                    Status = u.Status.ToString(),
                    u.Nationality,
                    u.PassportNumber,
                    u.ProfilePic,
                    u.CreatedAt,
                    u.IsBanned
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(new ApiResponse<object>(users));
        }

        // PUT: api/airline/admin/users/{id}/status - Update user status (Admin only)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(long id, [FromBody] UpdateUserStatusDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<object>(null, "User not found"));

            // Parse and update status
            if (Enum.TryParse<UserStatus>(dto.Status, true, out var newStatus))
            {
                user.Status = newStatus;
                
                // If banning user, set IsBanned flag
                if (newStatus == UserStatus.Banned)
                    user.IsBanned = true;
                else
                    user.IsBanned = false;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(new
                {
                    user.Id,
                    user.Name,
                    Status = user.Status.ToString(),
                    user.IsBanned
                }, "User status updated successfully"));
            }

            return BadRequest(new ApiResponse<object>(null, "Invalid status value"));
        }

        // PUT: api/airline/admin/users/{id}/role - Update user role (Admin only)
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(long id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<object>(null, "User not found"));

            // Parse and update role
            if (Enum.TryParse<UserRole>(dto.Role, true, out var newRole))
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(new
                {
                    user.Id,
                    user.Name,
                    Role = user.Role.ToString()
                }, "User role updated successfully"));
            }

            return BadRequest(new ApiResponse<object>(null, "Invalid role value"));
        }
    }
}
