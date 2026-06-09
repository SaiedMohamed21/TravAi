using System;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Hotels;

namespace TravAi.DTOs.Hotel
{
    public class CommissionSettingDto
    {
        public long Id { get; set; }
        public decimal PlatformCommissionPct { get; set; }
        public decimal VatPct { get; set; }
        public string CityTaxMode { get; set; } = string.Empty;
        public decimal CityTaxValue { get; set; }
        public bool IsActive { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByAdminName { get; set; } = string.Empty;
    }

    public class CreateCommissionSettingRequest
    {
        [Required]
        [Range(0, 100)]
        public decimal PlatformCommissionPct { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal VatPct { get; set; }

        [Required]
        public CityTaxMode CityTaxMode { get; set; }

        [Required]
        [Range(0, (double)decimal.MaxValue)]
        public decimal CityTaxValue { get; set; }
    }
}
