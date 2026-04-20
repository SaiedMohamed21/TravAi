using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Services.HotelService;

namespace UserAuthorizationandAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class PublicHotelsController : ControllerBase
    {
        private readonly IHotelService _hotelService; // Dummy comment for hot reload

        public PublicHotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // GET: api/PublicHotels
        [HttpGet]
        public async Task<IActionResult> SearchHotels([FromQuery] HotelSearchRequest request)
        {
            try
            {
                var result = await _hotelService.SearchHotelsAsync(request);
                return Ok(new ApiResponse<HotelSearchResponse>
                {
                    Success = true,
                    Message = $"Found {result.TotalCount} hotels.",
                    Data = result,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/PublicHotels/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHotelDetails(long id, [FromQuery] DateTime? checkIn = null, [FromQuery] DateTime? checkOut = null)
        {
            try
            {
                var hotel = await _hotelService.GetHotelDetailsAsync(id, checkIn, checkOut);
                return Ok(new ApiResponse<HotelDetailsDto>
                {
                    Success = true,
                    Message = "Hotel details retrieved.",
                    Data = hotel,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/PublicHotels/reviews
        [HttpPost("reviews")]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewRequest request)
        {
            try
            {
                // Extract User ID from JWT token (ClaimTypes.NameIdentifier)
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token.");
                
                var userId = long.Parse(userIdClaim);
                var result = await _hotelService.AddReviewAsync(userId, request);
                
                return Ok(new ApiResponse<HotelReviewDto>
                {
                    Success = true,
                    Message = "Review added successfully.",
                    Data = result,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        
        // PUT: api/PublicHotels/reviews/{id}
        [HttpPut("reviews/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(long id, [FromBody] UpdateReviewRequest request)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token.");
                var userId = long.Parse(userIdClaim);

                var result = await _hotelService.UpdateReviewAsync(userId, id, request);
                return Ok(new ApiResponse<HotelReviewDto>
                {
                    Success = true,
                    Message = "Review updated successfully.",
                    Data = result,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/PublicHotels/reviews/{id}
        [HttpDelete("reviews/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(long id)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token.");
                var userId = long.Parse(userIdClaim);

                await _hotelService.DeleteReviewAsync(userId, id);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Review deleted successfully.",
                    Data = true,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/PublicHotels/{id}/reviews
        [HttpGet("{id}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHotelReviews(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool isRandom = false)
        {
            try
            {
                var result = await _hotelService.GetHotelReviewsAsync(id, page, pageSize, isRandom);
                return Ok(new ApiResponse<HotelReviewsResponse>
                {
                    Success = true,
                    Message = "Reviews retrieved successfully.",
                    Data = result,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}



