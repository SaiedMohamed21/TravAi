using TravAi.Models.Enums;
using TravAi.Models;
using TravAi.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Airline.Models
{
    public class UserCompanion
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string AgeType { get; set; } = "Adult"; // Adult, Child, Infant

        [MaxLength(50)]
        public string? PassportNumber { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? PassportExpireDate { get; set; }

        [Required]
        public Gender Gender { get; set; } = Gender.Male;
    }
}




