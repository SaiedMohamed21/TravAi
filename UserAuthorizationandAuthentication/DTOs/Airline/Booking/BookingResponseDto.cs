namespace UserAuthorizationandAuthentication.Airline.DTOs.Booking
{
    public class BookingResponseDto
    {
        public long Id { get; set; }
        public string? UserName { get; set; }
        public long FlightId { get; set; }
        public string AirlineName { get; set; } = null!;
        public string FromCode { get; set; } = null!;
        public string ToCode { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int NumberOfSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public List<UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto> Passengers { get; set; } = new();
    }
}


