using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.Models
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        
        [Column(TypeName = "nvarchar(20)")]
        public UserRole Role { get; set; } = UserRole.User;
        
        public DateTime? DateOfBirth { get; set; }
        
        [Column(TypeName = "nvarchar(10)")]
        public Gender Gender { get; set; }
        
        public string? ProfilePic { get; set; }
        
        [Column(TypeName = "nvarchar(20)")]
        public UserStatus Status { get; set; } = UserStatus.Pending;
        
        public bool IsBanned { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- MERGED FROM AIRLINE ---
        public string? PassportNumber { get; set; }
        public string? PassportImage { get; set; }
        public string? Nationality { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal WalletBalance { get; set; } = 0;
        // ----------------------------

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<UserPhone> UserPhones { get; set; } = new List<UserPhone>();
    }
}
