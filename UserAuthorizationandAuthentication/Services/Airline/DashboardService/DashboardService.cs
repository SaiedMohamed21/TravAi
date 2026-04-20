using UserAuthorizationandAuthentication;
using Microsoft.EntityFrameworkCore;

using UserAuthorizationandAuthentication.Airline.DTOs.Dashboard;

namespace UserAuthorizationandAuthentication.Airline.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var stats = new DashboardStatsDto
            {
                TotalFlights = await _context.Flights.CountAsync(),
                ActiveFlights = await _context.Flights.CountAsync(f => f.Status == "Active"),
                TotalBookings = await _context.Bookings.CountAsync(),
                PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending"),
                TotalRevenue = await _context.Bookings.Where(b => b.Status == "Approved").SumAsync(b => b.TotalPrice),
                TotalUsers = await _context.Users.CountAsync(),
                RecentBookings = await _context.Bookings
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5)
                    .Select(b => new RecentBookingDto
                    {
                        Id = b.Id,
                        UserName = b.User.Name,
                        Route = $"{b.Flight.DepartureAirportCode} to {b.Flight.ArrivalAirportCode}",
                        Status = b.Status,
                        Price = b.TotalPrice,
                        Date = b.BookingDate
                    }).ToListAsync()
            };

            // Simple revenue chart data for last 6 months
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5);
            var bookings = await _context.Bookings
                .Where(b => b.Status == "Approved" && b.BookingDate >= startOfMonth)
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var monthDate = startOfMonth.AddMonths(i);
                var monthName = monthDate.ToString("MMM");
                var revenue = bookings
                    .Where(b => b.BookingDate.Month == monthDate.Month && b.BookingDate.Year == monthDate.Year)
                    .Sum(b => b.TotalPrice);

                stats.RevenueChart.Add(new RevenueByMonthDto
                {
                    Month = monthName,
                    Revenue = revenue
                });
            }

            return stats;
        }
    }
}



