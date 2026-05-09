using System.Text.Json.Serialization;

namespace TravAi.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WithdrawRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}


