using TravAi.TourGuide.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.UrgentRequest;

namespace TravAi.TourGuide.Services
{
    public interface IUrgentRequestService
    {
        Task<UrgentRequestResponseDto> CreateUrgentRequestAsync(long tourGuideId, CreateUrgentRequestDto model);
        Task<List<UrgentRequestResponseDto>> GetAllPendingRequestsAsync();
        Task<List<UrgentRequestResponseDto>> GetMyRequestsAsync(long tourGuideId);
        Task<UrgentRequestResponseDto> ProcessRequestAsync(long requestId, AdminProcessUrgentRequestDto model);
    }
}


