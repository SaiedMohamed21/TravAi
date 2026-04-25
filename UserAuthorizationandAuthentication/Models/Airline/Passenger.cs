using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAuthorizationandAuthentication.Airline.Models
{
    public class Passenger
    {
        [Key]
        public long Id { get; set; }

        public long BookingId { get; set; }
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [MaxLength(20)]
        public string PassengerType { get; set; } = "Adult"; // Adult, Child

        [MaxLength(20)]
        public string AgeType { get; set; } = "Adult"; // Adult, Child, Infant

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? RejectionReason { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }

        public ICollection<PassengerPhone> Phones { get; set; } = new List<PassengerPhone>();
        public ICollection<PassengerEmergencyContact> EmergencyContacts { get; set; } = new List<PassengerEmergencyContact>();
    }
}


