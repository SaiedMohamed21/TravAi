using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CancellationPolicy
    {
        Hours24 = 24,
        Hours48 = 48,
        Hours72 = 72
    }
}


