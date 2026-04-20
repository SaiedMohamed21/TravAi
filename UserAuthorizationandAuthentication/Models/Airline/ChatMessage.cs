using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Airline.Models.Airlines;

namespace UserAuthorizationandAuthentication.Airline.Models
{
    public class ChatMessage
    {
        [Key]
        public long Id { get; set; }

        public long BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        public long SenderId { get; set; }
        public virtual User Sender { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Helps identify if the sender is the user or a company representative
        public bool IsFromAdmin { get; set; } 
    }
}


