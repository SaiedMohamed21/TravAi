using TravAi.TourGuide.Models;
using System;

namespace TravAi.TourGuide.DTOs.Tour
{
    public class TourFilterDto
    {
        public string? Search { get; set; }

        // Price filters
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Date filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? Date { get; set; } // Added for mobile

        // Language filter (comma-separated)
        public string? Languages { get; set; }
        public string? Language { get; set; } // Added for mobile

        // City filter
        public string? City { get; set; }

        // Tour type filter
        public string? TourType { get; set; }

        // Sorting
        public string? SortBy { get; set; } // "price_asc", "price_desc", "rating", "popular", "recent"

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}



