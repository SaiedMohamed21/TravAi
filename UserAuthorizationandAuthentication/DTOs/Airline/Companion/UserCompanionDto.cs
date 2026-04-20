using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Companion
{
    public class UserCompanionDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string AgeType { get; set; } = null!;
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? PassportExpireDate { get; set; }
        public string Gender { get; set; } = "Male";
    }

    public class CreateCompanionDto
    {
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        [Required]
        public string AgeType { get; set; } = "Adult";
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportImage { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? PassportExpireDate { get; set; }
        [Required]
        public string Gender { get; set; } = "Male";
    }
}


