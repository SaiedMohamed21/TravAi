namespace TravAi.Airline.DTOs.Flight
{
    public class PaginatedFlightResultDto
    {
        public List<FlightResultDto> Flights { get; set; } = new();
        public List<FlightResultDto>? InboundFlights { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}


