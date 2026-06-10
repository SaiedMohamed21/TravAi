using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravAi.Data;
using TravAi.DTOs.AI;
using TravAi.DTOs.Common;
using TravAi.Models.AI;
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
        private readonly ApplicationDbContext _context;

        public AiChatbotController(IAiChatbotService chatbotService, ILogger<AiChatbotController> logger, ApplicationDbContext context)
        {
            _chatbotService = chatbotService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Send a message to the AI tourism chatbot (Horus).
        /// Supports Arabic and English. Returns AI response + retrieved tours from database.
        /// </summary>
        [HttpPost("chat")]
        [AllowAnonymous]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest(new ApiResponse<string>(false, "Message cannot be empty."));

                ChatSession? session = null;
                bool isUserLoggedIn = User.Identity?.IsAuthenticated == true;
                long? userId = null;

                if (isUserLoggedIn)
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        userId = parsedUserId;

                        // Delete sessions older than 1 hour for this user
                        var expiredSessions = await _context.ChatSessions
                            .Where(s => s.UserId == userId && s.LastUpdatedAt < DateTime.UtcNow.AddHours(-1))
                            .ToListAsync();
                        
                        if (expiredSessions.Any())
                        {
                            _context.ChatSessions.RemoveRange(expiredSessions);
                            await _context.SaveChangesAsync();
                        }

                        session = await _context.ChatSessions
                            .Include(s => s.Messages)
                            .FirstOrDefaultAsync(s => s.UserId == userId);

                        if (session == null)
                        {
                            session = new ChatSession { UserId = userId.Value };
                            _context.ChatSessions.Add(session);
                            await _context.SaveChangesAsync();
                        }

                        // Override frontend conversation history with DB history
                        request.ConversationHistory = session.Messages
                            .OrderBy(m => m.CreatedAt)
                            .Select(m => new ChatHistoryItemDto { Role = m.Role, Content = m.Content })
                            .ToList();
                    }
                }

                if (request.ConversationHistory == null)
                {
                    request.ConversationHistory = new List<ChatHistoryItemDto>();
                }

                _logger.LogInformation(
                    "Chat request received. Message: {Msg}, City: {City}, History: {Count}",
                    request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message,
                    request.CityContext ?? "All",
                    request.ConversationHistory.Count);

                var result = await _chatbotService.ChatAsync(request);

                if (!result.Success)
                {
                    return StatusCode(503, result);
                }

                // Save to database
                if (isUserLoggedIn && session != null && result.Message != null)
                {
                    // Add User Message
                    session.Messages.Add(new ChatMessage
                    {
                        ChatSessionId = session.Id,
                        Role = "user",
                        Content = request.Message,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Add Assistant Message
                    session.Messages.Add(new ChatMessage
                    {
                        ChatSessionId = session.Id,
                        Role = "assistant",
                        Content = result.Message.Content,
                        CreatedAt = DateTime.UtcNow
                    });

                    session.LastUpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
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
        /// Retrieves the current user's chat history. Only returns history from the last hour.
        /// </summary>
        [HttpGet("chat/history")]
        [Authorize]
        public async Task<IActionResult> GetChatHistory()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized();

            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.LastUpdatedAt >= DateTime.UtcNow.AddHours(-1));

            if (session == null)
            {
                return Ok(new ApiResponse<List<ChatHistoryItemDto>>(new List<ChatHistoryItemDto>(), "No active chat history."));
            }

            var history = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatHistoryItemDto
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList();

            return Ok(new ApiResponse<List<ChatHistoryItemDto>>(history, "History retrieved successfully."));
        }

        /// <summary>
        /// Clears the user's current chat history (call this on logout or when manual clear is needed).
        /// </summary>
        [HttpDelete("chat/history")]
        [Authorize]
        public async Task<IActionResult> ClearChatHistory()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId))
                return Unauthorized();

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (session != null)
            {
                _context.ChatSessions.Remove(session);
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<string>("Chat history cleared successfully."));
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
