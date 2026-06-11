using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.Models.Admin
{
    public class PlatformCommission
    {
        [Key]
        public long Id { get; set; }
        
        [Required]
        public string ServiceType { get; set; } // "Hotel", "Tour", "Airline"
        
        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Percentage { get; set; }
        
        public DateTime EffectiveFrom { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public long? CreatedByAdminUserId { get; set; }
        [ForeignKey("CreatedByAdminUserId")]
        public User CreatedByAdminUser { get; set; }
        
        public bool IsActive { get; set; } = false;

        public string? Notes { get; set; }
    }
}
