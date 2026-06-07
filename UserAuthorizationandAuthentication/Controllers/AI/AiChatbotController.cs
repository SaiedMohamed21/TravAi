using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravAi.DTOs.AI;
using TravAi.DTOs.Common;
using TravAi.Services.AI;

namespace TravAi.Controllers.AI
{
    /// <summary>
    /// AI Chatbot Controller — proxies chat requests to the Python AI microservice.
    /// Supports multilingual (Arabic/English) tourism Q&A with RAG-grounded responses.
    /// </summary>
    [Route("api/ai")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "AI")]
    public class AiChatbotController : ControllerBase
    {
        private readonly IAiChatbotService _chatbotService;
        private readonly ILogger<AiChatbotController> _logger;

        public AiChatbotController(IAiChatbotService chatbotService, ILogger<AiChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the AI tourism chatbot (Horus).
        /// Supports Arabic and English. Returns AI response + retrieved tours from database.
        /// </summary>
        /// <remarks>
        /// Response structure:
        /// - success: true/false
        /// - message.content: AI response text
        /// - message.retrieved_tours: list of matching tours from the database
        /// - message.language_detected: "ar" or "en"
        /// 
        /// The city_context parameter filters RAG retrieval to a specific city.
        /// If null, tours from all cities are considered.
        /// 
        /// conversation_history should contain the last 10 messages max.
        /// </remarks>
        [HttpPost("chat")]
        [AllowAnonymous]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest(new ApiResponse<string>(false, "Message cannot be empty."));

                _logger.LogInformation(
                    "Chat request received. Message: {Msg}, City: {City}, History: {Count}",
                    request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                    request.CityContext ?? "All",
                    request.ConversationHistory?.Count ?? 0);

                var result = await _chatbotService.ChatAsync(request);

                if (!result.Success)
                {
                    return StatusCode(503, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Chat endpoint");
                return StatusCode(500, new ChatResponseDto
                {
                    Success = false,
                    Error = "An unexpected error occurred.",
                    Message = new ChatMessageDto
                    {
                        Role = "assistant",
                        Content = "Sorry, something went wrong. Please try again.",
                        LanguageDetected = "en"
                    }
                });
            }
        }

        /// <summary>
        /// Check if the Python AI microservice is online and healthy.
        /// </summary>
        [HttpGet("chat/health")]
        [AllowAnonymous]
        public async Task<IActionResult> ChatHealth()
        {
            var isHealthy = await _chatbotService.IsHealthyAsync();
            return Ok(new ApiResponse<object>(new
            {
                ai_service_online = isHealthy,
                status = isHealthy ? "ok" : "offline"
            }, isHealthy ? "AI service is healthy." : "AI service is offline."));
        }
    }
}
