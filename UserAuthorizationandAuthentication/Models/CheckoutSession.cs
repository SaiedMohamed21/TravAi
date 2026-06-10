using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.Models
{
    public class CheckoutSession
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string CheckoutType { get; set; } = null!; // Airline, HotelTour

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!; // Pending, Paid, Expired, Cancelled, Failed

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        [MaxLength(255)]
        public string? StripeCheckoutSessionId { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        public ICollection<CheckoutSessionItem> Items { get; set; } = new List<CheckoutSessionItem>();

        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}
