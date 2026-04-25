using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;
using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Services.HotelService;

namespace UserAuthorizationandAuthentication.Controllers.Hotel
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // --- Management Section (Owner) ---

        [HttpPost("apply")]
        [Authorize]
        public async Task<IActionResult> ApplyAsHotel([FromForm] HotelApplicationRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                await _hotelService.ApplyAsHotelAsync(userId, request);
                return Ok(new ApiResponse<string>("Application submitted successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("my-application")]
        [Authorize]
        public async Task<IActionResult> GetMyApplicationStatus()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                var status = await _hotelService.GetMyApplicationStatusAsync(userId);
                return Ok(new ApiResponse<HotelDetailsDto>
                {
                    Success = true,
                    Message = status != null ? "Application status retrieved." : "No pending application.",
                    Data = status,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpDelete("my-application")]
        [Authorize]
        public async Task<IActionResult> DeleteMyApplication()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                var success = await _hotelService.DeleteMyApplicationAsync(userId);
                if (!success) return NotFound(new ApiResponse<string>(false, "No pending application found.", null));

                return Ok(new ApiResponse<string>("Application deleted successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("my")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> GetMyHotel()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                var hotel = await _hotelService.GetMyHotelProfileAsync(userId);
                return Ok(new ApiResponse<HotelDetailsDto>
                {
                    Success = true,
                    Message = "Hotel profile retrieved.",
                    Data = hotel,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdateHotel(long id, [FromBody] UpdateHotelRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                var success = await _hotelService.UpdateHotelAsync(userId, id, request);
                if (!success) return NotFound(new ApiResponse<string>(false, "Hotel not found or permission denied.", null));

                return Ok(new ApiResponse<string>("Hotel updated successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        [HttpGet("{id}/amenities")]
        public async Task<IActionResult> GetAmenities(long id)
        {
            var amenities = await _hotelService.GetAmenitiesAsync(id);
            return Ok(new ApiResponse<HotelAmenitiesDto>
            {
                Success = true,
                Message = "Amenities retrieved.",
                Data = amenities,
                Errors = new List<string>()
            });
        }

        [HttpPut("{id}/amenities")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdateAmenities(long id, [FromBody] UpdateAmenitiesRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Unauthorized(new ApiResponse<string>(false, "Unauthorized.", null));

                var success = await _hotelService.UpdateAmenitiesAsync(userId, id, request);
                if (!success) return NotFound(new ApiResponse<string>(false, "Hotel not found or permission denied.", null));

                return Ok(new ApiResponse<string>("Amenities updated successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }
    }
}



