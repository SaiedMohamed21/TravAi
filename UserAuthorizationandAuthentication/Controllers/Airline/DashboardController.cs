using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;
using UserAuthorizationandAuthentication.Airline.Services.DashboardService;

namespace UserAuthorizationandAuthentication.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/dashboard")]
    [Authorize(Roles = "Admin,Airline")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(new ApiResponse<object>(stats));
        }
    }
}
