using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Booking;
using TravAi.Airline.Services.BookingService;
using TravAi.Airline.Services.PassengerService;
using TravAi.Airline.DTOs.Passenger;

namespace TravAi.Airline.Controllers
{
    public class CancelBookingRefundRequest
    {
        public string? RefundMethod { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/airline/bookings")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirlineBookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPassengerService _passengerService;

        public AirlineBookingController(IBookingService bookingService, IPassengerService passengerService)
        {
            _bookingService = bookingService;
            _passengerService = passengerService;
        }

        // =============================
        // Book a Flight
        // =============================
        [HttpPost]
        public async Task<IActionResult> BookFlight([FromBody] BookingRequestDto dto)
        {
            // Extract User ID from JWT Claims
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));

            if (!long.TryParse(userIdStr, out long userId))
                 return Unauthorized(new ApiResponse<string>(false, "Invalid User ID in token."));

            var booking = await _bookingService.BookFlightAsync(userId, dto);

            return Ok(new ApiResponse<BookingResponseDto>(
                booking,
                "Flight reserved. Please complete passenger details before payment."
            ));
        }

        // =============================
        // Get My Bookings
        // =============================
        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
           if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));

            if (!long.TryParse(userIdStr, out long userId))
                 return Unauthorized(new ApiResponse<string>(false, "Invalid User ID in token."));

            var bookings = await _bookingService.GetUserBookingsAsync(userId);

            return Ok(new ApiResponse<List<BookingResponseDto>>(
                bookings,
                "Retrieved user bookings successfully."
            ));
        }

        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips([FromQuery] string tab)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Invalid User."));

            var trips = await _bookingService.GetUserTripsAsync(userId, tab);
            return Ok(new ApiResponse<List<BookingResponseDto>>(trips, $"Retrieved {tab} trips."));
        }

        // =============================
        // Get All Bookings (Admin)
        // =============================
        [Authorize(Roles = "Admin,Airline")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(new ApiResponse<List<BookingResponseDto>>(bookings, "All bookings retrieved."));
        }

        // =============================
        // Get Flight Bookings (Admin)
        // =============================
        [Authorize(Roles = "Admin,Airline")]
        [HttpGet("flight/{flightId}/bookings")]
        public async Task<IActionResult> GetFlightBookings(long flightId)
        {
            var bookings = await _bookingService.GetFlightBookingsAsync(flightId);

            return Ok(new ApiResponse<List<BookingResponseDto>>(
                bookings,
                "Retrieved flight bookings successfully."
            ));
        }

        // =============================
        // Get Booking By Id
        // =============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking == null)
            {
                return NotFound(new ApiResponse<string>(
                    success: false,
                    message: "Booking not found."
                ));
            }

            return Ok(new ApiResponse<BookingResponseDto>(
                booking,
                "Retrieved booking successfully."
            ));
        }

        // =============================
        // Preview Cancellation
        // =============================
        [HttpGet("{id}/cancel-preview")]
        public async Task<IActionResult> PreviewCancel(long id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized(new ApiResponse<string>(false, "Invalid User."));

            try
            {
                var preview = await _bookingService.PreviewCancelAsync(userId, id);
                return Ok(new ApiResponse<AirlineCancelPreviewDto>(preview, "Cancellation preview generated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        // =============================
        // Cancel Booking
        // =============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(long id, [FromBody] CancelBookingRefundRequest? request = null)
        {
            await _bookingService.CancelAsync(id, request?.RefundMethod);

            return Ok(new ApiResponse<string>(
                "Booking cancelled successfully."
            ));
        }

        [Authorize(Roles = "Admin,Airline")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromQuery] string status, [FromQuery] string? reason = null)
        {
            await _bookingService.UpdateBookingStatusAsync(id, status, reason);
            return Ok(new ApiResponse<string>($"Booking status updated to {status}."));
        }

        [Authorize(Roles = "Admin,Airline")]
        [HttpPut("passenger/{passengerId}/status")]
        public async Task<IActionResult> UpdatePassengerStatus(long passengerId, [FromQuery] string status, [FromQuery] string? reason = null)
        {
            await _bookingService.UpdatePassengerStatusAsync(passengerId, status, reason);
            return Ok(new ApiResponse<string>($"Passenger status updated to {status}."));
        }

        [HttpGet("{id}/eticket")]
        public async Task<IActionResult> GetETicket(long id)
        {
            try
            {
                var eticket = await _bookingService.GetETicketAsync(id);
                return Ok(new ApiResponse<ETicketDto>(eticket, "E-Ticket retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(success: false, message: ex.Message));
            }
        }

        [HttpPost("{bookingId}/passenger-details")]
        public async Task<IActionResult> SavePassengerDetails(long bookingId, [FromBody] SaveBookingPassengersRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                {
                    return Unauthorized(new ApiResponse<string>(false, "User ID not found in token."));
                }

                await _passengerService.SavePassengerDetailsAsync(bookingId, request, userId);
                return Ok(new ApiResponse<string>("Passenger details saved successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(success: false, message: ex.Message));
            }
        }
    }
}
