using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.TourGuide.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Language
    {
        English,
        Spanish,
        French,
        German,
        Italian,
        Portuguese,
        Russian,
        Japanese,
        Chinese,
        Arabic,
        Hindi,
        Bengali,
        Turkish,
        Korean,
        Dutch,
        Swedish,
        Norwegian,
        Danish,
        Finnish,
        Polish,
        Greek,
        Hebrew,
        Thai,
        Vietnamese,
        Indonesian
    }
}


