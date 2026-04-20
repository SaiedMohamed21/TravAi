using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PropertyType
    {
        Hotel,
        Apartment,
        Resort,
        Villa,
        Guesthouse,
        Hostel,
        Motel
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccommodationType
    {
        Room,
        Suite,
        Studio,
        Apartment,
        Villa
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HotelRoomName
    {
        StandardRoom,
        DeluxeRoom,
        SuperiorRoom,
        Suite,
        ExecutiveRoom,
        FamilyRoom
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BedType
    {
        Single,
        Double
    }
}
