using UserAuthorizationandAuthentication.TourGuide.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAuthorizationandAuthentication.TourGuide.DTOs.WithdrawRequest;

namespace UserAuthorizationandAuthentication.TourGuide.Services
{
    public interface IWithdrawRequestService
    {
        Task<WithdrawRequestResponseDto> CreateWithdrawRequestAsync(long tourGuideId, CreateWithdrawRequestDto model);
        Task<List<WithdrawRequestResponseDto>> GetAllPendingRequestsAsync();
        Task<List<WithdrawRequestResponseDto>> GetMyRequestsAsync(long tourGuideId);
        Task<WithdrawRequestResponseDto> ProcessRequestAsync(long requestId, ProcessWithdrawRequestDto model);
    }
}


