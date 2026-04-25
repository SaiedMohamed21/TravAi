using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Airline.DTOs.Review;

namespace UserAuthorizationandAuthentication.Airline.Services.ReviewService
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> AddReviewAsync(long userId, ReviewRequestDto dto);
        Task<List<ReviewResponseDto>> GetFlightReviewsAsync(long flightId);
    }
}



