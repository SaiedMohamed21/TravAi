using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.Airline.DTOs.Chat;
using UserAuthorizationandAuthentication.Airline.Services.ChatService;

namespace UserAuthorizationandAuthentication.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/chat")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetHistory(long bookingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            var userId = long.Parse(userIdStr);
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            
            var history = await _chatService.GetChatHistoryAsync(bookingId, userId, role);
            return Ok(new ApiResponse<List<ChatMessageDto>>(history));
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            var userId = long.Parse(userIdStr);
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

            var message = await _chatService.SendMessageAsync(userId, role, dto);
            return Ok(new ApiResponse<ChatMessageDto>(message));
        }
    }
}
