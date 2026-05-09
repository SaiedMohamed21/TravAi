using TravAi.Airline.DTOs.Flight;

namespace TravAi.Airline.Services.FlightService
{
    public interface IFlightService
    {
        Task CreateAsync(long userId, CreateFlightDto dto);
        Task<PaginatedFlightResultDto> SearchAsync(FlightSearchDto dto);
        Task<FlightResultDto?> GetByIdAsync(long id);
        Task UpdateAsync(long id, UpdateFlightDto dto);
        Task CancelAsync(long id);
    }
}
