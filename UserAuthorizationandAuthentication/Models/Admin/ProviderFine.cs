using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TravAi.Models.Auth;
using TravAi.Models.Hotels;

namespace TravAi.Models.Admin
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProviderType
    {
        Hotel = 1,
        TourGuide = 2,
        Airline = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProviderFineStatus
    {
        Active = 1,
        Cancelled = 2,
        Deducted = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProviderFineSourceType
    {
        Complaint = 1,
        TourGuideCancellation = 2,
        Others = 3
    }

    [Table("admin_ProviderFines")]
    public class ProviderFine
    {
        [Key]
        public long Id { get; set; }

        public ProviderType ProviderType { get; set; }
        
        /// <summary>
        /// The ID of the provider (HotelId, TourGuideId, or AirlineId).
        /// </summary>
        public long ProviderId { get; set; }

        public ProviderFineSourceType SourceType { get; set; }

        public long? ComplaintId { get; set; }
        [ForeignKey("ComplaintId")]
        public Complaint? Complaint { get; set; }

        public long? HotelBookingId { get; set; }
        public long? TourBookingId { get; set; }
        public long? AirlineBookingId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? AdminNotes { get; set; }

        public ProviderFineStatus Status { get; set; } = ProviderFineStatus.Active;

        public long CreatedByAdminUserId { get; set; }
        [ForeignKey("CreatedByAdminUserId")]
        public User CreatedByAdminUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long? CancelledByAdminUserId { get; set; }
        [ForeignKey("CancelledByAdminUserId")]
        public User? CancelledByAdminUser { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        [MaxLength(1000)]
        public string? CancellationReason { get; set; }
    }
}
