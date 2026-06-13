using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Models.Enums;
using TravAi.Data;
using TravAi.DTOs.AdminManagement;
using TravAi.Models.Auth;


namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/admin/users")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirlineAdminUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AirlineAdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/airline/admin/users - Get all users (Admin only)
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.UserName,
                    Role = u.Role.ToString(),
                    Status = u.Status.ToString(),
                    u.Nationality,
                    u.PassportNumber,
                    u.ProfilePic,
                    u.CreatedAt,
                    u.IsBanned
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(new ApiResponse<object>(users));
        }

        // PUT: api/airline/admin/users/{id}/status - Update user status (Admin only)
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(long id, [FromBody] UpdateUserStatusDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<object>(null, "User not found"));

            // Parse and update status
            if (Enum.TryParse<UserStatus>(dto.Status, true, out var newStatus))
            {
                user.Status = newStatus;
                
                // If banning user, set IsBanned flag
                if (newStatus == UserStatus.Banned)
                    user.IsBanned = true;
                else
                    user.IsBanned = false;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(new
                {
                    user.Id,
                    user.Name,
                    Status = user.Status.ToString(),
                    user.IsBanned
                }, "User status updated successfully"));
            }

            return BadRequest(new ApiResponse<object>(null, "Invalid status value"));
        }

        // PUT: api/airline/admin/users/{id}/role - Update user role (Admin only)
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(long id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<object>(null, "User not found"));

            // Parse and update role
            if (Enum.TryParse<UserRole>(dto.Role, true, out var newRole))
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(new
                {
                    user.Id,
                    user.Name,
                    Role = user.Role.ToString()
                }, "User role updated successfully"));
            }

            return BadRequest(new ApiResponse<object>(null, "Invalid role value"));
        }

        // GET: api/airline/admin/users/{id} - Aggregate user details (Admin only)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetail(long id)
        {
            var user = await _context.Users
                .Include(u => u.UserPhones)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new ApiResponse<object>(null, "User not found"));

            // 1. Profile information
            var profile = new UserDetailProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserName = user.UserName,
                Country = user.Nationality ?? "Egypt",
                RegistrationDate = user.CreatedAt,
                Phones = user.UserPhones.Select(p => p.PhoneNumber).ToList(),
                Status = user.Status.ToString()
            };

            // 2. Flight bookings
            var flightBookings = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.UserId == id)
                .Select(b => new UserDetailFlightBookingDto
                {
                    BookingId = "FBK-" + b.Id,
                    FlightInfo = (b.Flight.DepartureAirportCode ?? "N/A") + " → " + (b.Flight.ArrivalAirportCode ?? "N/A"),
                    BookingDate = b.BookingDate,
                    Price = b.TotalPrice,
                    Status = b.Status
                })
                .ToListAsync();

            // 3. Hotel bookings
            var hotelBookings = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Where(b => b.UserId == id)
                .Select(b => new UserDetailHotelBookingDto
                {
                    BookingId = "HBK-" + b.Id,
                    HotelName = b.Hotel.HotelName,
                    City = b.Hotel.CityArea ?? "N/A",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Price = b.TotalPrice,
                    Status = b.Status.ToString()
                })
                .ToListAsync();

            // 4. Tour bookings
            var tourBookings = await _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Where(b => b.UserId == id)
                .Select(b => new UserDetailTourBookingDto
                {
                    BookingId = "TBK-" + b.Id,
                    TourName = b.Tour.TourTitle,
                    GuideName = b.TourGuide.Name,
                    TourDate = b.TourDate,
                    Price = b.TotalPrice,
                    Status = b.Status.ToString()
                })
                .ToListAsync();

            // 5. Reviews (Aggregate Airline, Hotel, and Tour reviews)
            var reviews = new List<UserDetailReviewDto>();

            var airlineReviews = await _context.AirlineReviews
                .Include(r => r.Flight)
                .Where(r => r.UserId == id)
                .Select(r => new UserDetailReviewDto
                {
                    Type = "Flight",
                    TargetName = "Flight " + (r.Flight.FlightNumber ?? r.Flight.Id.ToString()),
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    Date = r.ReviewDate
                })
                .ToListAsync();
            reviews.AddRange(airlineReviews);

            var hotelReviews = await _context.HotelReviews
                .Include(r => r.Hotel)
                .Where(r => r.UserId == id)
                .Select(r => new UserDetailReviewDto
                {
                    Type = "Hotel",
                    TargetName = r.Hotel.HotelName,
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    Date = r.CreatedAt
                })
                .ToListAsync();
            reviews.AddRange(hotelReviews);

            var tourReviews = await _context.TourReviews
                .Include(r => r.Tour)
                .Where(r => r.UserId == id)
                .Select(r => new UserDetailReviewDto
                {
                    Type = "Tour",
                    TargetName = r.Tour.TourTitle,
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    Date = r.CreatedAt
                })
                .ToListAsync();
            reviews.AddRange(tourReviews);

            // 6. Complaints
            var complaints = await _context.Complaints
                .Where(c => c.UserId == id)
                .Select(c => new UserDetailComplaintDto
                {
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    Date = c.CreatedAt
                })
                .ToListAsync();

            // 7. Statistics
            var statistics = new UserDetailStatsDto
            {
                TotalBookings = flightBookings.Count + hotelBookings.Count + tourBookings.Count,
                TotalSpending = flightBookings.Sum(fb => fb.Price) + hotelBookings.Sum(hb => hb.Price) + tourBookings.Sum(tb => tb.Price),
                FlightBookingsCount = flightBookings.Count,
                HotelBookingsCount = hotelBookings.Count,
                TourBookingsCount = tourBookings.Count
            };

            var response = new UserDetailResponseDto
            {
                Profile = profile,
                Statistics = statistics,
                FlightBookings = flightBookings,
                HotelBookings = hotelBookings,
                TourBookings = tourBookings,
                Reviews = reviews.OrderByDescending(r => r.Date).ToList(),
                Complaints = complaints.OrderByDescending(c => c.Date).ToList()
            };

            return Ok(new ApiResponse<UserDetailResponseDto>(response, "User detail aggregated successfully"));
        }
    }
}
