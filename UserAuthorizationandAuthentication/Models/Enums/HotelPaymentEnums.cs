namespace TravAi.Models.Enums
{
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum HotelPaymentMethod
    {
        CreditCard,
        VodafoneCash,
        Instapay,
        Visa,
        MasterCard,
        Cash
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum HotelPaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Cancelled,
        Refunded
    }
}
