using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Airline
{
    public class CreateAirlineDto
    {
        [Required]
        public string Name { get; set; } = null!;
        
        [Required]
        public long UserId { get; set; }

        [Required]
        public string Country { get; set; } = null!;
        
        public string? LogoUrl { get; set; }
    }
}


