using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.Airline.DTOs.Passenger;
using UserAuthorizationandAuthentication.Airline.Services.PassengerService;

namespace UserAuthorizationandAuthentication.Airline.Controllers
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
            var passenger = await _passengerService.CreateAsync(dto);

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
            var passenger = await _passengerService.GetByIdAsync(id);

            if (passenger == null)
            {
                return NotFound(new ApiResponse<string>(
                    success: false,
                    message: "Passenger not found."
                ));
            }

            return Ok(new ApiResponse<PassengerResponseDto>(
                passenger,
                "Retrieved passenger successfully."
            ));
        }

        // =========================
        // Update Passenger
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePassengerDto dto)
        {
            await _passengerService.UpdateAsync(id, dto);

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
            await _passengerService.DeleteAsync(id);

            return Ok(new ApiResponse<string>(
                "Passenger deleted successfully."
            ));
        }
    }
}
