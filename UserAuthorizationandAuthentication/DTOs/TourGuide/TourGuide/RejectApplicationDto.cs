using UserAuthorizationandAuthentication.TourGuide.Models;
using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
{
    public class RejectApplicationDto
    {
        [Required]
        public string Reason { get; set; }
    }
}



