using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Airline.Models.Airlines;

namespace UserAuthorizationandAuthentication.Airline.Models
{
    public class Booking
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; } = null!;

        public int NumberOfSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public string? RejectionReason { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded

        public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
    }
}




