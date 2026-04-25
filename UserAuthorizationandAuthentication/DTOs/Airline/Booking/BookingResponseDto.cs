namespace UserAuthorizationandAuthentication.Airline.DTOs.Booking
{
    public class BookingResponseDto
    {
        public long Id { get; set; }
        public string? UserName { get; set; }
        public long? FlightId { get; set; }
        public string? AirlineName { get; set; }
        public string? FromCode { get; set; }
        public string? ToCode { get; set; }
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public int? NumberOfSeats { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Status { get; set; }
        public string? RejectionReason { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? BookingDate { get; set; }
        public string? FlightNumber { get; set; }
        public string? RouteTitle { get; set; }
        public string? UiBadge { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReview { get; set; }
        public string? BoardingTime { get; set; }
        public List<UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto> Passengers { get; set; } = new();
    }
}
