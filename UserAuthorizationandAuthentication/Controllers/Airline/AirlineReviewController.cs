using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Review;
using TravAi.Airline.Services.ReviewService;

namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/reviews")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirlineReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public AirlineReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // =============================
        // Add Review
        // =============================
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequestDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
           if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));

            if (!long.TryParse(userIdStr, out long userId))
                 return Unauthorized(new ApiResponse<string>(false, "Invalid User ID in token."));

            var review = await _reviewService.AddReviewAsync(userId, dto);

            return Ok(new ApiResponse<ReviewResponseDto>(
                review,
                "Review added successfully."
            ));
        }

        // =============================
        // Update Review
        // =============================
        [Authorize]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateReview(long id, [FromBody] UpdateReviewRequestDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));

            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Invalid User ID in token."));

            var review = await _reviewService.UpdateReviewAsync(userId, id, dto);

            return Ok(new ApiResponse<ReviewResponseDto>(
                review,
                "Review updated successfully."
            ));
        }

        // =============================
        // Delete Review
        // =============================
        [Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteReview(long id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));

            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Invalid User ID in token."));

            await _reviewService.DeleteReviewAsync(userId, id);

            return Ok(new ApiResponse<bool>(
                true,
                "Review deleted successfully."
            ));
        }

        // =============================
        // Get Flight Reviews
        // =============================
        [AllowAnonymous]
        [HttpGet("flight/{flightId:long}")]
        public async Task<IActionResult> GetFlightReviews(long flightId)
        {
            var reviews = await _reviewService.GetFlightReviewsAsync(flightId);

            return Ok(new ApiResponse<List<ReviewResponseDto>>(
                reviews,
                "Flight reviews retrieved successfully."
            ));
        }
    }
}
