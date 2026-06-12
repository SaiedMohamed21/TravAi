using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Enums;

namespace TravAi.DTOs.Hotel
{
    public class CreateBookingRequest
    {
        [Required]
        public long HotelId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public List<CreateBookingRoomRequest> Rooms { get; set; } 
    }

    public class CreateBookingRoomRequest
    {
        [Required]
        public long RoomId { get; set; }
        
        public string MealPlan { get; set; } = "RO";
    }

    public class BookingDto
    {
        public long Id { get; set; }
        public long HotelId { get; set; }
        public string HotelName { get; set; }
        public string PropertyType { get; set; }
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }
        
        public int TotalRooms { get; set; }
        public decimal TotalPrice { get; set; }
        
        public string PaymentStatus { get; set; }
        public string Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? RoomName { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReview { get; set; }
        public bool CanRebook { get; set; }
        
        public List<BookingRoomDto> Rooms { get; set; }
        public UserSummaryDto User { get; set; } // For hotel owner view

        // Cancellation Info
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public long? CancelledByUserId { get; set; }

        // Payment Info
        public List<PaymentResponseDto> Payments { get; set; } = new List<PaymentResponseDto>();
    }

    public class BookingRoomDto
    {
        public long RoomId { get; set; }
        public string RoomName { get; set; }
        public string BedType { get; set; }
        public string MealPlan { get; set; }
        public decimal PricePerNight { get; set; }
        public int Nights { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class UserSummaryDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class BookingActionRequest
    {
        [Required]
        public string Action { get; set; } // "Confirm", "CheckIn", "CheckOut", "Cancel"
    }

    public class MyTripHotelDto
    {
        public long BookingId { get; set; }
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public decimal? RefundAmount { get; set; }
        public decimal? CancellationFee { get; set; }
        public bool CanCancel { get; set; }
        public bool CanRebook { get; set; }
        public bool CanReview { get; set; }
        public bool HasReviewed { get; set; }
        public long? ReviewId { get; set; }
        public int? ReviewRating { get; set; }
        public string? ReviewComment { get; set; }
    }

    public class CancelBookingRequest
    {
        [Required]
        public long BookingId { get; set; }

        [Required]
        public string Reason { get; set; }

        public string? RefundMethod { get; set; } // "Wallet" or "OriginalPaymentMethod"
    }

    public class ProcessPaymentRequest
    {
        [Required]
        public long BookingId { get; set; }

        [Required]
        public HotelPaymentMethod PaymentMethod { get; set; }

        public string? Notes { get; set; }
    }

    public class PaymentResponseDto
    {
        public long BookingId { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
    }
    public class CancelPreviewDto
    {
        public long BookingId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundPercentage { get; set; }
        public string PolicyStrategy { get; set; }
        public List<HotelCancellationRuleDto>? CancellationRules { get; set; }
        public string? AppliedRuleText { get; set; }

        public bool OriginalPaymentMethodAvailable { get; set; }
        public List<string> AvailableRefundMethods { get; set; } = new List<string> { "Wallet" };
    }
}
