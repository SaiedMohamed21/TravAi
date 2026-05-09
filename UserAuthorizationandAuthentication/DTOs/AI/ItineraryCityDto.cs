namespace TravAi.DTOs.AI
{
    public class ItineraryCityDto
    {
        /// <summary>City name (matches Hotel.Governorate or Tour.City)</summary>
        public string City { get; set; } = null!;

        /// <summary>Number of days to spend in this city</summary>
        public int Days { get; set; }
    }
}
