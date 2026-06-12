using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravAi.DTOs.AI
{
    public class HotelRecommendationRequestDto
    {
        [JsonPropertyName("trip_plan")]
        public List<HotelTripPlanItemDto> TripPlan { get; set; } = new();

        [JsonPropertyName("cluster")]
        public string Cluster { get; set; } = string.Empty;

        [JsonPropertyName("total_budget")]
        public double TotalBudget { get; set; }

        [JsonPropertyName("num_people")]
        public int NumPeople { get; set; }

        [JsonPropertyName("single_rooms")]
        public int SingleRooms { get; set; }

        [JsonPropertyName("double_rooms")]
        public int DoubleRooms { get; set; }

        [JsonPropertyName("top_k_per_city")]
        public int TopKPerCity { get; set; } = 8;

        [JsonPropertyName("quality_threshold")]
        public double QualityThreshold { get; set; } = 0.45;

        [JsonPropertyName("regenerate_index")]
        public int RegenerateIndex { get; set; } = 0;
    }

    public class HotelTripPlanItemDto
    {
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("check_in")]
        public string CheckIn { get; set; } = string.Empty;

        [JsonPropertyName("check_out")]
        public string CheckOut { get; set; } = string.Empty;
    }

    public class HotelRecommendationResponseDto
    {
        [JsonPropertyName("hotels")]
        public List<HotelRecommendationItemDto> Hotels { get; set; } = new();

        [JsonPropertyName("budget")]
        public double Budget { get; set; }
    }

    public class HotelRecommendationItemDto
    {
        [JsonPropertyName("governorate")]
        public string? Governorate { get; set; }

        [JsonPropertyName("city_area")]
        public string? CityArea { get; set; }

        [JsonPropertyName("hotel_name")]
        public string? HotelName { get; set; }

        [JsonPropertyName("star_rating")]
        public int? StarRating { get; set; }

        [JsonPropertyName("num_reviews")]
        public int? NumReviews { get; set; }

        [JsonPropertyName("avg_review_score")]
        public double? AvgReviewScore { get; set; }

        [JsonPropertyName("amenities")]
        public string? Amenities { get; set; }

        [JsonPropertyName("normalized_type")]
        public string? NormalizedType { get; set; }

        [JsonPropertyName("cluster_segment")]
        public string? ClusterSegment { get; set; }

        [JsonPropertyName("check_in")]
        public string CheckIn { get; set; } = string.Empty;

        [JsonPropertyName("check_out")]
        public string CheckOut { get; set; } = string.Empty;
    }
}
