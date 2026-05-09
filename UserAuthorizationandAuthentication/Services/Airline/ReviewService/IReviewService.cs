using TravAi;
using TravAi.Data;
using TravAi.Airline.DTOs.Review;

namespace TravAi.Airline.Services.ReviewService
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> AddReviewAsync(long userId, ReviewRequestDto dto);
        Task<List<ReviewResponseDto>> GetFlightReviewsAsync(long flightId);
    }
}



