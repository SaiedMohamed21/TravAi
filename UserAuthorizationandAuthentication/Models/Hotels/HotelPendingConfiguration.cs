using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;

namespace TravAi.Models.Hotels
{
    public enum PendingRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    [Table("hotel_PendingProfiles")]
    public class HotelPendingProfile
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long RequestedByUserId { get; set; }

        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
        public string? Governorate { get; set; }
        public string? CityArea { get; set; }
        public string? AddressDetails { get; set; }
        public int? StarRating { get; set; }
        public PropertyType? PropertyType { get; set; }
        public AccommodationType? AccommodationType { get; set; }

        public string? ImagesJson { get; set; }
        public string? AmenitiesJson { get; set; }
        public string? RoomsJson { get; set; }
        public string? DynamicFieldsJson { get; set; }
        public string? ContactsJson { get; set; }
        
        public PendingRequestStatus Status { get; set; } = PendingRequestStatus.Pending;
        public string? AdminComment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }

    [Table("hotel_PendingPolicies")]
    public class HotelPendingPolicy
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long RequestedByUserId { get; set; }

        public decimal? ServiceChargePct { get; set; }
        public bool? IncludeServiceCharge { get; set; }
        public bool? IncludeVat { get; set; }
        public bool? IncludeCityTax { get; set; }
        public CancellationStrategy? CancellationStrategy { get; set; }

        public ICollection<HotelPendingCancellationRule> CancellationRules { get; set; } = new List<HotelPendingCancellationRule>();

        public PendingRequestStatus Status { get; set; } = PendingRequestStatus.Pending;
        public string? AdminComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }

    [Table("hotel_PendingCancellationRules")]
    public class HotelPendingCancellationRule
    {
        [Key]
        public long Id { get; set; }

        public long HotelPendingPolicyId { get; set; }
        [ForeignKey("HotelPendingPolicyId")]
        public HotelPendingPolicy PendingPolicy { get; set; }

        public int? FromHoursBeforeCheckIn { get; set; }
        public int? ToHoursBeforeCheckIn { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyPct { get; set; }
    }

    [Table("hotel_PendingLegalDocuments")]
    public class HotelPendingLegalDocument
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long RequestedByUserId { get; set; }

        public long DocumentTypeId { get; set; }
        [ForeignKey("DocumentTypeId")]
        public DocumentTypeDefinition DocumentType { get; set; }

        public string FileUrl { get; set; }
        public string? Notes { get; set; }

        public PendingRequestStatus Status { get; set; } = PendingRequestStatus.Pending;
        public string? AdminComment { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
