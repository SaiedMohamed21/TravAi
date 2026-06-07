using TravAi.TourGuide.Models;
using Tour = TravAi.TourGuide.Models.Tour;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.Tour;
using TravAi.TourGuide.DTOs.Review;
namespace TravAi.TourGuide.Services
{
    public interface ITourService
    {
        Task<TourResponseDto> CreateTourAsync(long tourGuideId, CreateTourDto model);
        Task<TourResponseDto> UpdateTourAsync(long tourId, long tourGuideId, UpdateTourDto model);
        Task<bool> DeleteTourAsync(long tourId, long tourGuideId);
        Task<bool> AdminDeleteTourAsync(long tourId);
        Task<TourResponseDto> GetTourByIdAsync(long tourId);
        Task<List<TourResponseDto>> GetToursByTourGuideAsync(long tourGuideId);
        Task<(List<TourResponseDto> Tours, int TotalCount)> GetToursWithFiltersAsync(TourFilterDto filters);
        Task<List<ReviewDto>> GetTourReviewsAsync(long tourId);
        Task<(List<TourCardDto> Cards, int TotalCount)> GetTourCardsAsync(TourFilterDto filters);
        Task<TourDetailsDto> GetTourDetailsAsync(long tourId);
    }
}



