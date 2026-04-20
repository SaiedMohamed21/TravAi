using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.TourGuide.DTOs.Booking;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Services;

namespace UserAuthorizationandAuthentication.TourGuide.Controllers
{
    /// <summary>
    /// Booking endpoints for users to manage tour bookings
    /// </summary>
    [Authorize(Roles = "User,Tourguide,Admin")]
    [Route("api/tourguide/bookings")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class TourBookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public TourBookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private long GetUserIdFromToken()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
                return userId;

            return 0;
        }

        /// <summary>
        /// Create a new booking (status: Pending) with just a Tour ID
        /// </summary>
        [HttpPost("tour/{tourId}")]
        public async Task<IActionResult> CreateBooking(long tourId)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            try
            {
                var result = await _bookingService.CreateBookingAsync(userId, tourId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update participant details for a pending booking
        /// </summary>
        [HttpPut("{bookingId}/participants")]
        public async Task<IActionResult> UpdateBookingParticipants(long bookingId, [FromBody] List<ParticipantDto> participants)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            try
            {
                var result = await _bookingService.UpdateBookingParticipantsAsync(userId, bookingId, participants);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Process payment for a pending booking
        /// </summary>
        [HttpPost("payment")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            try
            {
                var result = await _bookingService.ProcessPaymentAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancel a booking (pending or confirmed)
        /// </summary>
        [HttpDelete("{bookingId}")]
        public async Task<IActionResult> CancelBooking(long bookingId)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var success = await _bookingService.CancelBookingAsync(userId, bookingId);
            if (!success) return NotFound("Booking not found.");

            return Ok(new { message = "Booking cancelled successfully." });
        }

        /// <summary>
        /// Get all bookings for the authenticated user (optional status filter)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyBookings([FromQuery] BookingStatus? status = null)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var bookings = await _bookingService.GetUserBookingsAsync(userId, status);
            return Ok(bookings);
        }

        /// <summary>
        /// Get a specific booking by ID
        /// </summary>
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetBookingById(long bookingId)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var booking = await _bookingService.GetBookingByIdAsync(userId, bookingId);
            if (booking == null) return NotFound("Booking not found.");

            return Ok(booking);
        }

        /// <summary>
        /// (Admin) Get all tour bookings for the admin dashboard
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> AdminGetAllBookings([FromQuery] BookingStatus? status = null)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(status);
            return Ok(bookings);
        }

        /// <summary>
        /// (Admin) Get a specific booking by ID as an admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{bookingId}")]
        public async Task<IActionResult> AdminGetBookingById(long bookingId)
        {
            var booking = await _bookingService.GetBookingByAdminAsync(bookingId);
            if (booking == null) return NotFound("Booking not found.");
            return Ok(booking);
        }

        /// <summary>
        /// Check if a tour is available for booking
        /// </summary>
        [AllowAnonymous]
        [HttpGet("availability/{tourId}")]
        public async Task<IActionResult> CheckAvailability(long tourId, [FromQuery] int participantsCount = 1)
        {
            var isAvailable = await _bookingService.IsTourAvailableAsync(tourId, participantsCount);
            return Ok(new
            {
                tourId,
                participantsCount,
                isAvailable,
                message = isAvailable ? "Tour is available" : "Tour is fully booked"
            });
        }
    }
}

