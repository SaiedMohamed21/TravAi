using System.Text.Json.Serialization;

namespace TravAi.Airline.DTOs.Flight
{
    public class FlightResultDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("flightId")]
        public string? FlightId { get; set; }

        [JsonPropertyName("airline")]
        public AirlineInfoDto Airline { get; set; } = null!;

        [JsonPropertyName("airlineName")]
        public string? AirlineName { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";

        [JsonPropertyName("cabin_class")]
        public string? CabinClass { get; set; }

        [JsonPropertyName("total_duration")]
        public string TotalDuration { get; set; } = null!;

        [JsonPropertyName("segments")]
        public List<FlightSegmentDto> Segments { get; set; } = new();

        [JsonPropertyName("fromCode")]
        public string? FromCode { get; set; }

        [JsonPropertyName("toCode")]
        public string? ToCode { get; set; }

        [JsonPropertyName("fromCity")]
        public string? FromCity { get; set; }

        [JsonPropertyName("toCity")]
        public string? ToCity { get; set; }

        [JsonPropertyName("fromCountry")]
        public string? FromCountry { get; set; }

        [JsonPropertyName("toCountry")]
        public string? ToCountry { get; set; }

        [JsonPropertyName("departureTime")]
        public DateTime? DepartureTime { get; set; }

        [JsonPropertyName("arrivalTime")]
        public DateTime? ArrivalTime { get; set; }

        [JsonPropertyName("availableSeats")]
        public int? AvailableSeats { get; set; }

        [JsonPropertyName("numberOfStops")]
        public int? NumberOfStops { get; set; }

        [JsonPropertyName("flightNumber")]
        public string? FlightNumber { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("destinationImageUrl")]
        public string? DestinationImageUrl { get; set; }
    }

    public class AirlineInfoDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
    }
}
