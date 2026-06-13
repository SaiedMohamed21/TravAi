using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using TravAi.Airline.DTOs.Flight;
using TravAi.Airline.Models.Airlines;
using TravAi.Models;
using TravAi.Models.Auth;

namespace TravAi.Airline.Services.FlightService
{
    public class FlightService : IFlightService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public FlightService(ApplicationDbContext context, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<FlightResultDto?> GetByIdAsync(long id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Segments)
                    .ThenInclude(s => s.FromAirport)
                .Include(f => f.Segments)
                    .ThenInclude(s => s.ToAirport)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null || flight.Airline == null || 
                (flight.Airline.Status != "Approved" && flight.Airline.Status != "Active" && !flight.Airline.IsApproved) ||
                flight.Airline.Status == "Inactive" || flight.Airline.Status == "Disabled" || flight.Airline.Status == "Rejected" ||
                flight.Status == "Inactive")
            {
                return null;
            }

            return MapToFlightResultDto(flight);
        }

        public async Task<PaginatedFlightResultDto> SearchAsync(FlightSearchDto dto)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.Airline != null && 
                            (f.Airline.Status == "Approved" || f.Airline.Status == "Active" || f.Airline.IsApproved) && 
                            f.Airline.Status != "Inactive" && 
                            f.Airline.Status != "Disabled" && 
                            f.Airline.Status != "Rejected" &&
                            f.Status != "Inactive")
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.From))
                query = query.Where(f => f.DepartureAirportCode == dto.From || f.DepartureAirport.City == dto.From);

            if (!string.IsNullOrEmpty(dto.To))
                query = query.Where(f => f.ArrivalAirportCode == dto.To || f.ArrivalAirport.City == dto.To);

            if (dto.Date.HasValue)
                query = query.Where(f => f.DepartureTime.HasValue && f.DepartureTime.Value.Date == dto.Date.Value.Date);

            if (dto.AirlineIds != null && dto.AirlineIds.Any())
                query = query.Where(f => f.AirlineId.HasValue && dto.AirlineIds.Contains(f.AirlineId.Value));

            if (dto.MinPrice.HasValue)
                query = query.Where(f => f.Price >= dto.MinPrice.Value);

            if (dto.MaxPrice.HasValue)
                query = query.Where(f => f.Price <= dto.MaxPrice.Value);

            if (!string.IsNullOrEmpty(dto.Class))
                query = query.Where(f => f.FlightClass != null && f.FlightClass.ToLower() == dto.Class.ToLower());

            var totalCount = await query.CountAsync();
            
            var flights = await query
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var flightDtos = flights.Select(MapToFlightResultDto).ToList();

            var result = new PaginatedFlightResultDto
            {
                Flights = flightDtos,
                TotalCount = totalCount,
                Page = dto.Page,
                PageSize = dto.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)dto.PageSize),
                InboundFlights = new List<FlightResultDto>()
            };

            if (!string.IsNullOrEmpty(dto.TripType) && dto.TripType.Equals("RoundTrip", StringComparison.OrdinalIgnoreCase) && dto.ReturnDate.HasValue)
            {
                var inboundQuery = _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.Airline != null && 
                                (f.Airline.Status == "Approved" || f.Airline.Status == "Active" || f.Airline.IsApproved) && 
                                f.Airline.Status != "Inactive" && 
                                f.Airline.Status != "Disabled" && 
                                f.Airline.Status != "Rejected" &&
                                f.Status != "Inactive")
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(dto.To))
                    inboundQuery = inboundQuery.Where(f => f.DepartureAirportCode == dto.To || f.DepartureAirport.City == dto.To);

                if (!string.IsNullOrEmpty(dto.From))
                    inboundQuery = inboundQuery.Where(f => f.ArrivalAirportCode == dto.From || f.ArrivalAirport.City == dto.From);

                inboundQuery = inboundQuery.Where(f => f.DepartureTime.HasValue && f.DepartureTime.Value.Date == dto.ReturnDate.Value.Date);

                if (dto.AirlineIds != null && dto.AirlineIds.Any())
                    inboundQuery = inboundQuery.Where(f => f.AirlineId.HasValue && dto.AirlineIds.Contains(f.AirlineId.Value));

                if (dto.MinPrice.HasValue)
                    inboundQuery = inboundQuery.Where(f => f.Price >= dto.MinPrice.Value);

                if (dto.MaxPrice.HasValue)
                    inboundQuery = inboundQuery.Where(f => f.Price <= dto.MaxPrice.Value);

                if (!string.IsNullOrEmpty(dto.Class))
                    inboundQuery = inboundQuery.Where(f => f.FlightClass != null && f.FlightClass.ToLower() == dto.Class.ToLower());

                var inboundFlightsList = await inboundQuery.Take(20).ToListAsync();
                result.InboundFlights = inboundFlightsList.Select(MapToFlightResultDto).ToList();
            }

            return result;
        }

        private FlightResultDto MapToFlightResultDto(Flight f)
        {
            var departure = f.DepartureTime ?? DateTime.Now;
            var arrival = f.ArrivalTime ?? departure.AddHours(2);
            var totalDuration = arrival - departure;
            
            var dto = new FlightResultDto
            {
                Id = f.Id,
                FlightId = $"flight_{f.Id}",
                Airline = new AirlineInfoDto
                {
                    Name = f.Airline?.Name ?? "EgyptAir",
                    Logo = BuildFullLogoUrl(f.Airline?.LogoUrl)
                },
                AirlineName = f.Airline?.Name ?? "EgyptAir",
                Price = f.Price ?? 0,
                Currency = f.Currency ?? "USD",
                CabinClass = f.FlightClass ?? "Economy",
                TotalDuration = f.Duration ?? $"{totalDuration.Hours}h {totalDuration.Minutes}m",
                FromCode = f.DepartureAirportCode ?? "TBD",
                ToCode = f.ArrivalAirportCode ?? "TBD",
                FromCity = f.DepartureAirport?.City ?? "TBD",
                ToCity = f.ArrivalAirport?.City ?? f.ArrivalAirportCode ?? "TBD",
                FromCountry = f.DepartureAirport?.Country ?? "TBD",
                ToCountry = f.ArrivalAirport?.Country ?? "TBD",
                DepartureTime = departure,
                ArrivalTime = arrival,
                AvailableSeats = f.AvailableSeats ?? 0,
                NumberOfStops = f.NumberOfStops ?? 0,
                FlightNumber = f.FlightNumber ?? $"FL-{f.Id}",
                Status = f.Status ?? "Active",
                DestinationImageUrl = f.DestinationImageUrl
            };

            if (f.Segments != null)
            {
                foreach (var s in f.Segments.OrderBy(s => s.SegmentNumber))
                {
                    dto.Segments.Add(new FlightSegmentDto
                    {
                        SegmentId = $"seg_{s.Id}",
                        From = new AirportInfoDto
                        {
                            Code = s.FromAirportCode ?? s.FromAirport?.Code,
                            City = s.FromAirport?.City ?? "TBD"
                        },
                        To = new AirportInfoDto
                        {
                            Code = s.ToAirportCode ?? s.ToAirport?.Code,
                            City = s.ToAirport?.City ?? "TBD"
                        },
                        DepartureTime = s.DepartureTime ?? departure,
                        ArrivalTime = s.ArrivalTime ?? arrival,
                        Duration = "TBD",
                        Amenities = s.Amenities?.Split(',').ToList() ?? new List<string>()
                    });
                }
            }

            return dto;
        }

        private string BuildFullLogoUrl(string logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl)) return "https://logo.clearbit.com/egyptair.com";
            if (!logoUrl.StartsWith("/")) return logoUrl;
            
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}{request.PathBase}" : "";
            return $"{baseUrl}{logoUrl}";
        }

        public async Task CreateAsync(long userId, CreateFlightDto dto) { }
        public async Task UpdateAsync(long id, UpdateFlightDto dto) { }
        public async Task CancelAsync(long id) { }
    }
}

