using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Airline.DTOs.Companion;

namespace UserAuthorizationandAuthentication.Airline.Services.CompanionService
{
    public interface ICompanionService
    {
        Task<List<UserCompanionDto>> GetMyCompanionsAsync(long userId);
        Task<UserCompanionDto> AddCompanionAsync(long userId, CreateCompanionDto dto);
        Task DeleteCompanionAsync(long userId, long companionId);
    }
}



