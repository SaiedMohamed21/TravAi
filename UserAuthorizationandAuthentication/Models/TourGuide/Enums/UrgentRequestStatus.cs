using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UrgentRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}


