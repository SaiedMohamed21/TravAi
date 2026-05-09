using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Airline.Models.Airlines;

namespace TravAi.Airline.Models
{
    public class Review
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public long FlightId { get; set; }
        [ForeignKey("FlightId")]
        public Flight Flight { get; set; } = null!;

        public int Rating { get; set; } // 1 to 5
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}




