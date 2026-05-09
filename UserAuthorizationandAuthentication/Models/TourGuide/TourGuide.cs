using TravAi.TourGuide.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;

namespace TravAi.TourGuide.Models
{
    public class TourGuide
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Bio { get; set; }

        public string? LicenseId { get; set; }
        public string? LicenseCard { get; set; }
        public string? LicenseIdFrontPhoto { get; set; }
        public string? LicenseIdBackPhoto { get; set; }
        public string? Certification { get; set; }
        public string? RejectionReason { get; set; }

        public TourGuideStatus Status { get; set; } = TourGuideStatus.Pending;
        public int? ExperienceYears { get; set; }
        public DateTime? SuspendedUntil { get; set; }

        public ICollection<TourGuideEmail> TourGuideEmails { get; set; } = new List<TourGuideEmail>();
        public ICollection<TourGuidePhone> TourGuidePhones { get; set; } = new List<TourGuidePhone>();
        public ICollection<TourGuideLanguage> TourGuideLanguages { get; set; } = new List<TourGuideLanguage>();
        public ICollection<TourGuideCity> TourGuideCities { get; set; } = new List<TourGuideCity>();
    }
}



