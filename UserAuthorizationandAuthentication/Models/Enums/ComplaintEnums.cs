namespace TravAi.Models.Enums
{
    public enum ComplaintType
    {
        Booking = 1,
        Platform = 2
    }

    public enum ComplaintStatus
    {
        Pending = 1,
        InReview = 2,
        Resolved = 3,
        Rejected = 4,
        Closed = 5
    }

    public enum ComplaintPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}
