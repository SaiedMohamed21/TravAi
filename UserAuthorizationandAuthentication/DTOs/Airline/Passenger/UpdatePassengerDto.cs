using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Passenger
{
    public class UpdatePassengerDto
    {
        [MaxLength(20)]
        public string? PassengerType { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public DateTime? PassportExpiryDate { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }
    }
}


