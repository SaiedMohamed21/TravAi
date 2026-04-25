using System.Collections.Generic;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Tour
{
    /// <summary>
    /// Full tour details response for the mobile app Tour Details screen.
    /// Returned by GET /api/tours/{id}
    /// </summary>
    public class TourDetailsDto
    {
        public string Id { get; set; }
        public string Title { get; set; }

        /// <summary>All tour images (used in the UI image slider)</summary>
        public List<string> ImageUrls { get; set; } = new List<string>();

        public TourLocationDto Location { get; set; }

        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public decimal? TourScore { get; set; }

        public TourGuideInfoDto Guide { get; set; }

        /// <summary>Tour date in YYYY-MM-DD format</summary>
        public string? Date { get; set; }

        /// <summary>Tour start time in HH:mm format</summary>
        public string? StartTime { get; set; }

        /// <summary>Tour end time in HH:mm format (optional)</summary>
        public string? EndTime { get; set; }

        public int? DurationHours { get; set; }

        public int? GroupSize { get; set; }

        /// <summary>Available seats remaining (optional)</summary>
        public int? AvailableSeats { get; set; }

        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";

        public string? Description { get; set; }

        /// <summary>What is included in the tour (e.g. "Hotel pickup", "Private car")</summary>
        public List<string> Included { get; set; } = new List<string>();

        /// <summary>What is NOT included in the tour (e.g. "Lunch", "Tickets")</summary>
        public List<string> NotIncluded { get; set; } = new List<string>();
    }

    public class TourLocationDto
    {
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class TourGuideInfoDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Image { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
    }
}
