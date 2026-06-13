using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs;
using TravAi.DTOs.Common;
using TravAi.DTOs.Hotel;
using TravAi.Services.HotelService;

namespace TravAi.Controllers
{
    [ApiExplorerSettings(GroupName = "Common")]
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/user/complaints")]
    public class UserComplaintsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public UserComplaintsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        private long GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdStr, out long id) ? id : 0;
        }



        [HttpGet("bookings")]
        public async Task<IActionResult> GetEligibleBookings([FromQuery] string? type)
        {
            var complaintType = TravAi.Models.Enums.ComplaintType.Hotel;
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<TravAi.Models.Enums.ComplaintType>(type, true, out var parsedType))
            {
                complaintType = parsedType;
            }

            var bookings = await _hotelService.GetEligibleBookingsForComplaintAsync(GetUserId(), complaintType);
            return Ok(new ApiResponse<List<UserBookingMinimalDto>> { Success = true, Data = bookings });
        }

        [HttpPost]
        public async Task<IActionResult> CreateComplaint([FromForm] ComplaintCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                var complaintId = await _hotelService.CreateComplaintAsync(userId, dto);
                return Ok(new ApiResponse<long> { Success = true, Data = complaintId, Message = "Complaint submitted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    msg += " -> " + inner.Message;
                    inner = inner.InnerException;
                }
                return BadRequest(new ApiResponse<string> { Success = false, Message = msg });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComplaint(long id, [FromForm] ComplaintCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _hotelService.UpdateComplaintAsync(userId, id, dto);
                return Ok(new ApiResponse<bool> { Success = true, Data = result, Message = "Complaint updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyComplaints()
        {
            var complaints = await _hotelService.GetMyComplaintsAsync(GetUserId());
            return Ok(new ApiResponse<List<ComplaintSummaryDto>> { Success = true, Data = complaints });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaintDetails(long id)
        {
            try
            {
                var details = await _hotelService.GetComplaintDetailsAsync(GetUserId(), id);
                return Ok(new ApiResponse<ComplaintDetailsDto> { Success = true, Data = details });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(long id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _hotelService.DeleteComplaintAsync(userId, id);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Complaint deleted successfully." : "Failed to delete complaint." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToComplaint(long id, [FromForm] AdminReplyCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _hotelService.UserReplyToComplaintAsync(userId, id, dto);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Reply sent successfully." : "Failed to send reply." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

    }
}
