using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Companion;
using TravAi.Airline.Services.CompanionService;

namespace TravAi.Airline.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/airline/companions")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class CompanionController : ControllerBase
    {
        private readonly ICompanionService _companionService;

        public CompanionController(ICompanionService companionService)
        {
            _companionService = companionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCompanions()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = long.Parse(userIdStr);
            var companions = await _companionService.GetMyCompanionsAsync(userId);
            return Ok(new ApiResponse<List<UserCompanionDto>>(companions));
        }

        [HttpPost]
        public async Task<IActionResult> AddCompanion([FromBody] CreateCompanionDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = long.Parse(userIdStr);
            var companion = await _companionService.AddCompanionAsync(userId, dto);
            return Ok(new ApiResponse<UserCompanionDto>(companion, "Companion added successfully."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompanion(long id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = long.Parse(userIdStr);
            await _companionService.DeleteCompanionAsync(userId, id);
            return Ok(new ApiResponse<string>("Companion deleted successfully."));
        }
    }
}
