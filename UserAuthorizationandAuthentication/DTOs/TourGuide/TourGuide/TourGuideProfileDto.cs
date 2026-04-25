using System.Collections.Generic;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide
{
    public class TourGuideProfileDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Image { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public int? ExperienceYears { get; set; }
    }
}
