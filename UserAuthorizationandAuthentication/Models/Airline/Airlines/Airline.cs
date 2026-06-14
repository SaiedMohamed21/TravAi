using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Airline.Models.Airlines
{
    public class Airline
    {
        public long Id { get; set; }
        
        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string? LicenseNumber { get; set; }
        public bool Verified { get; set; } = false;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public bool IsApproved { get; set; } = false; // Keeping this for backward compatibility or map to Status

        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
}




