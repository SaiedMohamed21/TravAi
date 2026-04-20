using UserAuthorizationandAuthentication.TourGuide.Models;
using System.ComponentModel.DataAnnotations;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
{
    public class SuspendTourGuideDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Duration { get; set; }

        [Required]
        public SuspensionUnit Unit { get; set; }
    }
}



