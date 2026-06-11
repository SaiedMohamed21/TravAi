using System;
using System.Collections.Generic;

namespace TravAi.TourGuide.DTOs.Review
{
    public class TourGuideReviewsDashboardDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public RatingBreakdownDto RatingBreakdown { get; set; } = new RatingBreakdownDto();
        public List<TourGuideDashboardReviewDto> Reviews { get; set; } = new List<TourGuideDashboardReviewDto>();
    }

    public class RatingBreakdownDto
    {
        public int FiveStars { get; set; }
        public int FourStars { get; set; }
        public int ThreeStars { get; set; }
        public int TwoStars { get; set; }
        public int OneStars { get; set; }
    }

    public class TourGuideDashboardReviewDto
    {
        public long Id { get; set; }
        public long? TourId { get; set; }
        public string? TourTitle { get; set; }
        public string ReviewerName { get; set; } = null!;
        public string? ReviewerImage { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
