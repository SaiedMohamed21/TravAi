using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models
{
    public class PaymentTransaction
    {
        [Key]
        public long Id { get; set; }

        public long CheckoutSessionId { get; set; }

        [ForeignKey("CheckoutSessionId")]
        public CheckoutSession CheckoutSession { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Stripe";

        [MaxLength(255)]
        public string? ProviderTransactionId { get; set; } // Stripe PaymentIntent Id if available

        [MaxLength(255)]
        public string? ProviderCheckoutSessionId { get; set; } // Stripe Checkout Session Id

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!; // Pending, Paid, Failed, Refunded, Cancelled, Expired

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? RawProviderResponse { get; set; }
    }
}
