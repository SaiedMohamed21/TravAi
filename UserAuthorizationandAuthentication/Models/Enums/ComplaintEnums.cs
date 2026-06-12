namespace TravAi.Models.Enums
{
    public enum ComplaintType
    {
        Hotel = 1,
        Service = 2,
        Tour = 3,
        Airline = 4
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
