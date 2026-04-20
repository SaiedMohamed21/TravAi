using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserStatus
    {
        Pending,
        Active,
        Inactive,
        Suspended,
        Banned
    }
}
