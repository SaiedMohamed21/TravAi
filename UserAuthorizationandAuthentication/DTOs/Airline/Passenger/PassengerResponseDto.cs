namespace TravAi.Airline.DTOs.Passenger
{
    public class PassengerResponseDto
    {
        public long Id { get; set; }
        public long? BookingId { get; set; }
        public string? PassengerType { get; set; }
        public string? AgeType { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public decimal? Price { get; set; }
        public string? Status { get; set; }
        public string? RejectionReason { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }
        public List<string> PhoneNumbers { get; set; } = new();
        public List<EmergencyContactResponseDto> EmergencyContacts { get; set; } = new();
    }

    public class EmergencyContactResponseDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
