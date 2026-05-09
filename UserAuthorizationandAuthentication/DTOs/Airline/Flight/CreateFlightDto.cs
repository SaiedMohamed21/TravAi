using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Flight
{
    public class CreateFlightDto
    {
        [Required]
        public string DepartureAirportCode { get; set; } = null!;

        [Required]
        public string ArrivalAirportCode { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public long AirlineId { get; set; }
    }
}


