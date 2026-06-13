using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.Services.HotelService;
using TravAi.DTOs.Hotel;
using TravAi.Models.Enums;
using TravAi.DTOs;
using TravAi.DTOs.Common;

namespace TravAi.Controllers.Hotel
{
    [ApiExplorerSettings(GroupName = "Hotel")]
    [Authorize(Roles = "Hotel,Admin")]
    [ApiController]
    [Route("api/hotel/dashboard")]
    public class HotelDashboardController : ControllerBase
    {
        private readonly IHotelDashboardService _dashboardService;

        public HotelDashboardController(IHotelDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] long? hotelId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                var role = User.FindFirstValue(ClaimTypes.Role);
                long? targetHotelId = (role == "Admin") ? hotelId : null;

                var overview = await _dashboardService.GetOverviewAsync(userId, targetHotelId);
                return Ok(overview);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("bookings")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookings([FromQuery] long debugUserId, [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? bedType, [FromQuery] string? guestName, [FromQuery] string? bookingId, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] long? hotelId)
        {
            try
            {
                long userId = debugUserId; // Use passed userId for debug if anonymous
                if (userId == 0)
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!long.TryParse(userIdStr, out userId)) return Unauthorized();
                }

                var role = User.FindFirstValue(ClaimTypes.Role);
                long? targetHotelId = (role == "Admin") ? hotelId : null;
                
                var bookings = await _dashboardService.GetDashboardBookingsAsync(userId, status, from, to, bedType, guestName, bookingId, checkIn, checkOut, targetHotelId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("rooms/update-all")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdateAllRooms([FromBody] UpdateRoomsRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                await _dashboardService.UpdateRoomConfigAsync(userId, request.Rooms, request.DeletedIds);
                return Ok(new { message = "Room configuration updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews([FromQuery] string? datePreset, [FromQuery] int? starRating, [FromQuery] int page = 1, [FromQuery] DateTime? startDate = null, [FromQuery] long? hotelId = null)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                var role = User.FindFirstValue(ClaimTypes.Role);
                long? targetHotelId = (role == "Admin") ? hotelId : null;

                var reviews = await _dashboardService.GetHotelReviewsAsync(userId, datePreset, starRating, page, startDate, targetHotelId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("financials")]
        public async Task<IActionResult> GetFinancials([FromQuery] int? year, [FromQuery] long? hotelId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                int targetYear = year ?? DateTime.UtcNow.Year;
                var role = User.FindFirstValue(ClaimTypes.Role);
                long? targetHotelId = (role == "Admin") ? hotelId : null;

                var data = await _dashboardService.GetFinancialsAsync(userId, targetYear, targetHotelId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("inbox/dashboard")]
        public async Task<IActionResult> GetInboxDashboard()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                var data = await _dashboardService.GetInboxDashboardAsync(userId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("inbox/category/{category}")]
        public async Task<IActionResult> GetInboxCategory(InboxCategory category, [FromQuery] int page = 1)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                var data = await _dashboardService.GetInboxCategoryPagedAsync(userId, category, page, 20);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("inbox/{id}")]
        public async Task<IActionResult> GetInboxDetails(long id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                var data = await _dashboardService.GetInboxMessageDetailsAsync(userId, id);
                if (data == null) return NotFound();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("inbox/{id}/reply")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> ReplyToMessage(long id, [FromBody] HotelInboxReplyRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                await _dashboardService.ReplyToInboxMessageAsync(userId, id, request.Message);
                return Ok(new { message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("inbox/send-to-admin")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> SendToAdmin([FromBody] HotelInboxComposeRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

                await _dashboardService.SendMessageToAdminAsync(userId, request.Subject, request.Message, request.Category);
                return Ok(new { message = "Message sent to admin successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("inbox/{id}/mark-as-read")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            await _dashboardService.MarkInboxMessageAsReadAsync(userId, id);
            return Ok();
        }

        [HttpPost("inbox/{id}/resolve")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> Resolve(long id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            await _dashboardService.ResolveInboxMessageAsync(userId, id);
            return Ok();
        }

        // --- Configuration Section (Split Approval Workflow) ---

        [HttpPost("config/profile")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdateProfile([FromForm] HotelProfileUpdateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

                await _dashboardService.SubmitProfileUpdateAsync(userId, request);
                return Ok(new { message = "Profile update submitted for admin approval." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("config/policy")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdatePolicy([FromBody] HotelPolicyUpdateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

                await _dashboardService.SubmitPolicyUpdateAsync(userId, request);
                return Ok(new { message = "Policy update submitted for admin approval." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("config/legal")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> UpdateLegal([FromForm] HotelLegalUpdateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

                await _dashboardService.SubmitLegalUpdateAsync(userId, request);
                return Ok(new { message = "Legal documents update submitted for admin approval." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("config/pending")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

                var requests = await _dashboardService.GetPendingConfigSectionsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("config/pending-details")]
        [Authorize(Roles = "Hotel")]
        public async Task<IActionResult> GetPendingConfigDetails()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

                var dto = await _dashboardService.GetPendingApplicationAsync(userId);
                return Ok(new ApiResponse<HotelDetailsDto> { Success = true, Data = dto });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class UpdateRoomsRequest
    {
        public List<RoomTypeSummaryDto> Rooms { get; set; } = new();
        public List<long> DeletedIds { get; set; } = new();
    }
}
