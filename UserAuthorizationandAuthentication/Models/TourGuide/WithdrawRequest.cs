using TravAi.TourGuide.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;

namespace TravAi.TourGuide.Models
{
    public class WithdrawRequest
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("TourGuide")]
        public long TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public WithdrawRequestStatus Status { get; set; } = WithdrawRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        
        public string? AdminNotes { get; set; }
    }
}



