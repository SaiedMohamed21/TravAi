using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.Airline.DTOs.User
{
    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? ProfilePic { get; set; }
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Status { get; set; } = null!;
        public decimal WalletBalance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


