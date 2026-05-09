using TravAi;
using TravAi.Data;
using TravAi.Airline.DTOs.Companion;

namespace TravAi.Airline.Services.CompanionService
{
    public interface ICompanionService
    {
        Task<List<UserCompanionDto>> GetMyCompanionsAsync(long userId);
        Task<UserCompanionDto> AddCompanionAsync(long userId, CreateCompanionDto dto);
        Task DeleteCompanionAsync(long userId, long companionId);
    }
}



