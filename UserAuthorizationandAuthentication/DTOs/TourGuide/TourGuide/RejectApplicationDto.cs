using TravAi.TourGuide.Models;
using System.ComponentModel.DataAnnotations;

namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class RejectApplicationDto
    {
        [Required]
        public string Reason { get; set; }
    }
}



