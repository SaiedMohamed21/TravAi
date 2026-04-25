using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public long UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression("(?i)^(Male|Female)$", ErrorMessage = "Gender must be 'Male' or 'Female'.")]
        public string Gender { get; set; }

        public string? ProfilePic { get; set; }
        
        // Phones
        [Required(ErrorMessage = "Primary Phone Number is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Primary Phone Number must contain only digits.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Primary Phone Number must be between 10 and 15 digits.")]
        public string PhoneNumber { get; set; }

        [RegularExpression(@"^\d+$", ErrorMessage = "Secondary Phone Number must contain only digits.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Secondary Phone Number must be between 10 and 15 digits.")]
        public string? SecondaryPhoneNumber { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class RefreshTokenRequest
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RevokeTokenRequest
    {
        public string RefreshToken { get; set; }
    }
    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string? ProfilePic { get; set; }
        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Status { get; set; }
        public string? Gender { get; set; }
        public decimal WalletBalance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfilePic { get; set; }
        public string? Nationality { get; set; }
        public string? PassportNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
