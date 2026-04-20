namespace UserAuthorizationandAuthentication.Airline.DTOs.Passenger
{
    public class PassengerResponseDto
    {
        public long Id { get; set; }
        public long BookingId { get; set; }
        public string PassengerType { get; set; } = null!;
        public string AgeType { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public string? RejectionReason { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }
        public List<string> PhoneNumbers { get; set; } = new();
        public List<EmergencyContactResponseDto> EmergencyContacts { get; set; } = new();
    }

    public class EmergencyContactResponseDto
    {
        public string Name { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }
}


