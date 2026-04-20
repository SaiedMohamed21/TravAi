namespace UserAuthorizationandAuthentication.Airline.DTOs.Review
{
    public class ReviewResponseDto
    {
        public long Id { get; set; }
        public long FlightId { get; set; }
        public string UserName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}


