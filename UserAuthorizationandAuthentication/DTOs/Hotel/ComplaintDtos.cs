using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using TravAi.Models.Enums;

namespace TravAi.DTOs.Hotel
{
    public class ComplaintCreateDto
    {
        public ComplaintType ComplaintType { get; set; }
        public long? BookingId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<IFormFile>? Attachments { get; set; }
        public List<long>? RemovedAttachmentIds { get; set; }
    }

    public class UserBookingMinimalDto
    {
        public long BookingId { get; set; }
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string Dates { get; set; } = string.Empty;
        public string TotalPrice { get; set; } = string.Empty;
    }

    public class ComplaintSummaryDto
    {
        public long Id { get; set; }
        public string ComplaintType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ComplaintDetailsDto
    {
        public long Id { get; set; }
        public string ComplaintType { get; set; } = string.Empty;
        public long? BookingId { get; set; }
        public string? HotelName { get; set; }
        public string? HotelCity { get; set; }
        public string? RoomName { get; set; }
        public string? BookingDates { get; set; }
        public string? TotalPrice { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ComplaintAttachmentDto> Attachments { get; set; } = new List<ComplaintAttachmentDto>();
        public List<ComplaintReplyDto> Replies { get; set; } = new List<ComplaintReplyDto>();
    }

    public class ComplaintAttachmentDto
    {
        public long Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
    }

    public class ComplaintReplyDto
    {
        public long Id { get; set; }
        public long? AdminUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string ReplyMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ComplaintAttachmentDto> Attachments { get; set; } = new List<ComplaintAttachmentDto>();
    }

    public class AdminComplaintSummaryDto
    {
        public long Id { get; set; }
        public string ComplaintType { get; set; } = string.Empty;
        public long? BookingId { get; set; }
        public string FromUser { get; set; } = string.Empty;
        public string Regarding { get; set; } = string.Empty; // Hotel Name or "Platform"
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminComplaintDetailsDto
    {
        public long Id { get; set; }
        public string ComplaintType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string FromUser { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<ComplaintAttachmentDto> Attachments { get; set; } = new List<ComplaintAttachmentDto>();
        public List<ComplaintReplyDto> Replies { get; set; } = new List<ComplaintReplyDto>();

        // Booking Context (Only for Hotel complaints)
        public AdminBookingContextDto? BookingContext { get; set; }
    }

    public class AdminBookingContextDto
    {
        public long BookingId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string BookingDates { get; set; } = string.Empty;
        public int Nights { get; set; }
        public int TotalRooms { get; set; }
        public string TotalPrice { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        
        // Payment Context
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal? RefundAmount { get; set; }
    }

    public class AdminReplyCreateDto
    {
        public string Message { get; set; } = string.Empty;
        public List<IFormFile>? Attachments { get; set; }
    }

    public class AdminReplyEditDto
    {
        public string Message { get; set; } = string.Empty;
        public List<IFormFile>? NewAttachments { get; set; }
        public List<long>? RemovedAttachmentIds { get; set; }
    }
}
