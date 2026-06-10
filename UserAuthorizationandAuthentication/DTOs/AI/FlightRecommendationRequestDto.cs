using System.Text.Json.Serialization;

namespace TravAi.DTOs.AI
{
    public class FlightRecommendationRequestDto
    {
        [JsonPropertyName("flights")]
        public List<FlightRecommendationModelDto> Flights { get; set; } = new List<FlightRecommendationModelDto>();
    }

    public class FlightRecommendationModelDto
    {
        [JsonPropertyName("departure_city")]
        public string? DepartureCity { get; set; }

        [JsonPropertyName("arrival_city")]
        public string? ArrivalCity { get; set; }

        [JsonPropertyName("flight_class")]
        public string? FlightClass { get; set; }

        [JsonPropertyName("airline")]
        public string? Airline { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("stops")]
        public int Stops { get; set; }

        [JsonPropertyName("stop_1_airline")]
        public string? Stop1Airline { get; set; }

        [JsonPropertyName("stop_2_airline")]
        public string? Stop2Airline { get; set; }

        [JsonPropertyName("stop_3_airline")]
        public string? Stop3Airline { get; set; }

        [JsonPropertyName("Amenities_segment_1")]
        public string? AmenitiesSegment1 { get; set; }

        [JsonPropertyName("Amenities_segment_2")]
        public string? AmenitiesSegment2 { get; set; }

        [JsonPropertyName("Amenities_segment_3")]
        public string? AmenitiesSegment3 { get; set; }

        [JsonPropertyName("Legroom(inches)_segment_1")]
        public double? LegroomSegment1 { get; set; }

        [JsonPropertyName("Legroom(inches)_segment_2")]
        public double? LegroomSegment2 { get; set; }

        [JsonPropertyName("Legroom(inches)_segment_3")]
        public double? LegroomSegment3 { get; set; }

        [JsonPropertyName("price_USD")]
        public double PriceUsd { get; set; }

        [JsonPropertyName("route")]
        public string? Route { get; set; }

        [JsonPropertyName("departure_datetime")]
        public string? DepartureDatetime { get; set; }

        [JsonPropertyName("arrival_datetime")]
        public string? ArrivalDatetime { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }
    }

    public class FlightRecommendationResponseDto
    {
        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("flight")]
        public FlightRecommendationModelDto? Flight { get; set; }
    }

    public class FlightRegenerateRequestDto
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;
    }

    public class FlightRegenerateResponseDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("flight")]
        public FlightRecommendationModelDto? Flight { get; set; }
    }
}
