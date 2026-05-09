using System.Text.Json.Serialization;

namespace TravAi.Airline.DTOs.Flight
{
    public class FlightSegmentDto
    {
        [JsonPropertyName("segment_id")]
        public string SegmentId { get; set; } = null!;

        [JsonPropertyName("from")]
        public AirportInfoDto From { get; set; } = null!;

        [JsonPropertyName("to")]
        public AirportInfoDto To { get; set; } = null!;

        [JsonPropertyName("departure_time")]
        public DateTime? DepartureTime { get; set; }

        [JsonPropertyName("arrival_time")]
        public DateTime? ArrivalTime { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("layover")]
        public string? Layover { get; set; }

        [JsonPropertyName("amenities")]
        public List<string> Amenities { get; set; } = new();

        [JsonIgnore]
        public int? SegmentNumber { get; set; }
        [JsonIgnore]
        public double? LegroomInches { get; set; }
    }

    public class AirportInfoDto
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        [JsonPropertyName("city")]
        public string? City { get; set; }
    }
}
