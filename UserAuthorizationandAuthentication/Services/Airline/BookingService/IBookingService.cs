using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Airline.DTOs.Booking;

namespace UserAuthorizationandAuthentication.Airline.Services.BookingService
{
    public interface IBookingService
    {
        Task<BookingResponseDto> BookFlightAsync(long userId, BookingRequestDto dto);
        Task<List<BookingResponseDto>> GetUserBookingsAsync(long userId);
        Task<List<BookingResponseDto>> GetUserTripsAsync(long userId, string tab);
        Task<List<BookingResponseDto>> GetFlightBookingsAsync(long flightId);
        Task<List<BookingResponseDto>> GetAllBookingsAsync();
        Task<BookingResponseDto?> GetByIdAsync(long bookingId);
        Task CancelAsync(long bookingId);
        Task<ETicketDto> GetETicketAsync(long bookingId);
        
        // Review Methods
        Task UpdateBookingStatusAsync(long bookingId, string status, string? reason = null);
        Task UpdatePassengerStatusAsync(long passengerId, string status, string? reason = null);
    }
}



