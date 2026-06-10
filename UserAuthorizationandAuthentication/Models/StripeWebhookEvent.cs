using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models
{
    public class StripeWebhookEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string StripeEventId { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string EventType { get; set; } = null!;

        public long? CheckoutSessionId { get; set; }

        [ForeignKey("CheckoutSessionId")]
        public CheckoutSession? CheckoutSession { get; set; }

        public long? PaymentTransactionId { get; set; }

        [ForeignKey("PaymentTransactionId")]
        public PaymentTransaction? PaymentTransaction { get; set; }

        public string? RawJson { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
