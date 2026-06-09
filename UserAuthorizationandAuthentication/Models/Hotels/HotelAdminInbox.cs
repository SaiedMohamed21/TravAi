using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;
using TravAi.Models.Auth;

namespace TravAi.Models.Hotels
{
    [Table("hotel_AdminInboxMessages")]
    public class HotelAdminInboxMessage
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long? AdminUserId { get; set; }
        [ForeignKey("AdminUserId")]
        public User? AdminUser { get; set; }

        [Required]
        public InboxCategory Category { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        public InboxSeverity? Severity { get; set; }

        // Metadata for linking to other records
        public string? RefType { get; set; }
        public string? RefId { get; set; }

        public decimal? Amount { get; set; }
        public DateTime? ResolutionDate { get; set; }
        
        public string? ActionLabel { get; set; }
        public InboxPriority? Priority { get; set; }

        public InboxStatus Status { get; set; } = InboxStatus.Unread;
        public bool IsRead { get; set; } = false;
        public bool IsResolved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<HotelAdminInboxReply> Replies { get; set; } = new List<HotelAdminInboxReply>();
    }

    [Table("hotel_AdminInboxReplies")]
    public class HotelAdminInboxReply
    {
        [Key]
        public long Id { get; set; }

        public long InboxMessageId { get; set; }
        [ForeignKey("InboxMessageId")]
        public HotelAdminInboxMessage InboxMessage { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long FromUserId { get; set; }
        [ForeignKey("FromUserId")]
        public User FromUser { get; set; }

        public long? ToAdminUserId { get; set; }
        [ForeignKey("ToAdminUserId")]
        public User? ToAdminUser { get; set; }

        [Required]
        public string ReplyMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("hotel_ToAdminMessages")]
    public class HotelToAdminMessage
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long FromUserId { get; set; }
        [ForeignKey("FromUserId")]
        public User FromUser { get; set; }

        [Required]
        public HotelToAdminCategory Category { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        public InboxStatus Status { get; set; } = InboxStatus.Unread;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
