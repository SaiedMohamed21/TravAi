using System.Text.Json.Serialization;

namespace TravAi.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InboxCategory
    {
        FinancialTransaction = 1,
        WarningAlert = 2,
        UpcomingActionInstruction = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InboxStatus
    {
        Unread = 1,
        Read = 2,
        Pending = 3,
        Completed = 4,
        Archived = 5
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InboxSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InboxPriority
    {
        Normal = 1,
        High = 2,
        Urgent = 3
    }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HotelToAdminCategory
    {
        Complaint = 1,
        FinancialIssue = 2,
        CustomerIssue = 3,
        RefundRequest = 4,
        TechnicalSupport = 5,
        VerificationUpdate = 6,
        FeatureRequest = 7,
        GeneralInquiry = 8,
        Other = 9
    }
}
