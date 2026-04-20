namespace UserAuthorizationandAuthentication.Models.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded,
        Cancelled
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        CheckedIn,
        CheckedOut,
        Cancelled,
        NoShow,
        Completed
    }

    public enum RoomState
    {
        Available,
        Booked,
        Active,
        Inactive
    }
}
