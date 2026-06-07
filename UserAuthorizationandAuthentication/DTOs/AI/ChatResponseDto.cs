using System.Text.Json.Serialization;

namespace TravAi.DTOs.AI
{
    /// <summary>
    /// Response DTO from the AI Chatbot.
    /// Matches the Python FastAPI ChatResponse schema exactly.
    /// </summary>
    public class ChatResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public ChatMessageDto? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("retrieved_tours")]
        public List<ChatTourCardDto>? RetrievedTours { get; set; }

        [JsonPropertyName("language_detected")]
        public string? LanguageDetected { get; set; }
    }

    public class ChatTourCardDto
    {
        [JsonPropertyName("tour_id")]
        public int TourId { get; set; }

        [JsonPropertyName("tour_title")]
        public string TourTitle { get; set; } = "";

        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("cluster_label")]
        public string ClusterLabel { get; set; } = "";

        [JsonPropertyName("base_price_usd")]
        public double BasePriceUsd { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("number_of_reviews")]
        public int NumberOfReviews { get; set; }

        [JsonPropertyName("duration_hours")]
        public double DurationHours { get; set; }

        [JsonPropertyName("transport_included")]
        public bool TransportIncluded { get; set; }

        [JsonPropertyName("meals_included")]
        public bool MealsIncluded { get; set; }

        [JsonPropertyName("quality_score")]
        public double QualityScore { get; set; }

        [JsonPropertyName("value_score")]
        public double ValueScore { get; set; }

        [JsonPropertyName("recommendation_reason")]
        public string? RecommendationReason { get; set; }

        [JsonPropertyName("languages_spoken")]
        public string? LanguagesSpoken { get; set; }

        [JsonPropertyName("accessibility")]
        public string? Accessibility { get; set; }

        [JsonPropertyName("guide_name")]
        public string? GuideName { get; set; }

        [JsonPropertyName("available_datetime")]
        public string? AvailableDatetime { get; set; }

        [JsonPropertyName("tour_description")]
        public string? TourDescription { get; set; }
    }
}
