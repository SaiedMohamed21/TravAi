using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.UrgentRequest;
using TravAi.TourGuide.Services;

namespace TravAi.TourGuide.Controllers
{
    [Route("api/tourguide/urgent-requests")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class UrgentRequestController : ControllerBase
    {
        private readonly IUrgentRequestService _urgentRequestService;
        private readonly ITourGuideService _tourGuideService;

        public UrgentRequestController(IUrgentRequestService urgentRequestService, ITourGuideService tourGuideService)
        {
            _urgentRequestService = urgentRequestService;
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
        /// (Tour Guide) Submit an urgent request for a tour
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitUrgentRequest([FromBody] CreateUrgentRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            try
            {
                var result = await _urgentRequestService.CreateUrgentRequestAsync(tourGuideId, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// (Tour Guide) Get all my urgent requests
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var tourGuideId = await GetTourGuideIdFromUserId(userId);
            if (tourGuideId == 0) return Unauthorized("Tour Guide profile not found or not verified.");

            var requests = await _urgentRequestService.GetMyRequestsAsync(tourGuideId);
            return Ok(requests);
        }

        /// <summary>
        /// (Admin) Get all pending urgent requests
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _urgentRequestService.GetAllPendingRequestsAsync();
            return Ok(requests);
        }

        /// <summary>
        /// (Admin) Approve or reject an urgent request
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/process/{id}")]
        public async Task<IActionResult> ProcessRequest(long id, [FromBody] AdminProcessUrgentRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _urgentRequestService.ProcessRequestAsync(id, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

