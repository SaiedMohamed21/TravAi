using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models
{
    public class CheckoutSessionItem
    {
        [Key]
        public long Id { get; set; }

        public long CheckoutSessionId { get; set; }

        [ForeignKey("CheckoutSessionId")]
        public CheckoutSession CheckoutSession { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ItemType { get; set; } = null!; // AirlineBooking, HotelBooking, TourBooking

        public long ReferenceId { get; set; } // AirlineBookingId, HotelBookingId, or TourBookingId

        [MaxLength(255)]
        public string? DisplayName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
