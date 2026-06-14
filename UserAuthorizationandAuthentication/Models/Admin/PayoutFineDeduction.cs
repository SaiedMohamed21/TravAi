using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models.Admin
{
    [Table("admin_PayoutFineDeductions")]
    [Index(nameof(ProviderFineId), IsUnique = true)]
    public class PayoutFineDeduction
    {
        [Key]
        public long Id { get; set; }

        public long PayoutBatchId { get; set; }

        [ForeignKey("PayoutBatchId")]
        public PayoutBatch PayoutBatch { get; set; } = null!;

        public long ProviderFineId { get; set; }

        [ForeignKey("ProviderFineId")]
        public ProviderFine ProviderFine { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(1000)]
        public string? ReasonSnapshot { get; set; }

        public DateTime FineCreatedAt { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
