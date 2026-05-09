using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;

namespace TravAi.Models.Hotels
{
    public class HotelPolicy
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        // Pricing Inclusions
        public decimal ServiceChargePct { get; set; } = 12;
        public bool IncludeServiceCharge { get; set; }
        public bool IncludeVat { get; set; }
        public bool IncludeCityTax { get; set; }

        // Cancellation Rules
        public CancellationStrategy CancellationStrategy { get; set; } = CancellationStrategy.FreeAll;
        public ICollection<HotelCancellationRule> CancellationRules { get; set; } = new List<HotelCancellationRule>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string? CancellationWindow { get; set; }
        [NotMapped]
        public decimal CancellationPenaltyPct { get; set; } = 100;
    }
}
