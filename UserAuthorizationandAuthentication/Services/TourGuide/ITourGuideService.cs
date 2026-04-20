using UserAuthorizationandAuthentication.TourGuide.Models;
using TourGuide = UserAuthorizationandAuthentication.TourGuide.Models.TourGuide;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAuthorizationandAuthentication.TourGuide.DTOs.TourGuide;
using UserAuthorizationandAuthentication.TourGuide.DTOs.Review;
namespace UserAuthorizationandAuthentication.TourGuide.Services
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
    }
}



