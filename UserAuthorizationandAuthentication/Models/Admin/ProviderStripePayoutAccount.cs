using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Admin;

namespace TravAi.Models.Admin
{
    [Table("admin_ProviderStripePayoutAccounts")]
    public class ProviderStripePayoutAccount
    {
        [Key]
        public long Id { get; set; }

        public ProviderType ProviderType { get; set; }
        public long ProviderId { get; set; }

        [MaxLength(255)]
        public string? ProviderNameSnapshot { get; set; }

        [MaxLength(50)]
        public string ProviderPayoutAccountNumber { get; set; } = string.Empty;

        [MaxLength(255)]
        public string StripeConnectedAccountId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? StripeAccountDisplayName { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        [MaxLength(255)]
        public string? BankName { get; set; }

        [MaxLength(10)]
        public string? BankLast4 { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
