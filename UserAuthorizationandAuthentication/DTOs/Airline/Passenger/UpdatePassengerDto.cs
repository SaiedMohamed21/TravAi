using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Passenger
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

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }
    }
}


