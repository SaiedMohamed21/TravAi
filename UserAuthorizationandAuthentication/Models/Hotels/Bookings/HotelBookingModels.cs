using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models.Hotels;
using UserAuthorizationandAuthentication.Models.Auth;


namespace UserAuthorizationandAuthentication.Models.Hotels.Bookings
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

        // Relationships
        public ICollection<HotelBookingRoom> BookingRooms { get; set; } = new List<HotelBookingRoom>();
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
