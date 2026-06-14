using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models.Admin
{
    [Table("admin_PayoutItems")]
    [Index(nameof(BookingType), nameof(BookingId), IsUnique = true)]
    public class PayoutItem
    {
        [Key]
        public long Id { get; set; }

        public long PayoutBatchId { get; set; }

        [ForeignKey("PayoutBatchId")]
        public PayoutBatch PayoutBatch { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string BookingType { get; set; } = null!; // Hotel, Tour, Airline

        public long BookingId { get; set; }

        public long? PaymentTransactionId { get; set; }

        public long? PaymentTransactionItemId { get; set; }

        public DateTime ServiceEndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAfterRefundAmount { get; set; }

        [MaxLength(1000)]
        public string? RefundReason { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProviderAmount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
