using UserAuthorizationandAuthentication.TourGuide.Models;
using System.Collections.Generic;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
{
    public class TourGuideResponseDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } // From User able
        public string Name { get; set; }
        public string Bio { get; set; }
        public string LicenseId { get; set; }
        public string LicenseCard { get; set; }
        public string LicenseIdFrontPhoto { get; set; }
        public string LicenseIdBackPhoto { get; set; }
        public string Certification { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public int? ExperienceYears { get; set; }
        
        public List<TourGuideEmailDto> Emails { get; set; }
        public List<TourGuidePhoneDto> Phones { get; set; }
        public List<TourGuideLanguageDto> Languages { get; set; }
        public List<TourGuideCityDto> Cities { get; set; }
    }

    public class TourGuideEmailDto
    {
        public string Email { get; set; }
        public bool Verified { get; set; }
    }

    public class TourGuidePhoneDto
    {
        public string PhoneNumber { get; set; }
        public bool Verified { get; set; }
    }

    public class TourGuideLanguageDto
    {
        public Language Language { get; set; }
    }

    public class TourGuideCityDto
    {
        public string City { get; set; }
    }
}



