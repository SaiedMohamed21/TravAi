using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {
        Male,
        Female
    }
}
