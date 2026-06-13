using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.Models.Hotels.Bookings;

namespace TravAi.Models.Hotels
{
    [Table("hotel_Complaints")]
    public class Complaint
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ComplaintType ComplaintType { get; set; }

        public long? BookingId { get; set; }
        [ForeignKey("BookingId")]
        public HotelBooking? Booking { get; set; }

        public long? TourBookingId { get; set; }
        [ForeignKey("TourBookingId")]
        public TravAi.TourGuide.Models.TourBooking? TourBooking { get; set; }

        public long? AirlineBookingId { get; set; }
        [ForeignKey("AirlineBookingId")]
        public TravAi.Airline.Models.Booking? AirlineBooking { get; set; }

        public long? HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel? Hotel { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
        public ICollection<ComplaintReply> Replies { get; set; } = new List<ComplaintReply>();
    }

    [Table("hotel_ComplaintAttachments")]
    public class ComplaintAttachment
    {
        [Key]
        public long Id { get; set; }

        public long? ComplaintId { get; set; }
        [ForeignKey("ComplaintId")]
        public Complaint? Complaint { get; set; }

        public long? ReplyId { get; set; }
        [ForeignKey("ReplyId")]
        public ComplaintReply? Reply { get; set; }

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("hotel_ComplaintReplies")]
    public class ComplaintReply
    {
        [Key]
        public long Id { get; set; }

        public long ComplaintId { get; set; }
        [ForeignKey("ComplaintId")]
        public Complaint? Complaint { get; set; }

        public long AdminUserId { get; set; }
        [ForeignKey("AdminUserId")]
        public User? AdminUser { get; set; }

        [Required]
        public string ReplyMessage { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    }
}
