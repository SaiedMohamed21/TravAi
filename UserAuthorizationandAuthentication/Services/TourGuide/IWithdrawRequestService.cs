using TravAi.TourGuide.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.WithdrawRequest;

namespace TravAi.TourGuide.Services
{
    public interface IWithdrawRequestService
    {
        Task<WithdrawRequestResponseDto> CreateWithdrawRequestAsync(long tourGuideId, CreateWithdrawRequestDto model);
        Task<List<WithdrawRequestResponseDto>> GetAllPendingRequestsAsync();
        Task<List<WithdrawRequestResponseDto>> GetMyRequestsAsync(long tourGuideId);
        Task<WithdrawRequestResponseDto> ProcessRequestAsync(long requestId, ProcessWithdrawRequestDto model);
    }
}


