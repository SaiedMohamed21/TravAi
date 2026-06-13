using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.TourGuide;
using TravAi.TourGuide.Services;
using TravAi.TourGuide.DTOs.WithdrawRequest;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using TravAi.TourGuide.Models.Enums;

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
        /// Get all Tour Guides for Admin Management Dashboard
        /// </summary>
        [HttpGet("management")]
        public async Task<IActionResult> GetGuideManagementList()
        {
            var guides = await _service.GetGuideManagementListAsync();
            return Ok(guides);
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

        [HttpGet("/api/admin/tour-guide-cancellations")]
        public async Task<IActionResult> GetAllCancellationsForAdmin([FromQuery] UrgentRequestStatus? status, [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var query = context.UrgentRequests
                .Include(r => r.TourGuide)
                .Include(r => r.Tour)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var requests = await query
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<object>();
            foreach (var r in requests)
            {
                var affectedCount = await context.TourBookings
                    .CountAsync(b => b.TourId == r.TourId && b.Status == BookingStatus.PendingUserDecision);

                result.Add(new
                {
                    Id = r.Id,
                    TourGuideName = r.TourGuide?.Name ?? "Unknown Guide",
                    TourName = r.Tour?.TourTitle ?? "Unknown Tour",
                    Destination = r.Tour?.City ?? "Unknown Destination",
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                    AffectedBookingsCount = affectedCount,
                    Status = r.Status.ToString()
                });
            }

            return Ok(result);
        }

        [HttpGet("/api/admin/tour-guide-cancellations/pending-review")]
        public async Task<IActionResult> GetPendingCancellationsForAdmin([FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var requests = await context.UrgentRequests
                .Include(r => r.TourGuide)
                .Include(r => r.Tour)
                .Where(r => r.Status == UrgentRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<object>();
            foreach (var r in requests)
            {
                var affectedCount = await context.TourBookings
                    .CountAsync(b => b.TourId == r.TourId && b.Status == BookingStatus.PendingUserDecision);

                result.Add(new
                {
                    Id = r.Id,
                    TourGuideName = r.TourGuide?.Name ?? "Unknown Guide",
                    TourName = r.Tour?.TourTitle ?? "Unknown Tour",
                    Destination = r.Tour?.City ?? "Unknown Destination",
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                    AffectedBookingsCount = affectedCount,
                    Status = r.Status.ToString()
                });
            }

            return Ok(result);
        }

        [HttpPost("/api/admin/tour-guide-cancellations/{id}/review")]
        public async Task<IActionResult> ReviewCancellation(long id, [FromBody] AdminReviewCancellationDto dto, [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var request = await context.UrgentRequests.FindAsync(id);
            if (request == null) return NotFound("Cancellation request not found.");

            request.Status = dto.IsReasonAccepted ? UrgentRequestStatus.Approved : UrgentRequestStatus.Rejected;
            request.AdminNotes = dto.AdminNotes;
            request.ProcessedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return Ok(new { Message = "Review submitted successfully." });
        }

        [HttpGet("tours")]
        public async Task<IActionResult> GetAdminToursList([FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var toursList = await context.Tours
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TourTitle,
                    t.City,
                    t.BasePriceUsd,
                    t.DurationHours,
                    t.Active,
                    GuideName = t.TourGuide != null ? t.TourGuide.Name : "Unknown",
                    PrimaryImage = t.TourImages.Where(img => img.IsPrimary).OrderBy(img => img.SortOrder).Select(img => img.ImageUrl).FirstOrDefault() 
                                   ?? t.TourImages.OrderBy(img => img.SortOrder).Select(img => img.ImageUrl).FirstOrDefault(),
                    BookingsCount = context.TourBookings.Count(b => b.TourId == t.Id)
                })
                .ToListAsync();

            var result = toursList.Select(t => new
            {
                Id = t.Id,
                Image = t.PrimaryImage ?? "",
                Name = t.TourTitle,
                Location = t.City ?? "N/A",
                Price = t.BasePriceUsd ?? 0,
                Duration = t.DurationHours.HasValue ? $"{t.DurationHours} hours" : "N/A",
                Guide = t.GuideName,
                Status = t.Active ? "active" : "pending",
                Bookings = t.BookingsCount
            }).ToList();

            return Ok(result);
        }

        [HttpGet("tours/pending")]
        public async Task<IActionResult> GetAdminPendingTours([FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var pendingTours = await context.Tours
                .Where(t => !t.Active)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TourTitle,
                    t.City,
                    t.BasePriceUsd,
                    t.CreatedAt,
                    GuideName = t.TourGuide != null ? t.TourGuide.Name : "Unknown",
                    PrimaryImage = t.TourImages.Where(img => img.IsPrimary).OrderBy(img => img.SortOrder).Select(img => img.ImageUrl).FirstOrDefault() 
                                   ?? t.TourImages.OrderBy(img => img.SortOrder).Select(img => img.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

            var result = pendingTours.Select(t => new
            {
                Id = t.Id,
                Image = t.PrimaryImage ?? "",
                TourName = t.TourTitle,
                Guide = t.GuideName,
                Location = t.City ?? "N/A",
                Price = t.BasePriceUsd ?? 0,
                SubmittedDate = t.CreatedAt.ToString("M/d/yyyy"),
                Status = "pending"
            }).ToList();

            return Ok(result);
        }

        [HttpPost("tours/{id}/approve")]
        public async Task<IActionResult> ApproveTour(long id, [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var tour = await context.Tours.FindAsync(id);
            if (tour == null) return NotFound("Tour not found.");

            tour.Active = true;
            tour.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Ok(new { Message = "Tour approved successfully." });
        }

        [HttpPost("tours/{id}/reject")]
        public async Task<IActionResult> RejectTour(long id, [FromBody] RejectApplicationDto? model, [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var tour = await context.Tours.FindAsync(id);
            if (tour == null) return NotFound("Tour not found.");

            tour.Active = false;
            tour.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Ok(new { Message = "Tour rejected and deactivated successfully." });
        }

        [HttpDelete("bookings/{bookingId}")]
        public async Task<IActionResult> AdminDeleteBooking(long bookingId, [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            var booking = await context.TourBookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Booking not found.");

            booking.Status = BookingStatus.Cancelled;
            booking.PaymentStatus = PaymentStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Ok(new { Message = "Booking cancelled successfully." });
        }
    }

    public class AdminReviewCancellationDto
    {
        public bool IsReasonAccepted { get; set; }
        public string? AdminNotes { get; set; }
    }
}

