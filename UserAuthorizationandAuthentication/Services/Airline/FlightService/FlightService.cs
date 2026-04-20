using UserAuthorizationandAuthentication;
using Microsoft.EntityFrameworkCore;

using UserAuthorizationandAuthentication.Airline.DTOs.Flight;
using UserAuthorizationandAuthentication.Airline.Models.Airlines;

namespace UserAuthorizationandAuthentication.Airline.Services.FlightService
{
    public class FlightService : IFlightService
    {
        private readonly ApplicationDbContext _context;

        public FlightService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(long userId, CreateFlightDto dto)
        {
            if (dto.ArrivalTime < dto.DepartureTime)
                throw new Exception("Arrival time cannot be before departure time.");

            var flight = new Flight
            {
                DepartureAirportCode = dto.DepartureAirportCode,
                ArrivalAirportCode = dto.ArrivalAirportCode,
                DepartureTime = dto.DepartureTime,
                ArrivalTime = dto.ArrivalTime,
                Price = dto.Price,
                AvailableSeats = dto.AvailableSeats,
                AirlineId = dto.AirlineId,
                CreatedByUserId = userId,
                Status = "Active"
            };

            // Set destination image automatically if available
            var imageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CAI", "https://images.unsplash.com/photo-1572252009286-268acec5ca0a?q=80&w=2070" },
                { "DXB", "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?q=80&w=2070" },
                { "LHR", "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?q=80&w=2070" },
                { "JFK", "https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?q=80&w=2070" },
                { "CDG", "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?q=80&w=2070" },
                { "PAR", "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?q=80&w=2070" },
                { "RUH", "https://images.unsplash.com/photo-1589115712163-cf9d84635832?q=80&w=2070" },
                { "JED", "https://images.unsplash.com/photo-1626294311025-f71694df29da?q=80&w=2070" },
                { "IST", "https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?q=80&w=2070" },
                { "MED", "https://images.unsplash.com/photo-1591604129939-f1efa4d9f7fa?q=80&w=2070" },
                { "SSH", "https://images.unsplash.com/photo-1561569966-07a97210e7cb?q=80&w=2070" },
                { "ASW", "https://images.unsplash.com/photo-1548813831-2f2dd4b1b8e3?q=80&w=2070" },
                { "HRG", "https://images.unsplash.com/photo-1544551763-46a013bb70d5?q=80&w=2070" },
                { "LXR", "https://images.unsplash.com/photo-1539768942893-daf1563d6d4d?q=80&w=2070" },
                { "SVO", "https://images.unsplash.com/photo-1513326738677-b964603b136d?q=80&w=2070" },
                { "WAW", "https://images.unsplash.com/photo-1607427293702-036933bbf746?q=80&w=2070" }
            };

            if (imageMap.TryGetValue(dto.ArrivalAirportCode, out var url))
            {
                flight.DestinationImageUrl = url;
            }

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginatedFlightResultDto> SearchAsync(FlightSearchDto dto)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Segments)
                .Include(f => f.Layovers)
                .AsQueryable();

            // Basic Filters
            if (!string.IsNullOrEmpty(dto.From))
                query = query.Where(f => f.DepartureAirportCode == dto.From || f.DepartureAirport.City == dto.From);

            if (!string.IsNullOrEmpty(dto.To))
                query = query.Where(f => f.ArrivalAirportCode == dto.To || f.ArrivalAirport.City == dto.To);

            if (dto.Date.HasValue)
            {
                var targetDate = dto.Date.Value.Date;
                var nextDate = targetDate.AddDays(1);
                query = query.Where(f => f.DepartureTime >= targetDate && f.DepartureTime < nextDate);
            }

            // Price Filters
            if (dto.MinPrice.HasValue)
                query = query.Where(f => f.Price >= dto.MinPrice.Value);

            if (dto.MaxPrice.HasValue)
                query = query.Where(f => f.Price <= dto.MaxPrice.Value);

            // Stops Filter
            if (dto.MaxStops.HasValue)
                query = query.Where(f => f.NumberOfStops <= dto.MaxStops.Value);

            // Class Filter
            if (!string.IsNullOrEmpty(dto.Class))
                query = query.Where(f => f.FlightClass == dto.Class);

            // Passengers Filter (min available seats)
            if (dto.Passengers.HasValue && dto.Passengers.Value > 0)
                query = query.Where(f => f.AvailableSeats >= dto.Passengers.Value);

            // Airlines Filter
            if (dto.AirlineIds != null && dto.AirlineIds.Any())
                query = query.Where(f => dto.AirlineIds.Contains(f.AirlineId));

            // Time Filters
            if (dto.EarliestDeparture.HasValue)
                query = query.Where(f => f.DepartureTime.TimeOfDay >= dto.EarliestDeparture.Value);

