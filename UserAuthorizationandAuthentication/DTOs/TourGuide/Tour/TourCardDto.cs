using System;
using System.Collections.Generic;

namespace TravAi.TourGuide.DTOs.Tour
{
    public class TourCardDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public string GuideName { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        public TourLocationDto Location { get; set; }
        public string? Date { get; set; } // YYYY-MM-DD
        public string? StartTime { get; set; } // HH:mm
        public string? EndTime { get; set; } // HH:mm
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public decimal? TourScore { get; set; }
    }
}

