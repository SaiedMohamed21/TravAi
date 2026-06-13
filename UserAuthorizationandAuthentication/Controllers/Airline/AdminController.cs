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
            var flights = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Include(f => f.Airline)
            );

            var groups = flights
                .Where(f => f.DepartureAirportCode != null && f.ArrivalAirportCode != null)
                .GroupBy(f => new { f.DepartureAirportCode, f.ArrivalAirportCode })
                .Select(g => {
                    var depCode = g.Key.DepartureAirportCode;
                    var arrCode = g.Key.ArrivalAirportCode;
                    var depName = g.First().DepartureAirport?.Name ?? depCode;
                    var arrName = g.First().ArrivalAirport?.Name ?? arrCode;
                    var airlinesCount = g.Select(f => f.AirlineId).Distinct().Count();
                    
                    return new {
                        id = $"{depCode}-{arrCode}",
                        route = $"{depCode} → {arrCode}",
                        departure = $"{depCode} – {depName}",
                        arrival = $"{arrCode} – {arrName}",
                        distance = (string)null,
                        airlines = airlinesCount,
                        status = "active"
                    };
                })
                .ToList();

            return Ok(new ApiResponse<object>(groups));
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
                distance = (string)null,
                duration = first.Duration,
                airlinesCount = airlineGroups.Count,
                airlines = airlineGroups
            };

            return Ok(new ApiResponse<object>(result));
        }
    }
}