            if (dto.LatestDeparture.HasValue)
                query = query.Where(f => f.DepartureTime.TimeOfDay <= dto.LatestDeparture.Value);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            query = dto.SortBy?.ToLower() switch
            {
                "price" => dto.SortOrder == "desc" 
                    ? query.OrderByDescending(f => f.Price) 
                    : query.OrderBy(f => f.Price),
                "duration" => dto.SortOrder == "desc"
                    ? query.OrderByDescending(f => f.ArrivalTime - f.DepartureTime)
                    : query.OrderBy(f => f.ArrivalTime - f.DepartureTime),
                "departure" => dto.SortOrder == "desc"
                    ? query.OrderByDescending(f => f.DepartureTime)
                    : query.OrderBy(f => f.DepartureTime),
                _ => query.OrderBy(f => f.DepartureTime) // Default sort
            };

            // Pagination
            var flights = await query
                .Skip((dto.Page - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .Select(f => new FlightResultDto
                {
                    Id = f.Id,
                    AirlineId = f.AirlineId,
                    AirlineName = f.Airline.Name,
                    AirlineLogoUrl = f.Airline.LogoUrl,
                    FromCode = f.DepartureAirportCode,
                    ToCode = f.ArrivalAirportCode,
                    FromAirportName = f.DepartureAirport.Name,
                    FromCity = f.DepartureAirport.City,
                    FromCountry = f.DepartureAirport.Country,
                    ToAirportName = f.ArrivalAirport.Name,
                    ToCity = f.ArrivalAirport.City,
                    ToCountry = f.ArrivalAirport.Country,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Duration = f.ArrivalTime - f.DepartureTime,
                    Price = f.Price,
                    AvailableSeats = f.AvailableSeats,
                    NumberOfStops = f.NumberOfStops,
                    FlightClass = f.FlightClass,
                    FlightNumber = f.FlightNumber,
                    DestinationImageUrl = f.DestinationImageUrl,
                    Status = f.Status,
                    CreatedByUserName = f.CreatedByUser != null ? f.CreatedByUser.Name : "System",
                    Segments = f.Segments.Select(s => new FlightSegmentDto
                    {
                        SegmentNumber = s.SegmentNumber,
                        Amenities = s.Amenities,
                        LegroomInches = s.LegroomInches
                    }).ToList(),
                    Layovers = f.Layovers.Select(l => new FlightLayoverDto
                    {
                        LayoverOrder = l.LayoverOrder,
                        AirportName = l.AirportName,
                        DurationString = l.DurationString
                    }).ToList()
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)dto.PageSize);

            return new PaginatedFlightResultDto
            {
                Flights = flights,
                TotalCount = totalCount,
                Page = dto.Page,
                PageSize = dto.PageSize,
                TotalPages = totalPages,
                HasPrevious = dto.Page > 1,
                HasNext = dto.Page < totalPages
            };
        }

        public async Task<FlightResultDto?> GetByIdAsync(long id)
        {
            return await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.CreatedByUser)
                .Include(f => f.Segments)
                .Include(f => f.Layovers)
                .Where(f => f.Id == id)
                .Select(f => new FlightResultDto
                {
                    Id = f.Id,
                    AirlineId = f.AirlineId,
                    AirlineName = f.Airline.Name,
                    AirlineLogoUrl = f.Airline.LogoUrl,
                    FromCode = f.DepartureAirportCode,
                    ToCode = f.ArrivalAirportCode,
                    FromAirportName = f.DepartureAirport.Name,
                    FromCity = f.DepartureAirport.City,
                    FromCountry = f.DepartureAirport.Country,
                    ToAirportName = f.ArrivalAirport.Name,
                    ToCity = f.ArrivalAirport.City,
                    ToCountry = f.ArrivalAirport.Country,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Duration = f.ArrivalTime - f.DepartureTime,
                    Price = f.Price,
                    AvailableSeats = f.AvailableSeats,
                    NumberOfStops = f.NumberOfStops,
                    FlightClass = f.FlightClass,
                    FlightNumber = f.FlightNumber,
                    DestinationImageUrl = f.DestinationImageUrl,
                    Status = f.Status,
                    Segments = f.Segments.Select(s => new FlightSegmentDto
                    {
                        SegmentNumber = s.SegmentNumber,
                        Amenities = s.Amenities,
                        LegroomInches = s.LegroomInches
                    }).ToList(),
                    Layovers = f.Layovers.Select(l => new FlightLayoverDto
                    {
                        LayoverOrder = l.LayoverOrder,
                        AirportName = l.AirportName,
                        DurationString = l.DurationString
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(long id, UpdateFlightDto dto)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null) throw new Exception("Flight not found");

            if (dto.Price.HasValue) flight.Price = dto.Price.Value;
            if (dto.AvailableSeats.HasValue) flight.AvailableSeats = dto.AvailableSeats.Value;
            
            if (dto.DepartureTime.HasValue) flight.DepartureTime = dto.DepartureTime.Value;
            if (dto.ArrivalTime.HasValue) flight.ArrivalTime = dto.ArrivalTime.Value;

            // Validate dates
            if (flight.ArrivalTime < flight.DepartureTime)
                throw new Exception("Arrival time cannot be before departure time.");

            await _context.SaveChangesAsync();
        }

        public async Task CancelAsync(long id)
        {
             var flight = await _context.Flights.FindAsync(id);
            if (flight == null) throw new Exception("Flight not found");

            flight.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }
    }
}



