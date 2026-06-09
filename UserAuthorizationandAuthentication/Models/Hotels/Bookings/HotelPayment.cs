using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.Models.Hotels;

namespace TravAi.Models.Hotels.Bookings
{
    public class HotelPayment
    {
        [Key]
        public long Id { get; set; }

        public long BookingId { get; set; }
        [ForeignKey("BookingId")]
        public HotelBooking Booking { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public decimal Amount { get; set; }

        public HotelPaymentMethod PaymentMethod { get; set; }

        [Required]
        public string TransactionId { get; set; }

        public string? PaymentReference { get; set; }

        public HotelPaymentStatus Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
