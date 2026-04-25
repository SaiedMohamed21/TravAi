using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide;
using UserAuthorizationandAuthentication.TourGuide.Services;
using UserAuthorizationandAuthentication.TourGuide.DTOs.WithdrawRequest;

namespace UserAuthorizationandAuthentication.TourGuide.Controllers
{
    [Route("api/tourguide/profile")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class TourGuideController : ControllerBase
    {
        private readonly ITourGuideService _service;

        public TourGuideController(ITourGuideService service)
        {
            _service = service;
        }

        /// <summary>
        /// User applies to become a Tour Guide
        /// </summary>
        [Authorize(Roles = "User")]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] TourGuideApplicationDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                 return Unauthorized("User ID not found in token.");
            }

            try
            {
                var result = await _service.ApplyAsync(userId, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tour Guide updates their license photos and waits for admin approval
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("update-license")]
        public async Task<IActionResult> UpdateLicense([FromBody] UpdateLicenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.UpdateLicenseAsync(tourGuide.Id, model);
            if (!result) return BadRequest("Could not update license.");

            return Ok("License updated successfully. Status changed to Pending awaiting admin approval.");
        }

        /// <summary>
        /// Tour Guide updates their profile and waits for admin approval
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.UpdateProfileAsync(tourGuide.Id, model);
            if (!result) return BadRequest("Could not update profile.");

            return Ok("Profile updated successfully. Status changed to Pending awaiting admin approval.");
        }
        /// <summary>
        /// Tour Guide requests to withdraw money from their wallet/earnings
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawRequestDto model, [FromServices] IWithdrawRequestService withdrawRequestService)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            try
            {
                var result = await withdrawRequestService.CreateWithdrawRequestAsync(tourGuide.Id, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("not found", System.StringComparison.OrdinalIgnoreCase)) return NotFound(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tour Guide views their history of withdraw requests
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("withdraw")]
        public async Task<IActionResult> GetMyWithdrawalRequests([FromServices] IWithdrawRequestService withdrawRequestService)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await withdrawRequestService.GetMyRequestsAsync(tourGuide.Id);
            return Ok(result);
        }

        /// <summary>
        /// Get all reviews for a specific tour guide
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetTourGuideReviews(long id)
        {
            var reviews = await _service.GetTourGuideReviewsAsync(id);
            return Ok(reviews);
        }

        /// <summary>
        /// Get full profile information for a specific tour guide
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(long id)
        {
            var profile = await _service.GetProfileAsync(id);
            if (profile == null) return NotFound("Tour Guide profile not found.");
            return Ok(profile);
        }
    }
}

