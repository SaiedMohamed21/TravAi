using UserAuthorizationandAuthentication;
using Microsoft.EntityFrameworkCore;

using UserAuthorizationandAuthentication.Airline.DTOs.Chat;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Airline.Models;

namespace UserAuthorizationandAuthentication.Airline.Services.ChatService
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;

        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessageDto>> GetChatHistoryAsync(long bookingId, long userId, string role)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .ThenInclude(f => f.Airline)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new Exception("Booking not found");

            // Authorization: User owns booking OR User is Admin OR User is the Airline owner
            bool isAuthorized = booking.UserId == userId || role == "Admin" || (booking.Flight.Airline.UserId == userId);
            
            if (!isAuthorized) throw new Exception("You are not authorized to view this chat.");

            return await _context.ChatMessages
                .Where(m => m.BookingId == bookingId)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    BookingId = m.BookingId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    Message = m.Message,
                    SentAt = m.SentAt,
                    IsFromAdmin = m.IsFromAdmin
                }).ToListAsync();
        }

        public async Task<ChatMessageDto> SendMessageAsync(long userId, string role, SendMessageDto dto)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .ThenInclude(f => f.Airline)
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

            if (booking == null) throw new Exception("Booking not found");

            bool isAdmin = role == "Admin" || booking.Flight.Airline.UserId == userId;
            bool isUser = booking.UserId == userId;

            if (!isAdmin && !isUser) throw new Exception("You are not authorized to send messages for this booking.");

            var message = new ChatMessage
            {
                BookingId = dto.BookingId,
                SenderId = userId,
                Message = dto.Message,
                SentAt = DateTime.UtcNow,
                IsFromAdmin = isAdmin
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Load sender name
            var sender = await _context.Users.FindAsync(userId);

            return new ChatMessageDto
            {
                Id = message.Id,
                BookingId = message.BookingId,
                SenderId = message.SenderId,
                SenderName = sender?.Name ?? "Unknown",
                Message = message.Message,
                SentAt = message.SentAt,
                IsFromAdmin = message.IsFromAdmin
            };
        }
    }
}



