using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravAi.DTOs;
using TravAi.DTOs.Common;
using TravAi.DTOs.Hotel;
using TravAi.Models;
using TravAi.Services.HotelService;

namespace TravAi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/complaints")]
    [ApiController]
    public class AdminComplaintsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public AdminComplaintsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetComplaints([FromQuery] string? type, [FromQuery] string? status, [FromQuery] string? search, [FromQuery] long? bookingId, [FromQuery] string? hotelName)
        {
            try
            {
                var list = await _hotelService.GetAdminComplaintsAsync(type, status, search, bookingId, hotelName);
                return Ok(new ApiResponse<List<AdminComplaintSummaryDto>> { Success = true, Data = list });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaintDetails(long id)
        {
            try
            {
                var details = await _hotelService.GetAdminComplaintDetailsAsync(id);
                return Ok(new ApiResponse<AdminComplaintDetailsDto> { Success = true, Data = details });
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

        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToComplaint(long id, [FromForm] AdminReplyCreateDto dto)
        {
            try
            {
                var adminUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _hotelService.AdminReplyToComplaintAsync(adminUserId, id, dto);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Reply sent successfully." : "Failed to send reply." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> ResolveComplaint(long id)
        {
            try
            {
                var result = await _hotelService.ResolveComplaintAsync(id);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Complaint resolved successfully." : "Failed to resolve complaint." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }
        [HttpDelete("replies/{replyId}")]
        public async Task<IActionResult> DeleteReply(long replyId)
        {
            try
            {
                var adminUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _hotelService.DeleteAdminReplyAsync(adminUserId, replyId);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Reply deleted." : "Failed to delete reply." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("replies/{replyId}")]
        public async Task<IActionResult> EditReply(long replyId, [FromForm] AdminReplyEditDto dto)
        {
            try
            {
                var adminUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _hotelService.EditAdminReplyAsync(adminUserId, replyId, dto);
                return Ok(new ApiResponse<bool> { Success = result, Message = result ? "Reply updated." : "Failed to update reply." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message });
            }
        }
    }
}
