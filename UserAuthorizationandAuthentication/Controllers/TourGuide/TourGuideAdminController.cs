using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.TourGuide;
using TravAi.TourGuide.Services;
using TravAi.TourGuide.DTOs.WithdrawRequest;

namespace TravAi.TourGuide.Controllers
{
    /// <summary>
    /// Admin-only endpoints for managing Tour Guide applications
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/tourguide/admin")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class TourGuideAdminController : ControllerBase
    {
        private readonly ITourGuideService _service;

        public TourGuideAdminController(ITourGuideService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all pending Tour Guide applications
        /// </summary>
        [HttpGet("applications")]
        public async Task<IActionResult> GetAllApplications()
        {
            var applications = await _service.GetAllApplicationsAsync();
            return Ok(applications);
        }

        /// <summary>
        /// Get specific Tour Guide application details by ID
        /// </summary>
        [HttpGet("applications/{id}")]
        public async Task<IActionResult> GetApplicationById(long id)
        {
            var application = await _service.GetApplicationByIdAsync(id);
            if (application == null) return NotFound("Application not found.");
            return Ok(application);
        }

        /// <summary>
        /// Approve Tour Guide application - promotes user to TourGuide role
        /// </summary>
        [HttpPost("applications/{id}/approve")]
        public async Task<IActionResult> ApproveApplication(long id)
        {
            var success = await _service.ApproveApplicationAsync(id);
            if (!success) return NotFound("Application not found.");
            return Ok("Application approved. User is now a TourGuide.");
        }

        /// <summary>
        /// Reject Tour Guide application - sends a rejection reason
        /// </summary>
        [HttpPost("applications/{id}/reject")]
        public async Task<IActionResult> RejectApplication(long id, [FromBody] RejectApplicationDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _service.RejectApplicationAsync(id, model.Reason);
            if (!success) return NotFound("Application not found.");
            return Ok("Application rejected.");
        }

        /// <summary>
        /// Ban an active Tour Guide
        /// </summary>
        [HttpPost("{id}/ban")]
        public async Task<IActionResult> BanTourGuide(long id)
        {
            var success = await _service.BanTourGuideAsync(id);
            if (!success) return NotFound("Tour Guide not found.");
            return Ok("Tour Guide banned successfully.");
        }

        /// <summary>
        /// Suspend an active Tour Guide
        /// </summary>
        [HttpPost("{id}/suspend")]
        public async Task<IActionResult> SuspendTourGuide(long id, [FromBody] SuspendTourGuideDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _service.SuspendTourGuideAsync(id, model);
            if (!success) return NotFound("Tour Guide not found.");
            
            return Ok($"Tour Guide suspended successfully for {model.Duration} {model.Unit}.");
        }

        /// <summary>
        /// Get all pending withdraw requests
        /// </summary>
        [HttpGet("withdraw-requests/pending")]
        public async Task<IActionResult> GetPendingWithdrawRequests([FromServices] IWithdrawRequestService withdrawRequestService)
        {
            var requests = await withdrawRequestService.GetAllPendingRequestsAsync();
            return Ok(requests);
        }

        /// <summary>
        /// Approve or reject a withdraw request
        /// </summary>
        [HttpPost("withdraw-requests/{requestId}/process")]
        public async Task<IActionResult> ProcessWithdrawRequest(long requestId, [FromBody] ProcessWithdrawRequestDto model, [FromServices] IWithdrawRequestService withdrawRequestService)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await withdrawRequestService.ProcessRequestAsync(requestId, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("not found", System.StringComparison.OrdinalIgnoreCase)) return NotFound(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}

