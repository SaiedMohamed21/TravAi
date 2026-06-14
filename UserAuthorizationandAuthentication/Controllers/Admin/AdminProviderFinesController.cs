using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Admin.Fines;
using TravAi.Models.Admin;
using TravAi.Services.Admin.Fines;

namespace TravAi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/fines")]
    [Authorize(Roles = "Admin")]
    public class AdminProviderFinesController : ControllerBase
    {
        private readonly IProviderFineService _fineService;

        public AdminProviderFinesController(IProviderFineService fineService)
        {
            _fineService = fineService;
        }

        private long GetAdminUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdStr, out long userId))
                return userId;
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        [HttpGet]
        public async Task<IActionResult> GetFines([FromQuery] ProviderFineFilterDto filter)
        {
            try
            {
                var fines = await _fineService.GetFinesAsync(filter);
                return Ok(new { success = true, data = fines });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFine(long id)
        {
            try
            {
                var fine = await _fineService.GetFineDetailsAsync(id);
                if (fine == null)
                    return NotFound(new { success = false, message = "Fine not found." });
                return Ok(new { success = true, data = fine });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFine([FromBody] CreateProviderFineDto dto)
        {
            try
            {
                var adminId = GetAdminUserId();
                var result = await _fineService.CreateFineAsync(dto, adminId);
                return Ok(new { success = true, data = result, message = "Fine created successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFine(long id, [FromBody] UpdateProviderFineDto dto)
        {
            try
            {
                var adminId = GetAdminUserId();
                var result = await _fineService.UpdateFineAsync(id, dto, adminId);
                return Ok(new { success = true, data = result, message = "Fine updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelFine(long id, [FromBody] CancelProviderFineDto dto)
        {
            try
            {
                var adminId = GetAdminUserId();
                await _fineService.CancelFineAsync(id, dto, adminId);
                return Ok(new { success = true, message = "Fine cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("eligible-complaints")]
        public async Task<IActionResult> GetEligibleComplaints([FromQuery] ProviderType type, [FromQuery] string? search)
        {
            try
            {
                var complaints = await _fineService.GetEligibleComplaintsAsync(type, search);
                return Ok(new { success = true, data = complaints });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("tour-guide-cancelled-bookings")]
        public async Task<IActionResult> GetTourGuideCancelledBookings([FromQuery] string? search)
        {
            try
            {
                var bookings = await _fineService.GetTourGuideCancelledBookingsAsync(search);
                return Ok(new { success = true, data = bookings });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders([FromQuery] ProviderType type, [FromQuery] string? search)
        {
            try
            {
                var providers = await _fineService.GetProvidersLookupAsync(type, search);
                return Ok(new { success = true, data = providers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-complaint/{complaintId}")]
        public async Task<IActionResult> GetFinesByComplaintId(long complaintId)
        {
            try
            {
                var fines = await _fineService.GetFinesByComplaintIdAsync(complaintId);
                return Ok(new { success = true, data = fines });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("tour-cancellations/{tourBookingId}")]
        public async Task<IActionResult> GetTourCancellationDetails(long tourBookingId)
        {
            try
            {
                var details = await _fineService.GetTourCancellationDetailsAsync(tourBookingId);
                if (details == null) return NotFound(new { success = false, message = "Tour booking cancellation not found." });
                return Ok(new { success = true, data = details });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
