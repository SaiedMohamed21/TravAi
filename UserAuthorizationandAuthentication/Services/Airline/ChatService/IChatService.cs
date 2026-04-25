using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Airline.DTOs.Chat;

namespace UserAuthorizationandAuthentication.Airline.Services.ChatService
{
    public interface IChatService
    {
        Task<List<ChatMessageDto>> GetChatHistoryAsync(long bookingId, long userId, string role);
        Task<ChatMessageDto> SendMessageAsync(long userId, string role, SendMessageDto dto);
    }
}



