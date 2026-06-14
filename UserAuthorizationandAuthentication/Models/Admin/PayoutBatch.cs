using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TravAi.Models.Auth;

namespace TravAi.Models.Admin
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PayoutBatchStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3,
        Cancelled = 4
    }

    [Table("admin_PayoutBatches")]
    public class PayoutBatch
    {
        [Key]
        public long Id { get; set; }

        public ProviderType ProviderType { get; set; }

        public long ProviderId { get; set; }

        [MaxLength(255)]
        public string? ProviderNameSnapshot { get; set; }

        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        public PayoutBatchStatus Status { get; set; } = PayoutBatchStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRefundAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAfterRefundAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFineDeductionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalPayoutAmount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public long? GeneratedByAdminUserId { get; set; }

        [ForeignKey("GeneratedByAdminUserId")]
        public User? GeneratedByAdminUser { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public long? ConfirmedByAdminUserId { get; set; }

        [ForeignKey("ConfirmedByAdminUserId")]
        public User? ConfirmedByAdminUser { get; set; }

        public DateTime? FailedAt { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public ICollection<PayoutItem> Items { get; set; } = new List<PayoutItem>();
        public ICollection<PayoutFineDeduction> Deductions { get; set; } = new List<PayoutFineDeduction>();
    }
}
