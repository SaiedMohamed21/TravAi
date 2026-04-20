namespace UserAuthorizationandAuthentication.Airline.DTOs.Flight
{
    public class FlightSearchDto
    {
        // Basic Search
        public string? From { get; set; }
        public string? To { get; set; }
        public DateTime? Date { get; set; }
        public int? Passengers { get; set; }  // Min available seats
        public string? Class { get; set; }    // Economy, Business, PremiumEconomy

        // Price Filters
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Stops Filter
        public int? MaxStops { get; set; } // 0 = Direct, 1 = 1 Stop, etc.

        // Airlines Filter
        public List<long>? AirlineIds { get; set; }

        // Time Filters
        public TimeSpan? EarliestDeparture { get; set; }
        public TimeSpan? LatestDeparture { get; set; }

        // Sorting
        public string? SortBy { get; set; } // "price", "duration", "departure"
        public string? SortOrder { get; set; } = "asc"; // "asc" or "desc"

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}


