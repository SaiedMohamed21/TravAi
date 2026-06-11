using System;
using System.Collections.Generic;

namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class TourGuideApplicationDetailsDto
    {
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string LicenseId { get; set; }
        public int? ExperienceYears { get; set; }
        public string Certification { get; set; }
        
        public List<string> Emails { get; set; } = new List<string>();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public List<string> Cities { get; set; } = new List<string>();

        public string LicenseFileUrl { get; set; }
        public string LicenseFrontUrl { get; set; }
        public string LicenseBackUrl { get; set; }

        public string ApplicationStatus { get; set; }
        public string RejectionReason { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
