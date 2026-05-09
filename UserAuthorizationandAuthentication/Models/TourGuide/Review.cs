using TravAi.TourGuide.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.TourGuide.Models
{
    public class Review
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("TourGuide")]
        public long? TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        [ForeignKey("Tour")]
        public long? TourId { get; set; }
        public Tour Tour { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}



