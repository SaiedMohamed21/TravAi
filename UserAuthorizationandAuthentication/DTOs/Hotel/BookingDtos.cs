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
    }

    public class BookingRoomDto
    {
        public long RoomId { get; set; }
        public string RoomName { get; set; }
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
}
