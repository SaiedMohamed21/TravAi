using TravAi.TourGuide.Models;
using TourGuide = TravAi.TourGuide.Models.TourGuide;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.TourGuide;
using TravAi.TourGuide.DTOs.Review;
namespace TravAi.TourGuide.Services
{
    public interface ITourGuideService
    {
        Task<TourGuideResponseDto> ApplyAsync(long userId, TourGuideApplicationDto model);
        Task<List<TourGuideResponseDto>> GetAllApplicationsAsync();
        Task<TourGuideResponseDto> GetApplicationByIdAsync(long id);
        Task<TourGuideResponseDto> GetTourGuideByUserIdAsync(long userId);
        Task<bool> ApproveApplicationAsync(long id);
        Task<bool> RejectApplicationAsync(long id, string reason);
        Task<bool> BanTourGuideAsync(long id);
        Task<bool> SuspendTourGuideAsync(long tourGuideId, SuspendTourGuideDto model);
        Task<bool> UpdateLicenseAsync(long tourGuideId, UpdateLicenseDto model);
        Task<bool> UpdateProfileAsync(long tourGuideId, UpdateProfileDto model);
        Task<List<ReviewDto>> GetTourGuideReviewsAsync(long tourGuideId);
        Task<TourGuideReviewsDashboardDto> GetTourGuideReviewsDashboardAsync(long tourGuideId);
        Task<TourGuideProfileDto> GetProfileAsync(long id);
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(long tourGuideId);
        Task<List<EarningsChartDto>> GetEarningsChartAsync(long tourGuideId);
        Task<TourGuideEarningsSummaryDto> GetEarningsSummaryAsync(long tourGuideId);
    }
}



