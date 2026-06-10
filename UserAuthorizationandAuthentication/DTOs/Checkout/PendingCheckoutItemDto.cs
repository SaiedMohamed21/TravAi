using System;

namespace TravAi.DTOs.Checkout
{
    public class PendingCheckoutItemDto
    {
        public long Id { get; set; }
        public string ItemType { get; set; } = null!; // AirlineBooking, HotelBooking, TourBooking
        public string DisplayName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public DateTime CreatedAt { get; set; }
        public DateTime BookingDate => CreatedAt;
        public DateTime ExpiresAt { get; set; }
        public DateTime ServerNowUtc { get; set; }
        public double RemainingSeconds { get; set; }
        public long ReferenceId { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        
        public bool RequiresPassengerDetails { get; set; }
        public string PassengerDetailsStatus { get; set; } = "Incomplete";
        public bool IsReadyForPayment { get; set; }
        public string ReadinessMessage { get; set; } = string.Empty;
    }
}
