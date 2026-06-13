using System.Collections.Generic;

namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class GuideManageSummaryDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<string> Languages { get; set; }
        public double Rating { get; set; }
        public int ToursCount { get; set; }
        public string Status { get; set; }
    }
}
