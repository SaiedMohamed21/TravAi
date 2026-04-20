using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Services.HotelService;

namespace UserAuthorizationandAuthentication.Controllers
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
    }

    [ApiExplorerSettings(GroupName = "Hotel")]
    public class RejectRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}



