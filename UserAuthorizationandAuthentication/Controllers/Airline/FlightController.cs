using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Flight;
using TravAi.Airline.Services.FlightService;

namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/flights")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class FlightController : ControllerBase
    {
        private readonly IFlightService _flightService;

        public FlightController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        // =========================
        // Create Flight (Admin)
        // =========================
        [Authorize(Roles = "Admin,Airline")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFlightDto dto)
        {
            return StatusCode(403, new ApiResponse<string>(
                success: false,
                message: "Flight creation via API is currently disabled. Flights are loaded from DB scripts."
            ));
        }

        // =========================
        // Update Flight (Admin)
        // =========================
        [Authorize(Roles = "Admin,Airline")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFlightDto dto)
        {
            await _flightService.UpdateAsync(id, dto);

            return Ok(new ApiResponse<string>(
                "Flight updated successfully"
            ));
        }

        // =========================
        // Cancel Flight (Admin)
        // =========================
        [Authorize(Roles = "Admin,Airline")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(long id)
        {
            await _flightService.CancelAsync(id);

            return Ok(new ApiResponse<string>(
                "Flight cancelled successfully"
            ));
        }

        // =========================
        // Search Flights
        // =========================
        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] FlightSearchDto dto)
        {
            var flights = await _flightService.SearchAsync(dto);

            return Ok(new ApiResponse<object>(
                flights
            ));
        }

        // =========================
        // Get Flight By Id
        // =========================
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            // Parse ID if it comes as "flight_123"
            long numericId;
            if (!long.TryParse(id, out numericId))
            {
                var idPart = id.Split('_').Last();
                if (!long.TryParse(idPart, out numericId))
                {
                    return BadRequest(new ApiResponse<string>(success: false, message: "Invalid flight ID format"));
                }
            }

            var flight = await _flightService.GetByIdAsync(numericId);
            
            if (flight is null)
            {
                return NotFound(new ApiResponse<string>(success: false, message: "Flight not found"));
            }

            return Ok(new ApiResponse<object>(flight));
        }
    }
}
