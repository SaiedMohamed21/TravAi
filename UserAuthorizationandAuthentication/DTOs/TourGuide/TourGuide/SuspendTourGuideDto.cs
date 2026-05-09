using TravAi.TourGuide.Models;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.TourGuide.DTOs.TourGuide
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



