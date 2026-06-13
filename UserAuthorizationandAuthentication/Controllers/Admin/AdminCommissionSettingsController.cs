using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.DTOs;
using TravAi.DTOs.Hotel;
using TravAi.Services.HotelService;
using TravAi.DTOs.Common;

namespace TravAi.Controllers.Admin
{
    [ApiExplorerSettings(GroupName = "Hotel")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/commission-settings")]
    public class AdminCommissionSettingsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public AdminCommissionSettingsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var result = await _hotelService.GetCurrentCommissionSettingAsync();
            return Ok(new ApiResponse<CommissionSettingDto?> { Success = true, Data = result });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var result = await _hotelService.GetCommissionSettingsHistoryAsync();
            return Ok(new ApiResponse<List<CommissionSettingDto>> { Success = true, Data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] CreateCommissionSettingRequest request)
        {
            var adminUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserIdStr) || !long.TryParse(adminUserIdStr, out long adminUserId))
            {
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid admin token" });
            }

            var result = await _hotelService.SaveCommissionSettingAsync(adminUserId, request);
            return Ok(new ApiResponse<CommissionSettingDto> { Success = true, Message = "New commission rule created and set as active", Data = result });
        }
    }
}
