using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Auth;

namespace TravAi.Models.AI
{
    public class ChatSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
