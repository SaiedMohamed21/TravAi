using System.ComponentModel.DataAnnotations.Schema;

namespace UserAuthorizationandAuthentication.Airline.Models.Airlines
{
    public class FlightSegment
    {
        public long Id { get; set; }

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; } = null!;

        public int SegmentNumber { get; set; } 

        public string? Amenities { get; set; } 
        public double? LegroomInches { get; set; }
    }
}
