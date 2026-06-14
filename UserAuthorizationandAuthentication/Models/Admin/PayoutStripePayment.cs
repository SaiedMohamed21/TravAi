using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TravAi.Models.Admin
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StripePaymentStatus
    {
        Created = 1,
        Paid = 2,
        Failed = 3,
        Cancelled = 4,
        Skipped = 5
    }

    [Table("admin_PayoutStripePayments")]
    public class PayoutStripePayment
    {
        [Key]
        public long Id { get; set; }

        public long PayoutBatchId { get; set; }

        [ForeignKey("PayoutBatchId")]
        public PayoutBatch? PayoutBatch { get; set; }

        public long ProviderStripePayoutAccountId { get; set; }

        [ForeignKey("ProviderStripePayoutAccountId")]
        public ProviderStripePayoutAccount? ProviderStripePayoutAccount { get; set; }

        public ProviderType ProviderType { get; set; }
        public long ProviderId { get; set; }

        [MaxLength(255)]
        public string StripeConnectedAccountId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string StripeCheckoutSessionId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        public StripePaymentStatus Status { get; set; } = StripePaymentStatus.Created;

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public long? CreatedByAdminUserId { get; set; }
    }
}
