using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.Models
{
    public class Airport
    {
        [Key]
        [MaxLength(3)]
        public string Code { get; set; } = null!; // CAI, DXB

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string City { get; set; } = null!;

        [MaxLength(100)]
        public string Country { get; set; } = null!;
    }
}


