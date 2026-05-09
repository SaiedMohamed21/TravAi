using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TravAi.Airline.Models.Airlines
{
    public class FlightSegment
    {
        public long Id { get; set; }

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        [JsonIgnore]
        public Flight Flight { get; set; } = null!;

        public int SegmentNumber { get; set; } 

        public string? Amenities { get; set; } 
        public double? LegroomInches { get; set; }

        public string? FromAirportCode { get; set; }
        [ForeignKey("FromAirportCode")]
        public Airport? FromAirport { get; set; }

        public string? ToAirportCode { get; set; }
        [ForeignKey("ToAirportCode")]
        public Airport? ToAirport { get; set; }

        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
    }
}
