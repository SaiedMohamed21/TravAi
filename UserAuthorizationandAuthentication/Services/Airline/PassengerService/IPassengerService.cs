using TravAi;
using TravAi.Data;
using TravAi.Airline.DTOs.Passenger;

namespace TravAi.Airline.Services.PassengerService
{
    public interface IPassengerService
    {
        Task<PassengerResponseDto> CreateAsync(CreatePassengerDto dto, long userId);
        Task<List<PassengerResponseDto>> GetBookingPassengersAsync(long bookingId);
        Task<PassengerResponseDto?> GetByIdAsync(long id);
        Task UpdateAsync(long id, UpdatePassengerDto dto, long userId);
        Task DeleteAsync(long id, long userId);
        Task SavePassengerDetailsAsync(long bookingId, SaveBookingPassengersRequest request, long userId);
    }
}



