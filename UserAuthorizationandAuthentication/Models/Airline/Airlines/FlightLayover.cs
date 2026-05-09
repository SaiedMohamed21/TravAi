using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TravAi.Airline.Models.Airlines
{
    public class FlightLayover
    {
        public long Id { get; set; }

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        [JsonIgnore]
        public Flight Flight { get; set; } = null!;

        public int LayoverOrder { get; set; } 
        public string AirportName { get; set; } = null!;
        public string? DurationString { get; set; } 
    }
}
