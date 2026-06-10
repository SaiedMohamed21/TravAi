using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Passenger;
using TravAi.Airline.Services.PassengerService;

namespace TravAi.Airline.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/airline/passengers")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class PassengerController : ControllerBase
    {
        private readonly IPassengerService _passengerService;

        public PassengerController(IPassengerService passengerService)
        {
            _passengerService = passengerService;
        }

        // =========================
        // Create Passenger
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePassengerDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Unauthorized user."));

            var passenger = await _passengerService.CreateAsync(dto, userId);

            return Ok(new ApiResponse<PassengerResponseDto>(
                passenger,
                "Passenger added successfully."
            ));
        }

        // =========================
        // Get Booking Passengers
        // =========================
        [HttpGet("booking/{bookingId}")]
        public async Task<IActionResult> GetBookingPassengers(long bookingId)
        {
            var passengers = await _passengerService.GetBookingPassengersAsync(bookingId);

            return Ok(new ApiResponse<List<PassengerResponseDto>>(
                passengers,
                "Retrieved booking passengers successfully."
            ));
        }

        // =========================
        // Get Passenger By Id
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var passengerDto = await _passengerService.GetByIdAsync(id);

            if (passengerDto == null)
            {
                return NotFound(new ApiResponse<string>(
                    success: false,
                    message: "Passenger not found."
                ));
            }

            return Ok(new ApiResponse<PassengerResponseDto>(
                passengerDto,
                "Retrieved passenger successfully."
            ));
        }

        // =========================
        // Update Passenger
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePassengerDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Unauthorized user."));

            await _passengerService.UpdateAsync(id, dto, userId);

            return Ok(new ApiResponse<string>(
                "Passenger updated successfully."
            ));
        }

        // =========================
        // Delete Passenger
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Unauthorized user."));

            await _passengerService.DeleteAsync(id, userId);

            return Ok(new ApiResponse<string>(
                "Passenger deleted successfully."
            ));
        }
    }
}
