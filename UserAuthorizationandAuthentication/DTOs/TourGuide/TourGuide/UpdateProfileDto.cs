using TravAi.TourGuide.Models;
using System.Collections.Generic;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class UpdateProfileDto
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Certification { get; set; }
        public int? ExperienceYears { get; set; }

        public List<string>? Emails { get; set; }
        public List<string>? PhoneNumbers { get; set; }
        public List<Language>? Languages { get; set; }
        public List<string>? Cities { get; set; }
    }
}



