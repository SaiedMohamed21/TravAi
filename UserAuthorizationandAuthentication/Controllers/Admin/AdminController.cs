using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.DTOs.Hotel;
using TravAi.Services.HotelService;
using TravAi.DTOs.AdminManagement;
using TravAi.Models.Enums;

namespace TravAi.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class AdminController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public AdminController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var apps = await _hotelService.GetAllApplicationsAsync();
                return Ok(new ApiResponse<List<HotelDetailsDto>>(apps));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("applications/pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                var apps = await _hotelService.GetPendingApplicationsAsync();
                return Ok(new ApiResponse<List<HotelDetailsDto>>(apps));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpPost("applications/{id}/approve")]
        public async Task<IActionResult> ApproveApplication(long id)
        {
            try
            {
                var success = await _hotelService.ApproveApplicationAsync(id);
                if (!success) return NotFound(new ApiResponse<string>(false, "Application not found.", null));
                
                return Ok(new ApiResponse<string>("Application approved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpPost("applications/{id}/reject")]
        public async Task<IActionResult> RejectApplication(long id, [FromBody] RejectRequest request)
        {
            try
            {
                var success = await _hotelService.RejectApplicationAsync(id, request.Reason);
                if (!success) return NotFound(new ApiResponse<string>(false, "Application not found.", null));

                return Ok(new ApiResponse<string>("Application rejected."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("dashboard/kpis")]
        public async Task<IActionResult> GetDashboardKpis()
        {
            try
            {
                var stats = await _hotelService.GetAdminDashboardKpisAsync();
                return Ok(new ApiResponse<AdminDashboardKpiDto>(stats, "Dashboard KPIs retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("dashboard/charts")]
        public async Task<IActionResult> GetDashboardCharts([FromQuery] int? year)
        {
            try
            {
                var charts = await _hotelService.GetAdminChartDataAsync(year);
                return Ok(new ApiResponse<AdminChartDataDto>(charts, "Dashboard charts data retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("hotels/management")]
        public async Task<IActionResult> GetHotelManagementList([FromQuery] string? searchQuery, [FromQuery] VerificationStatus? status, [FromQuery] string? city)
        {
            try
            {
                var hotels = await _hotelService.GetHotelManagementListAsync(searchQuery, status, city);
                return Ok(new ApiResponse<List<HotelManageSummaryDto>>(hotels));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpPost("hotels/{id}/suspend")]
        public async Task<IActionResult> SuspendHotel(long id)
        {
            try
            {
                var success = await _hotelService.SuspendHotelAsync(id);
                if (!success) return NotFound(new ApiResponse<string>(false, "Hotel not found"));
                return Ok(new ApiResponse<string>("Hotel suspended successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpPost("hotels/{id}/ban")]
        public async Task<IActionResult> BanHotel(long id)
        {
            try
            {
                var success = await _hotelService.BanHotelAsync(id);
                if (!success) return NotFound(new ApiResponse<string>(false, "Hotel not found"));
                return Ok(new ApiResponse<string>("Hotel banned successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }
        
        [HttpPost("hotels/{id}/approve")]
        public async Task<IActionResult> ApproveHotel(long id)
        {
            try
            {
                var success = await _hotelService.ApproveHotelAsync(id);
                if (!success) return NotFound(new ApiResponse<string>(false, "Hotel not found"));
                return Ok(new ApiResponse<string>("Hotel approved correctly and restored to operational state."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetAdminBookings([FromQuery] AdminBookingSearchRequest request)
        {
            try
            {
                var result = await _hotelService.GetAdminBookingsAsync(request);
                return Ok(new ApiResponse<AdminBookingPaginationResponse>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }
    }

    [ApiExplorerSettings(GroupName = "Hotel")]
    public class RejectRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}



