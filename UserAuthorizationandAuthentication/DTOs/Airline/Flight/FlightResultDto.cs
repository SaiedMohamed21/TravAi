namespace UserAuthorizationandAuthentication.Airline.DTOs.Flight
{
    public class FlightResultDto
    {
        public long Id { get; set; }

        // Airline Info
        public long AirlineId { get; set; }
        public string AirlineName { get; set; } = null!;
        public string? AirlineLogoUrl { get; set; }

        // Route - Airport Codes
        public string FromCode { get; set; } = null!;
        public string ToCode { get; set; } = null!;

        // Route - Airport Details
        public string FromAirportName { get; set; } = null!;
        public string FromCity { get; set; } = null!;
        public string FromCountry { get; set; } = null!;

        public string ToAirportName { get; set; } = null!;
        public string ToCity { get; set; } = null!;
        public string ToCountry { get; set; } = null!;

        // Time
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public TimeSpan Duration { get; set; }

        // Pricing
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";

        // Availability
        public int AvailableSeats { get; set; }

        // Flight Details
        public int NumberOfStops { get; set; }
        public bool IsDirect => NumberOfStops == 0;
        public string? FlightNumber { get; set; }
        public string? FlightClass { get; set; }  // Economy, Business, PremiumEconomy
        public string? DestinationImageUrl { get; set; }
        public string Status { get; set; } = null!;
        public string? CreatedByUserName { get; set; }

        public List<FlightSegmentDto> Segments { get; set; } = new();
        public List<FlightLayoverDto> Layovers { get; set; } = new();
    }
}


