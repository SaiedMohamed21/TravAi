using TravAi;
using TravAi.Data;
using Microsoft.EntityFrameworkCore;

using TravAi.Airline.DTOs.Review;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Airline.Models;

namespace TravAi.Airline.Services.ReviewService
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewResponseDto> AddReviewAsync(long userId, ReviewRequestDto dto)
        {
            var flight = await _context.Flights.FindAsync(dto.FlightId);
            if (flight == null)
                throw new Exception("Flight not found.");

            // Optional: Verify if user has booked this flight
            /*
            var hasBooking = await _context.Bookings.AnyAsync(b => b.UserId == userId && b.FlightId == dto.FlightId);
            if (!hasBooking)
                throw new Exception("You can only review flights you have booked.");
            */

            var review = new Review
            {
                UserId = userId,
                FlightId = dto.FlightId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                ReviewDate = DateTime.UtcNow
            };

            _context.AirlineReviews.Add(review);
            await _context.SaveChangesAsync();

            // Fetch user name for response
            var user = await _context.Users.FindAsync(userId);

            return new ReviewResponseDto
            {
                Id = review.Id,
                FlightId = review.FlightId,
                UserName = user?.UserName ?? "Unknown",
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewDate = review.ReviewDate
            };
        }

        public async Task<List<ReviewResponseDto>> GetFlightReviewsAsync(long flightId)
        {
            return await _context.AirlineReviews
                .Include(r => r.User)
                .Where(r => r.FlightId == flightId)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    FlightId = r.FlightId,
                    UserName = r.User.UserName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                })
                .ToListAsync();
        }
    }
}




