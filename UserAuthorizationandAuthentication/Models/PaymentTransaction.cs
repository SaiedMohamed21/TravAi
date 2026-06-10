using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.Models
{
    public class PaymentTransaction
    {
        [Key]
        public long Id { get; set; }

        public long CheckoutSessionId { get; set; }

        [ForeignKey("CheckoutSessionId")]
        public CheckoutSession CheckoutSession { get; set; } = null!;

        public long? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Stripe";

        [MaxLength(255)]
        public string? ProviderTransactionId { get; set; } // Stripe PaymentIntent Id if available

        [MaxLength(255)]
        public string? ProviderCheckoutSessionId { get; set; } // Stripe Checkout Session Id

        [MaxLength(255)]
        public string? StripeSessionId { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } = "Stripe";

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!; // Pending, Paid, Failed, Refunded, Cancelled, Expired

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? RawProviderResponse { get; set; }

        public ICollection<PaymentTransactionItem> Items { get; set; } = new List<PaymentTransactionItem>();
    }
}

