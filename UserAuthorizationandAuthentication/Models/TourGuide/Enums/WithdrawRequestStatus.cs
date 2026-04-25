using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WithdrawRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}


