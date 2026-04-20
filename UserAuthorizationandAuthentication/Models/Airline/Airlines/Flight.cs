using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System.ComponentModel.DataAnnotations.Schema;
namespace UserAuthorizationandAuthentication.Airline.Models.Airlines
{
    public class Flight
    {
        public long Id { get; set; }

        [ForeignKey("DepartureAirport")]
        public string DepartureAirportCode { get; set; } = null!;
        public Airport DepartureAirport { get; set; } = null!;

        [ForeignKey("ArrivalAirport")]
        public string ArrivalAirportCode { get; set; } = null!;
        public Airport ArrivalAirport { get; set; } = null!;

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }

        public long AirlineId { get; set; }
        public Airline Airline { get; set; } = null!;

        public int NumberOfStops { get; set; } = 0; // 0 = Direct, 1 = 1 Stop, etc.
        public string? FlightNumber { get; set; }
        public string? DestinationImageUrl { get; set; }
        public string? FlightClass { get; set; } // Economy, Business, PremiumEconomy

        public string Status { get; set; } = "Active"; // Active, Cancelled, Delayed

        public long? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }

        public ICollection<FlightSegment> Segments { get; set; } = new List<FlightSegment>();
        public ICollection<FlightLayover> Layovers { get; set; } = new List<FlightLayover>();
    }
}


