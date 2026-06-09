using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;
using TravAi.Models.Hotels;
using TravAi.Models.Auth;


namespace TravAi.Models.Hotels.Bookings
{
    public class HotelBooking
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? Nights { get; set; }
        public int TotalRooms { get; set; }
        public decimal TotalPrice { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Cancellation Info
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public long? CancelledByUserId { get; set; }

        public decimal? CancellationFee { get; set; }
        public decimal? RefundAmount { get; set; }

        // Relationships
        public ICollection<HotelBookingRoom> BookingRooms { get; set; } = new List<HotelBookingRoom>();
        public ICollection<HotelPayment> Payments { get; set; } = new List<HotelPayment>();
    }

    public class HotelBookingRoom
    {
        [Key]
        public long Id { get; set; }

        [NotMapped]
        public long UserId { get; set; }
        [NotMapped]
        public User? User { get; set; }

        public long BookingId { get; set; }
        [ForeignKey("BookingId")]
        public HotelBooking Booking { get; set; }

        public long? RoomId { get; set; }
        [ForeignKey("RoomId")]
        public HotelRoom? Room { get; set; }

        public string? RoomName { get; set; }
        public string MealPlan { get; set; } = "RO";
        public decimal? PricePerNight { get; set; }
        public int Nights { get; set; }
        public decimal Subtotal { get; set; }
    }
}
