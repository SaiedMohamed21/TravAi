using TravAi.TourGuide.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.TourGuide.Models
{
    public class TourGuideEmail
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("TourGuide")]
        public long TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public bool EmailVerified { get; set; } = false;
    }
}



