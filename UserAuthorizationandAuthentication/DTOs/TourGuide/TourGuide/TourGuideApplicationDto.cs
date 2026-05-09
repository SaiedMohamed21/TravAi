using TravAi.TourGuide.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class TourGuideApplicationDto
    {
        [Required]
        public string Name { get; set; }
        public string? Bio { get; set; }
        public string? LicenseId { get; set; }
        public string? LicenseCard { get; set; }
        public string? LicenseIdFrontPhoto { get; set; }
        public string? LicenseIdBackPhoto { get; set; }
        public string? Certification { get; set; }
        public int? ExperienceYears { get; set; }

        public List<string> Emails { get; set; } = new List<string>();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<Language> Languages { get; set; } = new List<Language>();
        public List<string> Cities { get; set; } = new List<string>();
    }
}



