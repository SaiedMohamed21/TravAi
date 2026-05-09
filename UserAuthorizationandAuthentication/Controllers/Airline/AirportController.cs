using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Airport;
using TravAi.Airline.Services.AirportService;

namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/airports")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirportController : ControllerBase
    {
        private readonly IAirportService _airportService;

        public AirportController(IAirportService airportService)
        {
            _airportService = airportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var airports = await _airportService.GetAllAsync();
            return Ok(new ApiResponse<List<AirportDto>>(airports));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var airports = await _airportService.SearchAsync(q);
            return Ok(new ApiResponse<List<AirportDto>>(airports));
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var airport = await _airportService.GetByCodeAsync(code);
            if (airport == null)
                return NotFound(new ApiResponse<string>(false, "Airport not found."));

            return Ok(new ApiResponse<AirportDto>(airport));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAirportDto dto)
        {
            await _airportService.CreateAsync(dto);
            return Ok(new ApiResponse<string>("Airport created successfully."));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            await _airportService.DeleteAsync(code);
            return Ok(new ApiResponse<string>("Airport deleted successfully."));
        }
    }
}
