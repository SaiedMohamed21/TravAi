using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravAi.DTOs.AI
{
    public class TourRecommendationRequestDto
    {
        [JsonPropertyName("budget_type")]
        public string BudgetType { get; set; } = string.Empty;

        [JsonPropertyName("cities")]
        public List<string> Cities { get; set; } = new List<string>();

        [JsonPropertyName("days")]
        public List<int> Days { get; set; } = new List<int>();

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = string.Empty;

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = string.Empty;

        [JsonPropertyName("preferences")]
        public List<string> Preferences { get; set; } = new List<string>();

        [JsonPropertyName("tour_budget")]
        public decimal TourBudget { get; set; }

        [JsonPropertyName("travelers")]
        public int Travelers { get; set; }
    }

    public class TourRecommendationResponseDto
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public List<TourDailyRecommendationDto> Recommendations { get; set; } = new List<TourDailyRecommendationDto>();
    }

    public class TourDailyRecommendationDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("tour")]
        public RecommendedTourItemDto? Tour { get; set; }
    }

    public class RecommendedTourItemDto
    {
        [JsonPropertyName("tour_id")]
        public long TourId { get; set; }

        [JsonPropertyName("tour_title")]
        public string? TourTitle { get; set; }

        [JsonPropertyName("base_price_usd")]
        public decimal BasePriceUsd { get; set; }

        [JsonPropertyName("guide_name")]
        public string? GuideName { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("number_of_reviews")]
        public int? NumberOfReviews { get; set; }

        [JsonPropertyName("duration_hours")]
        public double? DurationHours { get; set; }
    }

    public class RegenerateTourApiRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public List<string> FixedDates { get; set; } = new List<string>();
        public int TotalPeople { get; set; }
    }

    public class RegenerateTourPythonRequestDto
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("fixed_dates")]
        public List<string> FixedDates { get; set; } = new List<string>();
    }

    public class RegenerateTourPythonResponseDto
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public List<TourDailyRecommendationDto> Recommendations { get; set; } = new();
    }
}
