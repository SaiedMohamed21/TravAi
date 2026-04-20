namespace UserAuthorizationandAuthentication.Airline.DTOs.Chat
{
    public class ChatMessageDto
    {
        public long Id { get; set; }
        public long BookingId { get; set; }
        public long SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsFromAdmin { get; set; }
    }

    public class SendMessageDto
    {
        public long BookingId { get; set; }
        public string Message { get; set; } = null!;
    }
}


