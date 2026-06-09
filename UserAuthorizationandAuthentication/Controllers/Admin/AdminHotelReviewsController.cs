using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs;
using TravAi.DTOs.Hotel;
using TravAi.Services.HotelService;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.DTOs.Common;

namespace TravAi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/hotel-reviews")]
    public class AdminHotelReviewsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public AdminHotelReviewsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? userName = null, [FromQuery] string? hotelName = null, [FromQuery] string? keyword = null, [FromQuery] int? rating = null)
        {
            var result = await _hotelService.GetAdminReviewsAsync(pageNumber, pageSize, userName, hotelName, keyword, rating);
            return Ok(new ApiResponse<PaginatedAdminReviewsResponse> { Success = true, Data = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _hotelService.DeleteReviewAsync(id);
            if (!success) return NotFound(new ApiResponse<string> { Success = false, Message = "Review not found" });
            return Ok(new ApiResponse<string> { Success = true, Message = "Review deleted" });
        }
    }
}
