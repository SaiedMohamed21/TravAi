using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.TourGuide.DTOs.Booking;
using TravAi.TourGuide.Models.Enums;
using TravAi.TourGuide.Services;
using Microsoft.EntityFrameworkCore;
using TravAi.Models.Hotels;
using TravAi.Models.Hotels.Bookings;
using TravAi.TourGuide.Models;
using System.Linq;

namespace TravAi.TourGuide.Controllers
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
        private readonly TravAi.Data.ApplicationDbContext _context;

        public TourBookingController(IBookingService bookingService, TravAi.Data.ApplicationDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
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
        /// Preview booking cancellation refund details
        /// </summary>
        [HttpGet("{bookingId}/cancel-preview")]
        public async Task<IActionResult> PreviewCancelBooking(long bookingId)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            try
            {
                var preview = await _bookingService.PreviewCancelBookingAsync(userId, bookingId);
                return Ok(new ApiResponse<TourCancelPreviewDto>(preview, "Tour cancellation preview generated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        /// <summary>
        /// Cancel a booking (pending or confirmed)
        /// </summary>
        [HttpDelete("{bookingId}")]
        public async Task<IActionResult> CancelBooking(long bookingId, [FromBody] CancelTourBookingRequest? request = null)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var success = await _bookingService.CancelBookingAsync(userId, bookingId, request?.RefundMethod);
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

        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips([FromQuery] string tab)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized();

            var result = await _bookingService.GetUserTripsAsync(userId, tab);
            return Ok(new ApiResponse<List<BookingResponseDto>>(result, $"Retrieved {tab} tour trips."));
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

        /// <summary>
        /// Seed 3 past completed bookings (Tours and/or Hotels) for testing review system
        /// </summary>
        [HttpGet("seed-past-reviews")]
        public async Task<IActionResult> SeedPastReviews()
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            // Get first 2 tours
            var tours = await _context.Tours.Where(t => t.Active).Take(2).ToListAsync();
            // Get first hotel
            var hotel = await _context.Hotels.FirstOrDefaultAsync();

            int seededCount = 0;

            if (tours.Count > 0)
            {
                foreach (var tour in tours)
                {
                    // Create a past completed Tour booking
                    var tourBooking = new TourBooking
                    {
                        UserId = userId,
                        TourId = tour.Id,
                        TourGuideId = tour.TourGuideId,
                        BookingDate = DateTime.UtcNow.AddDays(-30),
                        TourDate = DateTime.UtcNow.AddDays(-15),
                        TourTime = new TimeSpan(9, 0, 0),
                        ParticipantsCount = 2,
                        TotalPrice = (tour.BasePriceUsd ?? 100) * 2,
                        Currency = tour.Currency ?? "USD",
                        SpecialRequests = "Test past tour booking for review",
                        PaymentStatus = TravAi.TourGuide.Models.Enums.PaymentStatus.Completed,
                        Status = TravAi.TourGuide.Models.Enums.BookingStatus.Completed,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        UpdatedAt = DateTime.UtcNow.AddDays(-15)
                    };
                    _context.TourBookings.Add(tourBooking);
                    seededCount++;
                }
            }

            if (hotel != null)
            {
                // Create a past completed Hotel booking
                var hotelBooking = new HotelBooking
                {
                    UserId = userId,
                    HotelId = hotel.Id,
                    CheckInDate = DateTime.UtcNow.AddDays(-20),
                    CheckOutDate = DateTime.UtcNow.AddDays(-15),
                    Nights = 5,
                    TotalRooms = 1,
                    TotalPrice = 500,
                    PaymentStatus = TravAi.Models.Enums.PaymentStatus.Paid,
                    Status = TravAi.Models.Enums.BookingStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                };
                
                // Add a booking room as well to avoid details errors
                var firstRoom = await _context.HotelRooms.FirstOrDefaultAsync(r => r.HotelId == hotel.Id);
                var roomName = firstRoom?.RoomName ?? "Standard Room";
                var roomPrice = firstRoom?.BBPrice ?? 100;
                
                var bookingRoom = new HotelBookingRoom
                {
                    Booking = hotelBooking,
                    RoomId = firstRoom?.Id,
                    RoomName = roomName,
                    MealPlan = "BB",
                    PricePerNight = roomPrice,
                    Nights = 5,
                    Subtotal = roomPrice * 5
                };
                hotelBooking.BookingRooms.Add(bookingRoom);
                
                _context.HotelBookings.Add(hotelBooking);
                seededCount++;
            }

            // Get first flight
            var flight = await _context.Flights.FirstOrDefaultAsync();
            if (flight != null)
            {
                var flightBooking = new TravAi.Airline.Models.Booking
                {
                    UserId = userId,
                    FlightId = flight.Id,
                    NumberOfSeats = 1,
                    TotalPrice = flight.Price ?? 150,
                    BookingDate = DateTime.UtcNow.AddDays(-20),
                    Status = "Completed",
                    PaymentStatus = "Paid"
                };
                _context.Bookings.Add(flightBooking);
                seededCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Successfully seeded {seededCount} past journeys for review testing." });
        }

        /// <summary>
        /// Submit a review for a completed tour
        /// </summary>
        [HttpPost("reviews")]
        public async Task<IActionResult> SubmitTourReview([FromBody] CreateTourReviewRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            // Find the tour to get TourGuideId
            var tour = await _context.Tours.FindAsync(model.TourId);
            if (tour == null) return NotFound("Tour not found.");

            var review = new TravAi.TourGuide.Models.Review
            {
                UserId = userId,
                TourId = model.TourId,
                TourGuideId = tour.TourGuideId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.TourReviews.Add(review);

            // Fetch current reviews for this tour to calculate the new average rating
            var tourReviews = _context.TourReviews.Where(r => r.TourId == model.TourId).ToList();
            tourReviews.Add(review);

            tour.NumberOfReviews = tourReviews.Count;
            tour.Rating = (decimal)tourReviews.Average(r => r.Rating);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Review added successfully" });
        }

        /// <summary>
        /// Update a review for a completed tour
        /// </summary>
        [HttpPut("reviews/{id}")]
        public async Task<IActionResult> UpdateTourReview(long id, [FromBody] UpdateTourReviewRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var review = await _context.TourReviews.FindAsync(id);
            if (review == null) return NotFound("Review not found.");
            if (review.UserId != userId) return Unauthorized("You are not authorized to update this review.");

            review.Rating = model.Rating;
            review.Comment = model.Comment;

            // Recalculate average rating for the tour
            var tour = await _context.Tours.FindAsync(review.TourId);
            if (tour != null)
            {
                var tourReviews = _context.TourReviews.Where(r => r.TourId == review.TourId).ToList();
                tour.Rating = (decimal)tourReviews.Average(r => r.Rating);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Review updated successfully" });
        }

        /// <summary>
        /// Delete a review for a completed tour
        /// </summary>
        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteTourReview(long id)
        {
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            var review = await _context.TourReviews.FindAsync(id);
            if (review == null) return NotFound("Review not found.");
            if (review.UserId != userId) return Unauthorized("You are not authorized to delete this review.");

            _context.TourReviews.Remove(review);

            // Recalculate average rating for the tour
            var tour = await _context.Tours.FindAsync(review.TourId);
            if (tour != null)
            {
                var tourReviews = _context.TourReviews.Where(r => r.TourId == review.TourId && r.Id != id).ToList();
                tour.NumberOfReviews = tourReviews.Count;
                tour.Rating = tourReviews.Count > 0 ? (decimal)tourReviews.Average(r => r.Rating) : 0;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Review deleted successfully" });
        }
    }

    public class CreateTourReviewRequest
    {
        public long TourId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class UpdateTourReviewRequest
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class CancelTourBookingRequest
    {
        public string? RefundMethod { get; set; }
    }
}

