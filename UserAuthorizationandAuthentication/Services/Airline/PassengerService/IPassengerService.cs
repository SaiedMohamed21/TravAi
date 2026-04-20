using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Airline.DTOs.Passenger;

namespace UserAuthorizationandAuthentication.Airline.Services.PassengerService
{
    public interface IPassengerService
    {
        Task<PassengerResponseDto> CreateAsync(CreatePassengerDto dto);
        Task<List<PassengerResponseDto>> GetBookingPassengersAsync(long bookingId);
        Task<PassengerResponseDto?> GetByIdAsync(long id);
        Task UpdateAsync(long id, UpdatePassengerDto dto);
        Task DeleteAsync(long id);
    }
}



