using UserAuthorizationandAuthentication.TourGuide.Models;
using System;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Tour
{
    public class TourFilterDto
    {
        // Price filters
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Date filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Language filter (comma-separated)
        public string? Languages { get; set; }

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



