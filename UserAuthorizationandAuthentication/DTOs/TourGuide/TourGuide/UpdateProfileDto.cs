using UserAuthorizationandAuthentication.TourGuide.Models;
using System.Collections.Generic;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
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



