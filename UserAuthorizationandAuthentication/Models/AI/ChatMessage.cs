using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravAi.Models.AI
{
    public class ChatMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ChatSessionId { get; set; }
        [ForeignKey("ChatSessionId")]
        public ChatSession ChatSession { get; set; } = null!;

        public string Role { get; set; } = null!; // "user" or "assistant"
        public string Content { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
