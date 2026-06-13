using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;

using TravAi.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TravAi.Airline.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/airline/admin")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AdminController : ControllerBase
    {
        private readonly TravAi.Data.ApplicationDbContext _context;

        public AdminController(TravAi.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // =============================
        // Approve Airline
        // =============================
        [HttpPost("approve-airline")]
        public IActionResult ApproveAirline()
        {
            return Ok(new ApiResponse<string>(
                data: "Airline approved",
                message: "Success"
            ));
        }

        // =============================
        // Approve Hotel
        // =============================
        [HttpPost("approve-hotel")]
        public IActionResult ApproveHotel()
        {
            return Ok(new ApiResponse<string>(
                data: "Hotel approved",
                message: "Success"
            ));
        }

        // =============================
        // Approve Tour Guide
        // =============================
        [HttpPost("approve-tourguide")]
        public IActionResult ApproveTourGuide()
        {
            return Ok(new ApiResponse<string>(
                data: "Tour guide approved",
                message: "Success"
            ));
        }

        // =============================
        // Get Routes (Aggregation)
        // =============================
        [HttpGet("routes")]
        public async System.Threading.Tasks.Task<IActionResult> GetRoutes()
        {
            var flightGroups = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                _context.Flights
                    .Where(f => f.DepartureAirportCode != null && f.ArrivalAirportCode != null)
                    .GroupBy(f => new { f.DepartureAirportCode, f.ArrivalAirportCode })
                    .Select(g => new {
                        DepartureCode = g.Key.DepartureAirportCode,
                        ArrivalCode = g.Key.ArrivalAirportCode,
                        AirlinesCount = g.Select(f => f.AirlineId).Distinct().Count()
                    })
            );

            var airports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToDictionaryAsync(
                _context.Airports,
                a => a.Code,
                a => a.Name
            );

            var result = flightGroups.Select(g => {
                airports.TryGetValue(g.DepartureCode!, out var depName);
                airports.TryGetValue(g.ArrivalCode!, out var arrName);
                return new {
                    id = $"{g.DepartureCode}-{g.ArrivalCode}",
                    route = $"{g.DepartureCode} → {g.ArrivalCode}",
                    departure = $"{g.DepartureCode} – {(depName ?? g.DepartureCode)}",
                    arrival = $"{g.ArrivalCode} – {(arrName ?? g.ArrivalCode)}",
                    airlines = g.AirlinesCount,
                    status = "active"
                };
            }).ToList();

            return Ok(new ApiResponse<object>(result));
        }

        // =============================
        // Get Route Detail (Aggregation)
        // =============================
        [HttpGet("routes/{routeId}")]
        public async System.Threading.Tasks.Task<IActionResult> GetRouteDetail(string routeId)
        {
            if (string.IsNullOrEmpty(routeId) || !routeId.Contains("-"))
                return BadRequest(new ApiResponse<string>(false, "Invalid route ID format"));

            var parts = routeId.Split('-');
            var depCode = parts[0];
            var arrCode = parts[1];

            var flights = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Include(f => f.Airline)
                    .Where(f => f.DepartureAirportCode == depCode && f.ArrivalAirportCode == arrCode)
            );

            if (!flights.Any())
                return NotFound(new ApiResponse<string>(false, "Route not found"));

            var first = flights.First();
            var depName = first.DepartureAirport?.Name ?? depCode;
            var arrName = first.ArrivalAirport?.Name ?? arrCode;

            var airlineGroups = flights
                .Where(f => f.Airline != null)
                .GroupBy(f => f.AirlineId)
                .Select(g => {
                    var airline = g.First().Airline;
                    var country = airline?.Country ?? "Unknown";
                    string flag = country switch
                    {
                        "Egypt" => "🇪🇬",
                        "UAE" => "🇦🇪",
                        "Saudi Arabia" => "🇸🇦",
                        "Qatar" => "🇶🇦",
                        "Turkey" => "🇹🇷",
                        "France" => "🇫🇷",
                        "Germany" => "🇩🇪",
                        "United Kingdom" => "🇬🇧",
                        _ => "✈️"
                    };

                    return new {
                        id = airline.Id,
                        name = airline.Name,
                        country = country,
                        flag = flag,
                        flightsPerWeek = g.Count(),
                        status = airline.Status == "Approved" ? "active" : "inactive"
                    };
                })
                .ToList();

            var result = new {
                route = $"{depCode} → {arrCode}",
                departureAirport = $"{depCode} – {depName}",
                arrivalAirport = $"{arrCode} – {arrName}",
                duration = first.Duration,
                airlinesCount = airlineGroups.Count,
                airlines = airlineGroups
            };

            return Ok(new ApiResponse<object>(result));
        }

        // =============================
        // Update Airline Status
        // =============================
        [HttpPut("airlines/{id}/status")]
        public async System.Threading.Tasks.Task<IActionResult> UpdateAirlineStatus(long id, [FromBody] UpdateAirlineStatusDto dto)
        {
            var exists = await _context.Airlines.AnyAsync(a => a.Id == id);
            if (!exists)
                return NotFound(new ApiResponse<string>(false, "Airline not found"));

            var isActive = dto.Status.ToLower() == "active" || dto.Status.ToLower() == "approved";
            var statusStr = isActive ? "Approved" : "Rejected";

            await _context.Airlines
                .Where(a => a.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, statusStr)
                    .SetProperty(a => a.IsApproved, isActive)
                );

            var flightStatus = isActive ? "Active" : "Inactive";
            await _context.Flights
                .Where(f => f.AirlineId == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.Status, flightStatus)
                );

            return Ok(new ApiResponse<string>(true, "Status updated successfully"));
        }





        public class UpdateAirlineStatusDto
        {
            public string Status { get; set; } = null!;
        }

        // =============================
        // Get Flight Reports (Aggregation)
        // =============================
        [HttpGet("reports")]
        public async System.Threading.Tasks.Task<IActionResult> GetReports()
        {
            // 1. Total Flights
            var totalFlights = await _context.Flights.CountAsync();
            var activeFlights = await _context.Flights.CountAsync(f => f.Status == "Active");

            // 2. Bookings (excluding cancelled/rejected)
            var bookingsQuery = _context.Bookings.Where(b => b.Status != "Cancelled" && b.Status != "Rejected");
            var totalBookings = await bookingsQuery.CountAsync();

            // 3. Revenue
            var totalRevenue = await bookingsQuery.SumAsync(b => b.TotalPrice);

            // 4. Average Ticket Price
            var averageTicketPrice = totalBookings > 0 ? (totalRevenue / totalBookings) : 0;

            // 5. Bookings per month (e.g., last 6 months)
            var bookingsPerMonth = await bookingsQuery
                .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var bookingsPerMonthList = bookingsPerMonth.Select(b => new {
                month = $"{b.Year}-{b.Month:D2}",
                bookings = b.Count
            }).ToList();

            // 6. Revenue By Airline
            var revenueByAirline = await bookingsQuery
                .Include(b => b.Flight)
                .ThenInclude(f => f.Airline)
                .GroupBy(b => b.Flight.Airline.Name)
                .Select(g => new {
                    AirlineName = g.Key ?? "Unknown Airline",
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(g => g.Revenue)
                .ToListAsync();

            var revenueByAirlineList = revenueByAirline.Select(a => new {
                airlineName = a.AirlineName,
                revenue = a.Revenue
            }).ToList();

            // 7. Route Performance & Top Routes
            var routesQuery = bookingsQuery
                .Include(b => b.Flight)
                .GroupBy(b => new { b.Flight.DepartureAirportCode, b.Flight.ArrivalAirportCode })
                .Select(g => new {
                    Departure = g.Key.DepartureAirportCode,
                    Arrival = g.Key.ArrivalAirportCode,
                    BookingsCount = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalPrice)
                });

            var rawRoutes = await routesQuery.ToListAsync();

            var routePerformanceList = rawRoutes.Select(r => {
                var routeStr = $"{r.Departure} → {r.Arrival}";
                return new {
                    route = routeStr,
                    totalBookings = r.BookingsCount,
                    totalRevenue = r.TotalRevenue,
                    averagePrice = r.BookingsCount > 0 ? (r.TotalRevenue / r.BookingsCount) : 0
                };
            }).ToList();

            var topRoutesList = routePerformanceList
                .OrderByDescending(r => r.totalBookings)
                .Take(5)
                .Select(r => new {
                    route = r.route,
                    bookingsCount = r.totalBookings
                })
                .ToList();

            var result = new {
                totalFlights = totalFlights,
                activeFlights = activeFlights,
                totalBookings = totalBookings,
                totalRevenue = totalRevenue,
                averageTicketPrice = averageTicketPrice,
                bookingsPerMonth = bookingsPerMonthList,
                revenueByAirline = revenueByAirlineList,
                topRoutes = topRoutesList,
                routePerformance = routePerformanceList
            };

            return Ok(new ApiResponse<object>(result));
        }
    }
}
