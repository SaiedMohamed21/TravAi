using System.Text.Json.Serialization;

namespace TravAi.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed,
        PendingUserDecision
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParticipantType
    {
        Adult,
        Child,
        Infant
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AgeType
    {
        Adult,
        Child,
        Infant,
        Senior
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PayoutStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}


