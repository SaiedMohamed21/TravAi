using System.ComponentModel.DataAnnotations;

namespace TravAi.DTOs.Hotel
{
    public class CreateReviewRequest
    {
        [Required]
        public long HotelId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class HotelReviewDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfilePicture { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty; // "2 days ago"
    }

    public class HotelReviewsResponse
    {
        public List<HotelReviewDto> Reviews { get; set; } = new List<HotelReviewDto>();
        public decimal AvgRating { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminHotelReviewDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaginatedAdminReviewsResponse
    {
        public List<AdminHotelReviewDto> Items { get; set; } = new List<AdminHotelReviewDto>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
