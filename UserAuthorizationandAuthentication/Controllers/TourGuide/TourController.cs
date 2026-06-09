using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.Tour;
using TravAi.TourGuide.Services;
using TravAi.TourGuide.DTOs.Review;

namespace TravAi.TourGuide.Controllers
{
    /// <summary>
    /// Tour Guide endpoints for managing their tours (CRUD operations)
    /// </summary>
    [Authorize(Roles = "Tourguide,Admin")]
    [Route("api/tourguide/tours")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class TourController : ControllerBase
    {
        private readonly ITourService _tourService;
        private readonly ITourGuideService _tourGuideService;

        public TourController(ITourService tourService, ITourGuideService tourGuideService)
        {
            _tourService = tourService;
            _tourGuideService = tourGuideService;
        }

        private long GetUserIdFromToken()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                return userId;

            return 0;
        }

        private async Task<long> GetTourGuideIdFromUserId(long userId)
        {
            var tourGuide = await _tourGuideService.GetTourGuideByUserIdAsync(userId);
            if (tourGuide != null && tourGuide.Status == "Active")
                return tourGuide.Id;
            return 0;
        }

        /// <summary>
        /// Create a new tour
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] CreateTourDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            try
            {
                var result = await _tourService.CreateTourAsync(tourGuideId, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing tour
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(long id, [FromBody] UpdateTourDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            var result = await _tourService.UpdateTourAsync(id, tourGuideId, model);
            if (result == null) return NotFound("Tour not found or you don't have permission to update it.");

            return Ok(result);
        }

        /// <summary>
        /// Delete a tour (Admin version)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> AdminDeleteTour(long id)
        {
            try
            {
                var success = await _tourService.AdminDeleteTourAsync(id);
                if (!success) return NotFound("Tour not found.");

                return Ok("Tour deleted successfully by Admin.");
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Repeat an existing tour to a new date
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("{id}/repeat")]
        public async Task<IActionResult> RepeatTour(long id, [FromBody] RepeatTourRequestDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newTour = await _tourService.RepeatTourAsync(id, tourGuideId, request);
                return Ok(new { id = newTour.Id, message = "Tour repeated successfully." });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete a tour
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(long id)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            try
            {
                var success = await _tourService.DeleteTourAsync(id, tourGuideId);
                if (!success) return NotFound("Tour not found or you don't have permission to delete it.");

                return Ok("Tour deleted successfully.");
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all tours by the authenticated tour guide
        /// </summary>
        [HttpGet("my-tours")]
        public async Task<IActionResult> GetMyTours()
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            var tours = await _tourService.GetToursByTourGuideAsync(tourGuideId);
            return Ok(tours);
        }

        /// <summary>
        /// Get a specific tour by ID (public endpoint for viewing)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTourById(long id)
        {
            var tour = await _tourService.GetTourByIdAsync(id);
            if (tour == null) return NotFound("Tour not found.");
            return Ok(tour);
        }

        /// <summary>
        /// Get tours with filters (public endpoint)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetToursWithFilters([FromQuery] TourFilterDto filters)
        {
            var (tours, totalCount) = await _tourService.GetToursWithFiltersAsync(filters);
            return Ok(new
            {
                tours,
                totalCount,
                pageNumber = filters.PageNumber,
                pageSize = filters.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            });
        }

        [AllowAnonymous]
        [HttpGet("/api/tours/cards")]
        public async Task<IActionResult> GetTourCards([FromQuery] TourFilterDto filters)
        {
            var (cards, totalCount) = await _tourService.GetTourCardsAsync(filters);
            return Ok(new
            {
                data = cards,
                totalCount = totalCount,
                totalPages = (int)System.Math.Ceiling(totalCount / (double)filters.PageSize),
                hasMore = (filters.PageNumber * filters.PageSize) < totalCount
            });
        }

        /// <summary>
        /// Get full tour details for the mobile app Tour Details screen.
        /// Returns a complete details object including imageUrls slider, guide info, included/notIncluded lists, etc.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/api/tours/{id:long}")]
        public async Task<IActionResult> GetTourDetails(long id)
        {
            var details = await _tourService.GetTourDetailsAsync(id);
            if (details == null) return NotFound(new { message = $"Tour with id {id} not found." });
            return Ok(details);
        }

        /// <summary>
        /// Get all reviews for a specific tour
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetTourReviews(long id)
        {
            var reviews = await _tourService.GetTourReviewsAsync(id);
            return Ok(reviews);
        }
    }
}

