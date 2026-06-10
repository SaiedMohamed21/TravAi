using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models
{
    public class PaymentTransactionItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long PaymentTransactionId { get; set; }

        [ForeignKey("PaymentTransactionId")]
        public PaymentTransaction PaymentTransaction { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string BookingType { get; set; } = null!; // Hotel, Tour, Airline

        [Required]
        public long BookingId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "usd";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!; // Paid, Pending, etc.

        public DateTime CreatedAt { get; set; }
    }
}
