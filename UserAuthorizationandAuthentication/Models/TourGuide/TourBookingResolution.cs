using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.TourGuide.Models
{
    public class TourBookingResolution
    {
        [Key]
        public long Id { get; set; }

        public long OriginalBookingId { get; set; }
        [ForeignKey("OriginalBookingId")]
        public TourBooking OriginalBooking { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        // "Alternative", "Refund"
        [MaxLength(20)]
        public string ResolutionType { get; set; }

        public long? NewBookingId { get; set; }
        [ForeignKey("NewBookingId")]
        public TourBooking? NewBooking { get; set; }

        public long? SelectedAlternativeTourId { get; set; }
        [ForeignKey("SelectedAlternativeTourId")]
        public Tour? SelectedAlternativeTour { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
    }
}
