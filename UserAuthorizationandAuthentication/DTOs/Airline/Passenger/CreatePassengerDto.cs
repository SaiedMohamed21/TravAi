using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Passenger
{
    public class CreatePassengerDto
    {
        [Required]
        public long BookingId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PassengerType { get; set; } = "Adult"; // Adult, Child

        [Required]
        [MaxLength(20)]
        public string AgeType { get; set; } = "Adult"; // Adult, Child, Infant

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public List<string> PhoneNumbers { get; set; } = new();
        public List<CreateEmergencyContactDto> EmergencyContacts { get; set; } = new();
    }

    public class CreateEmergencyContactDto
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string PhoneNumber { get; set; } = null!;
    }
}


