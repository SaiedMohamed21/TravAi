using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.Data;
using TravAi.Models.Admin;
using TravAi.Models.Admin.DTOs;

namespace TravAi.Controllers.Admin
{
    [Route("api/admin/commissions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminPlatformCommissionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminPlatformCommissionController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task AutoActivateCommissionsAsync()
        {
            var now = DateTime.UtcNow;
            var serviceTypes = new[] { "Hotel", "Tour", "Airline" };
            bool changed = false;

            foreach (var type in serviceTypes)
            {
                var pastCommissions = await _context.PlatformCommissions
                    .Where(c => c.ServiceType == type && c.EffectiveFrom <= now)
                    .OrderByDescending(c => c.EffectiveFrom)
                    .ThenByDescending(c => c.Id)
                    .ToListAsync();

                if (!pastCommissions.Any()) continue;

                var latest = pastCommissions.First();
                if (!latest.IsActive)
                {
                    latest.IsActive = true;
                    changed = true;
                }

                foreach (var old in pastCommissions.Skip(1))
                {
                    if (old.IsActive)
                    {
                        old.IsActive = false;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
        }

        private PlatformCommissionDto MapToDto(PlatformCommission c, DateTime now)
        {
            return new PlatformCommissionDto
            {
                Id = c.Id,
                ServiceType = c.ServiceType,
                Percentage = c.Percentage,
                EffectiveFrom = c.EffectiveFrom,
                CreatedAt = c.CreatedAt,
                CreatedByAdminName = c.CreatedByAdminUser?.UserName ?? "System",
                Notes = c.Notes,
                IsActive = c.IsActive,
                Status = c.IsActive ? "Active" : (!c.IsActive && c.EffectiveFrom > now ? "Scheduled" : "Old")
            };
        }

        // GET: /api/admin/commissions
        [HttpGet]
        public async Task<IActionResult> GetAllCommissions()
        {
            await AutoActivateCommissionsAsync();

            var now = DateTime.UtcNow;
            var serviceTypes = new[] { "Hotel", "Tour", "Airline" };
            var response = new List<PlatformCommissionDashboardResponse>();

            var allCommissions = await _context.PlatformCommissions
                .Include(c => c.CreatedByAdminUser)
                .ToListAsync();

            foreach (var type in serviceTypes)
            {
                var typeCommissions = allCommissions
                    .Where(c => c.ServiceType == type)
                    .OrderByDescending(c => c.EffectiveFrom)
                    .ToList();

                var active = typeCommissions.FirstOrDefault(c => c.IsActive);
                var pending = typeCommissions.FirstOrDefault(c => !c.IsActive && c.EffectiveFrom > now);

                var dto = new PlatformCommissionDashboardResponse
                {
                    ServiceType = type,
                    ActiveCommission = active != null ? MapToDto(active, now) : null,
                    PendingCommission = pending != null ? MapToDto(pending, now) : null,
                    HistoryCount = typeCommissions.Count
                };

                if (pending != null)
                {
                    var diff = pending.EffectiveFrom - now;
                    if (diff.TotalSeconds > 0)
                    {
                        dto.RemainingDays = diff.Days;
                        dto.RemainingHours = diff.Hours;
                        dto.RemainingMinutes = diff.Minutes;
                        dto.RemainingSeconds = diff.Seconds;
                    }
                }

                response.Add(dto);
            }

            return Ok(response);
        }

        // GET: /api/admin/commissions/{serviceType}
        [HttpGet("{serviceType}")]
        public async Task<IActionResult> GetCommissionsByService(string serviceType)
        {
            var validTypes = new[] { "Hotel", "Tour", "Airline" };
            if (!validTypes.Contains(serviceType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid service type. Must be Hotel, Tour, or Airline.");
            }

            await AutoActivateCommissionsAsync();

            var now = DateTime.UtcNow;
            var typeCommissions = await _context.PlatformCommissions
                .Include(c => c.CreatedByAdminUser)
                .Where(c => c.ServiceType.ToLower() == serviceType.ToLower())
                .OrderByDescending(c => c.EffectiveFrom)
                .ToListAsync();

            var active = typeCommissions.FirstOrDefault(c => c.IsActive);
            var pending = typeCommissions.FirstOrDefault(c => !c.IsActive && c.EffectiveFrom > now);

            var dto = new PlatformCommissionDashboardResponse
            {
                ServiceType = serviceType,
                ActiveCommission = active != null ? MapToDto(active, now) : null,
                PendingCommission = pending != null ? MapToDto(pending, now) : null,
                HistoryCount = typeCommissions.Count
            };

            if (pending != null)
            {
                var diff = pending.EffectiveFrom - now;
                if (diff.TotalSeconds > 0)
                {
                    dto.RemainingDays = diff.Days;
                    dto.RemainingHours = diff.Hours;
                    dto.RemainingMinutes = diff.Minutes;
                    dto.RemainingSeconds = diff.Seconds;
                }
            }

            return Ok(dto);
        }

        // POST: /api/admin/commissions/{serviceType}
        [HttpPost("{serviceType}")]
        public async Task<IActionResult> CreateCommission(string serviceType, [FromBody] CreatePlatformCommissionRequest request)
        {
            var validTypes = new[] { "Hotel", "Tour", "Airline" };
            if (!validTypes.Contains(serviceType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid service type. Must be Hotel, Tour, or Airline.");
            }

            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !long.TryParse(adminIdClaim, out var adminUserId))
            {
                return Unauthorized();
            }

            await AutoActivateCommissionsAsync();

            var now = DateTime.UtcNow;
            var formattedServiceType = validTypes.First(v => v.Equals(serviceType, StringComparison.OrdinalIgnoreCase));

            var existingCommissions = await _context.PlatformCommissions
                .Where(c => c.ServiceType == formattedServiceType)
                .OrderByDescending(c => c.EffectiveFrom)
                .ToListAsync();

            var pending = existingCommissions.FirstOrDefault(c => !c.IsActive && c.EffectiveFrom > now);
            if (pending != null)
            {
                return BadRequest($"A pending commission update already exists for {formattedServiceType}. It will be active at {pending.EffectiveFrom:yyyy-MM-dd HH:mm} UTC.");
            }

            var active = existingCommissions.FirstOrDefault(c => c.IsActive);
            
            var newCommission = new PlatformCommission
            {
                ServiceType = formattedServiceType,
                Percentage = request.Percentage,
                CreatedAt = now,
                CreatedByAdminUserId = adminUserId,
                Notes = request.Notes,
                IsActive = active == null,
                EffectiveFrom = active == null ? now : now.AddDays(30)
            };

            _context.PlatformCommissions.Add(newCommission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commission successfully set.", effectiveFrom = newCommission.EffectiveFrom });
        }

        // GET: /api/admin/commissions/{serviceType}/history
        [HttpGet("{serviceType}/history")]
        public async Task<IActionResult> GetCommissionHistory(string serviceType)
        {
            var validTypes = new[] { "Hotel", "Tour", "Airline" };
            if (!validTypes.Contains(serviceType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid service type.");
            }

            await AutoActivateCommissionsAsync();

            var now = DateTime.UtcNow;
            var formattedServiceType = validTypes.First(v => v.Equals(serviceType, StringComparison.OrdinalIgnoreCase));

            var history = await _context.PlatformCommissions
                .Include(c => c.CreatedByAdminUser)
                .Where(c => c.ServiceType == formattedServiceType)
                .OrderByDescending(c => c.EffectiveFrom)
                .ToListAsync();

            var dtos = history.Select(c => MapToDto(c, now)).ToList();

            return Ok(dtos);
        }

        // DELETE: /api/admin/commissions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommission(long id)
        {
            var commission = await _context.PlatformCommissions.FindAsync(id);
            if (commission == null)
            {
                return NotFound("Commission not found.");
            }

            if (commission.IsActive || commission.EffectiveFrom <= DateTime.UtcNow)
            {
                return BadRequest("Cannot delete an active or past commission. Only scheduled future commissions can be deleted.");
            }

            _context.PlatformCommissions.Remove(commission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Scheduled commission deleted successfully." });
        }
    }
}
