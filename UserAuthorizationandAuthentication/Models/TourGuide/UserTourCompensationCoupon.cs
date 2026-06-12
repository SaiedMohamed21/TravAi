using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.TourGuide.Models
{
    public class UserTourCompensationCoupon
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        // The booking that was cancelled triggering this coupon
        public long TriggeringBookingId { get; set; }
        [ForeignKey("TriggeringBookingId")]
        public TourBooking TriggeringBooking { get; set; }

        [MaxLength(50)]
        public string CouponCode { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 5.0m;

        public bool IsUsed { get; set; } = false;

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
    }
}
