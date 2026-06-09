using System;
using System.Collections.Generic;

namespace TravAi.DTOs.Hotel
{
    public class DashboardBookingItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public string RoomTypes { get; set; } = string.Empty; // e.g. "StandardRoom, DeluxeDouble"
        public string RoomDetails { get; set; } = string.Empty; // e.g. "3 Single, 1 Double"
        public List<string> RoomNames { get; set; } = new List<string>();
        
        public string BookingDate { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string CheckOut { get; set; } = string.Empty;
        
        public decimal TotalPaid { get; set; }
        public string Method { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        public string ColorCode { get; set; } = string.Empty;

        // Cancellation specifics for log
        public decimal? CancellationFee { get; set; }
        public decimal? RefundAmount { get; set; }
        public string CancellationDate { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
    }
}
