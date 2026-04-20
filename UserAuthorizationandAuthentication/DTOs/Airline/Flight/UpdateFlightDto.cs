using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Flight
{
    public class UpdateFlightDto
    {
        public decimal? Price { get; set; }
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int? AvailableSeats { get; set; }
    }
}


