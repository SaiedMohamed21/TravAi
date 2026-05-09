using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Airline.Models.Airlines
{
    public class Flight
    {
        public long Id { get; set; }

        [ForeignKey("DepartureAirport")]
        public string? DepartureAirportCode { get; set; }
        public Airport? DepartureAirport { get; set; }

        [ForeignKey("ArrivalAirport")]
        public string? ArrivalAirportCode { get; set; }
        public Airport? ArrivalAirport { get; set; }

        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }

        public decimal? Price { get; set; }
        public int? AvailableSeats { get; set; } = 100;

        public long? AirlineId { get; set; }
        public Airline? Airline { get; set; }

        public int? NumberOfStops { get; set; }
        public string? FlightNumber { get; set; }
        public string? DestinationImageUrl { get; set; }
        public string? FlightClass { get; set; }
        public string Currency { get; set; } = "USD";
        
        public string? Duration { get; set; }
        public int? DurationMinutes { get; set; }

        public string Status { get; set; } = "Active";

        public long? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }

        public ICollection<FlightSegment> Segments { get; set; } = new List<FlightSegment>();
        public ICollection<FlightLayover> Layovers { get; set; } = new List<FlightLayover>();
    }
}
