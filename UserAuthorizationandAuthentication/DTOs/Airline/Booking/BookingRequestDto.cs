using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Booking
{
    public class BookingRequestDto
    {
        [Required]
        public long FlightId { get; set; }

        [Required]
        [Range(1, 100)]
        public int NumberOfSeats { get; set; }

        [Required]
        public List<long> CompanionIds { get; set; } = new();
    }
}


