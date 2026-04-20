namespace UserAuthorizationandAuthentication.Airline.DTOs.Flight
{
    public class FlightLayoverDto
    {
        public int LayoverOrder { get; set; }
        public string AirportName { get; set; } = null!;
        public string? DurationString { get; set; }
    }
}
