using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserAuthorizationandAuthentication.Models.Auth
{
    public class UserPhone
    {
        [Key]
        public long Id { get; set; }
        
        [Required]
        public long UserId { get; set; }
        
        [ForeignKey("UserId")]
        [JsonIgnore]
        public User User { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string PhoneNumber { get; set; }

        public bool PhoneVerified { get; set; } = false;

        public DateTime? PhoneVerifiedAt { get; set; }
    }
}
