using System.ComponentModel.DataAnnotations.Schema;

namespace UserAuthorizationandAuthentication.Airline.Models.Airlines
{
    public class FlightLayover
    {
        public long Id { get; set; }

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; } = null!;

        public int LayoverOrder { get; set; } 
        public string AirportName { get; set; } = null!;
        public string? DurationString { get; set; } 
    }
}
