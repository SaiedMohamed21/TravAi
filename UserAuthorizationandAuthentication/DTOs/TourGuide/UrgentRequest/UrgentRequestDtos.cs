using UserAuthorizationandAuthentication.TourGuide.Models;
using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.UrgentRequest
{
    public class CreateUrgentRequestDto
    {
        [Required]
        public long TourId { get; set; }

        [Required]
        public string Reason { get; set; }

        public string? DocumentationUrl { get; set; }
    }

    public class AdminProcessUrgentRequestDto
    {
        [Required]
        public string Status { get; set; } // Approved, Rejected
        
        public string? AdminNotes { get; set; }
    }
}



