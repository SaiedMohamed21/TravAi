using TravAi.TourGuide.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.TourGuide.Models
{
    public class TourImage
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("Tour")]
        public long TourId { get; set; }
        public Tour Tour { get; set; }

        [Required]
        public string ImageUrl { get; set; }
        
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}



