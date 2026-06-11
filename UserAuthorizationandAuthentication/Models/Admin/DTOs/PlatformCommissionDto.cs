using System;
using System.ComponentModel.DataAnnotations;

namespace TravAi.Models.Admin.DTOs
{
    public class PlatformCommissionDto
    {
        public long Id { get; set; }
        public string ServiceType { get; set; }
        public decimal Percentage { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByAdminName { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } // "Active", "Scheduled", "Old"
    }

    public class CreatePlatformCommissionRequest
    {
        [Required]
        [Range(0, 100)]
        public decimal Percentage { get; set; }

        public string? Notes { get; set; }
    }

    public class PlatformCommissionDashboardResponse
    {
        public string ServiceType { get; set; }
        
        public PlatformCommissionDto? ActiveCommission { get; set; }
        public PlatformCommissionDto? PendingCommission { get; set; }

        // Timer calculation
        public int? RemainingDays { get; set; }
        public int? RemainingHours { get; set; }
        public int? RemainingMinutes { get; set; }
        public int? RemainingSeconds { get; set; }
        
        public int HistoryCount { get; set; }
    }
}
