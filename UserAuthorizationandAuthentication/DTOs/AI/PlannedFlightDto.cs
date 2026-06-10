namespace TravAi.DTOs.AI
{
    public class PlannedFlightDto
    {
        public long   Id                  { get; set; }
        public string FlightNumber        { get; set; } = null!;
        public string AirlineName         { get; set; } = null!;
        public string DepartureAirportCode{ get; set; } = null!;
        public string DepartureCity       { get; set; } = null!;
        public string ArrivalAirportCode  { get; set; } = null!;
        public string ArrivalCity         { get; set; } = null!;
        public DateTime? DepartureTime    { get; set; }
        public DateTime? ArrivalTime      { get; set; }
        public string? FlightClass        { get; set; }
        public string? Duration           { get; set; }
        public int?   NumberOfStops       { get; set; }
        public string? DestinationImageUrl{ get; set; }

        /// <summary>Price per adult</summary>
        public decimal PricePerAdult  { get; set; }
        /// <summary>Price per child (75% of adult)</summary>
        public decimal PricePerChild  { get; set; }
        /// <summary>Total price for all passengers</summary>
        public decimal TotalPrice     { get; set; }

        /// <summary>Go | Return</summary>
        public string Direction { get; set; } = null!;

        // Used for AI Regenerate requests
        public string? SessionId { get; set; }
    }
}
