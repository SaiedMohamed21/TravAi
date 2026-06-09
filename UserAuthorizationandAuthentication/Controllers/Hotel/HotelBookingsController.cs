using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.DTOs.Hotel;
using TravAi.Services.HotelService;

namespace TravAi.Controllers.Hotel
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All booking actions require login
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class HotelBookingsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelBookingsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return 0;
            return long.Parse(userIdClaim.Value);
        }

        // 1. Create Booking (User)
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var booking = await _hotelService.CreateBookingAsync(GetUserId(), request);
                return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, new ApiResponse<BookingDto>
                {
                    Success = true,
                    Message = "Booking created successfully.",
                    Data = booking,
                    Errors = new List<string>()
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "Internal Server Error: " + ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 2. Get My Bookings (User)
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyBookings()
        {
            var bookings = await _hotelService.GetMyBookingsAsync(GetUserId());
            return Ok(new ApiResponse<List<BookingDto>> { Success = true, Message = "Bookings retrieved.", Data = bookings, Errors = new List<string>() });
        }

        [HttpGet("/api/user/trips/hotels")]
        public async Task<IActionResult> GetUserTrips([FromQuery] string tab)
        {
            try
            {
                var trips = await _hotelService.GetMyTripsAsync(GetUserId(), tab ?? "upcoming");
                return Ok(new ApiResponse<List<MyTripHotelDto>> { Success = true, Message = "Trips retrieved.", Data = trips, Errors = new List<string>() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 3. Get Hotel Bookings (Owner)
        [HttpGet("hotel/{hotelId}")]
        public async Task<IActionResult> GetHotelBookings(long hotelId)
        {
            try
            {
                var bookings = await _hotelService.GetHotelBookingsAsync(GetUserId(), hotelId);
                return Ok(new ApiResponse<List<BookingDto>> { Success = true, Message = "Bookings retrieved.", Data = bookings, Errors = new List<string>() });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 4. Get Booking Details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(long id)
        {
            try
            {
                var booking = await _hotelService.GetBookingByIdAsync(GetUserId(), id);
                return Ok(new ApiResponse<BookingDto> { Success = true, Message = "Booking retrieved.", Data = booking, Errors = new List<string>() });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 5. Cancel Booking (User or Owner)
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingRequest request)
        {
            try
            {
                var booking = await _hotelService.CancelBookingAsync(GetUserId(), request.BookingId, request.Reason);
                return Ok(new ApiResponse<BookingDto>
                {
                    Success = true,
                    Message = "Booking cancelled successfully.",
                    Data = booking,
                    Errors = new List<string>()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "Error cancelling booking: " + ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 7. Process Payment (User)
        [HttpPost("pay")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                var result = await _hotelService.ProcessPaymentAsync(GetUserId(), request);
                return Ok(new ApiResponse<PaymentResponseDto> { Success = true, Message = "Payment processed successfully.", Data = result, Errors = new List<string>() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "Error processing payment: " + ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }

        // 6. Update Status (Owner)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(long id, [FromBody] BookingActionRequest request)
        {
            try
            {
                var booking = await _hotelService.UpdateBookingStatusAsync(GetUserId(), id, request.Action);
                return Ok(new ApiResponse<BookingDto> { Success = true, Message = "Status updated.", Data = booking, Errors = new List<string>() });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string> { Success = false, Message = ex.Message, Data = null!, Errors = new List<string> { ex.Message } });
            }
        }
    }
}



