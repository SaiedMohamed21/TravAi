using TravAi.DTOs.AI;

namespace TravAi.Services.AI
{
    /// <summary>
    /// Interface for the AI Chatbot service that proxies requests
    /// to the Python FastAPI AI microservice.
    /// </summary>
    public interface IAiChatbotService
    {
        /// <summary>
        /// Sends a chat message to the Python AI service and returns the response.
        /// </summary>
        /// <param name="request">Chat request containing the message, history, and optional city filter.</param>
        /// <returns>Chat response with AI text, retrieved tours, and detected language.</returns>
        Task<ChatResponseDto> ChatAsync(ChatRequestDto request);

        /// <summary>
        /// Checks the health status of the Python AI microservice.
        /// </summary>
        /// <returns>True if the service is healthy, false otherwise.</returns>
        Task<bool> IsHealthyAsync();
    }
}
