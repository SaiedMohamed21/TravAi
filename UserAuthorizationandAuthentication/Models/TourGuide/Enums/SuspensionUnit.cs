using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SuspensionUnit
    {
        Hours,
        Days,
        Weeks,
        Years
    }
}


