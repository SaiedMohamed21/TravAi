namespace TravAi.DTOs.AI
{
    public class PlannedHotelDto
    {
        public long    Id             { get; set; }
        public string  HotelName     { get; set; } = null!;
        public string  City          { get; set; } = null!;
        public string? Country       { get; set; }
        public int?    StarRating    { get; set; }
        public decimal AvgReviewScore{ get; set; }
        public int     NumReviews    { get; set; }
        public string? ImageUrl      { get; set; }

        public int     Nights        { get; set; }
        public int     SingleRooms   { get; set; }
        public int     DoubleRooms   { get; set; }

        /// <summary>Price per night per single room (Full Board)</summary>
        public decimal SingleRoomPricePerNight { get; set; }
        /// <summary>Price per night per double room (Full Board)</summary>
        public decimal DoubleRoomPricePerNight { get; set; }
        /// <summary>Total hotel cost for all rooms and nights</summary>
        public decimal TotalPrice    { get; set; }
    }
}
