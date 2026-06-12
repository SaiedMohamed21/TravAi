using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Review
{
    public class ReviewRequestDto
    {
        [Required]
        public long FlightId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class UpdateReviewRequestDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}


