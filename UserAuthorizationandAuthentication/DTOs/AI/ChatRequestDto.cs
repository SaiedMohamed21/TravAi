using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TravAi.DTOs.AI
{
    /// <summary>
    /// Request DTO for the AI Chatbot endpoint.
    /// Matches the Python FastAPI ChatRequest schema.
    /// </summary>
    public class ChatRequestDto
    {
        /// <summary>User's chat message (required, min 1 char).</summary>
        [Required]
        [MinLength(1)]
        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;

        /// <summary>
        /// Conversation history: list of prior messages.
        /// Each item has "role" (user/assistant) and "content".
        /// Frontend should send only the last 10 messages.
        /// </summary>
        [JsonPropertyName("conversation_history")]
        public List<ChatHistoryItemDto> ConversationHistory { get; set; } = new();

        /// <summary>
        /// Optional city filter from the frontend dropdown.
        /// If null → search across all cities.
        /// If set → RAG searches only in that city.
        /// </summary>
        [JsonPropertyName("city_context")]
        public string? CityContext { get; set; }
    }

    public class ChatHistoryItemDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = null!;

        [JsonPropertyName("content")]
        public string Content { get; set; } = null!;
    }
}
