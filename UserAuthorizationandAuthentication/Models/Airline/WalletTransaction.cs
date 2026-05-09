using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Airline.Models
{
    public class WalletTransaction
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = null!; // "Deposit", "Withdrawal", "Refund"

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? ReferenceId { get; set; } // BookingId or PaymentTransactionId
    }
}




