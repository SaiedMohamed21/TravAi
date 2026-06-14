using System;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Admin;

namespace TravAi.DTOs.Admin.Fines
{
    public class CreateProviderFineDto
    {
        [Required]
        public ProviderType ProviderType { get; set; }
        
        [Required]
        public ProviderFineSourceType SourceType { get; set; }

        public long? ProviderId { get; set; }
        public long? ComplaintId { get; set; }
        public long? TourBookingId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "USD";
        
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? AdminNotes { get; set; }
    }

    public class UpdateProviderFineDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? AdminNotes { get; set; }
    }

    public class CancelProviderFineDto
    {
        [Required]
        [MaxLength(1000)]
        public string CancellationReason { get; set; } = string.Empty;
    }

    public class ProviderFineListItemDto
    {
        public long Id { get; set; }
        public ProviderType ProviderType { get; set; }
        public long ProviderId { get; set; }
        public string ProviderDisplayName { get; set; } = string.Empty;
        public ProviderFineSourceType SourceType { get; set; }
        public long? ComplaintId { get; set; }
        public long? RelatedBookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Reason { get; set; } = string.Empty;
        public ProviderFineStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByAdminName { get; set; } = string.Empty;

        // Rich data for TourGuideCancellation
        public long? TourId { get; set; }
        public long? TourBookingId { get; set; }
        public string? TourTitle { get; set; }
        public DateTime? TourDate { get; set; }
        public int? ParticipantsCount { get; set; }
        public decimal? TicketPricePerParticipant { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TotalPaid { get; set; }
        public long? TourGuideId { get; set; }
        public string? TourGuideName { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancellationReviewStatus { get; set; }
    }

    public class ProviderFineDetailsDto : ProviderFineListItemDto
    {
        public string? AdminNotes { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledByAdminName { get; set; }
        public string? CancellationReason { get; set; }

        public object? ProviderDetails { get; set; }
        public object? ComplaintSummary { get; set; }
        public object? BookingSummary { get; set; }
    }

    public class ProviderFineFilterDto
    {
        public long? FineId { get; set; }
        public string? ProviderName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public ProviderType? ProviderType { get; set; }
        public long? ProviderId { get; set; }
        public ProviderFineStatus? Status { get; set; }
        public ProviderFineSourceType? SourceType { get; set; }
        public long? ComplaintId { get; set; }
        public long? BookingId { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        public string? Currency { get; set; }
        public string? Reason { get; set; }
        public string? CreatedByAdminName { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class EligibleFineComplaintDto
    {
        public long ComplaintId { get; set; }
        public string ComplaintType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? ComplaintStatus { get; set; }

        public string? UserName { get; set; }
        public string? UserEmail { get; set; }

        public long? ProviderId { get; set; }
        public string? ProviderName { get; set; }

        public long? RelatedBookingId { get; set; }
        public string? BookingTitle { get; set; }
        public string? BookingStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal? TotalPaid { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Currency { get; set; }

        public string? ExtraInfo { get; set; }
        public bool HasActiveFine { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EligibleTourCancellationDto
    {
        public long TourBookingId { get; set; }
        public long TourId { get; set; }
        public string TourName { get; set; } = string.Empty;
        public long TourGuideId { get; set; }
        public string TourGuideName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        public string? CancellationReason { get; set; }
        public string? CancellationReviewStatus { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TotalPaid { get; set; }
        public string? Currency { get; set; }
        
        public DateTime? TourDate { get; set; }
        public int? ParticipantsCount { get; set; }
        public decimal? TicketPricePerParticipant { get; set; }

        public bool HasActiveFine { get; set; }
    }

    public class ProviderLookupDto
    {
        public long ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public ProviderType ProviderType { get; set; }
        public string? ExtraInfo { get; set; }
    }
}
