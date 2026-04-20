using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;
using PaymentStatus = UserAuthorizationandAuthentication.TourGuide.Models.Enums.PaymentStatus;

namespace UserAuthorizationandAuthentication.TourGuide.Models
{
    public class TourBookingPayment
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Booking")]
        public long BookingId { get; set; }
        public TourBooking Booking { get; set; }

        public string? StripePaymentIntentId { get; set; }
        public long? UserSavedCardId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        
        public string Currency { get; set; } = "USD";
        
        [Column(TypeName = "nvarchar(20)")]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformCommission { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProviderNetAmount { get; set; }
        
        [Column(TypeName = "nvarchar(20)")]
        public PayoutStatus PayoutStatus { get; set; } = PayoutStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}



