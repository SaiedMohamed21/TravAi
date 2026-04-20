using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Airport
{
    public class AirportDto
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Country { get; set; } = null!;
    }

    public class CreateAirportDto
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Code { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string City { get; set; } = null!;

        [Required]
        public string Country { get; set; } = null!;
    }
}


