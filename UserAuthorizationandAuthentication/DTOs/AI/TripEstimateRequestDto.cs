namespace TravAi.DTOs.AI
{
    public class TripEstimateRequestDto
    {
        // --- Trip Details ---
        public string FromCity    { get; set; } = null!;  // departure city (Airport.City)
        public string FromCountry { get; set; } = null!;
        public string ToCity      { get; set; } = null!;  // main destination (first city if itinerary)
        public string ToCountry   { get; set; } = null!;
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate    { get; set; }

        // --- Travelers & Rooms ---
        public int Adults      { get; set; } = 1;
        public int Children    { get; set; } = 0;
        public int SingleRooms { get; set; } = 0;
        public int DoubleRooms { get; set; } = 1;

        // --- Tourist Language (for Tour Guide filter) ---
        public string TouristLanguage { get; set; } = "English";

        // --- Preferences ---
        public bool ExcludeFlights  { get; set; } = false;
        public bool ExcludeHotels   { get; set; } = false;
        public bool ExcludeTours    { get; set; } = false;

        // --- Multi-city Itinerary (optional) ---
        // If null → single city trip to ToCity for the full duration
        // If provided → sum of Days must equal (ReturnDate - DepartureDate).Days
        public List<ItineraryCityDto>? Itinerary { get; set; }
    }
}
