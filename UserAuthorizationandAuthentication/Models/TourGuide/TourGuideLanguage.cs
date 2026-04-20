using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.Models
{
    public class TourGuideLanguage
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("TourGuide")]
        public long TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        [Required]
        public Language Language { get; set; }
    }
}



