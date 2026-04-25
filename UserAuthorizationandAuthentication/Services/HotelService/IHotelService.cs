using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Models.Hotels;

namespace UserAuthorizationandAuthentication.Services.HotelService
{
    public interface IHotelService
    {
        // 1. Hotel Profile (For owner)
        Task<HotelDetailsDto> GetMyHotelProfileAsync(long userId);
        Task<bool> UpdateHotelAsync(long userId, long hotelId, UpdateHotelRequest request);

        // 1.5. Hotel Application Process
        Task<bool> ApplyAsHotelAsync(long userId, HotelApplicationRequest request);
        Task<HotelDetailsDto> GetMyApplicationStatusAsync(long userId);
        Task<bool> DeleteMyApplicationAsync(long userId);

        // 2. Hotel Rooms CRUD
        Task<RoomDto> CreateRoomAsync(long userId, CreateRoomRequest request);
        Task<List<RoomDto>> GetRoomsByHotelAsync(long hotelId);
        Task<RoomDto> UpdateRoomAsync(long userId, long roomId, UpdateRoomRequest request);
        Task<bool> DeleteRoomAsync(long userId, long roomId);

        // 3. Amenities
        Task<HotelAmenitiesDto> GetAmenitiesAsync(long hotelId);
        Task<bool> UpdateAmenitiesAsync(long userId, long hotelId, UpdateAmenitiesRequest request);

        // 4. Public Search & Details
        Task<HotelSearchResponse> SearchHotelsAsync(HotelSearchRequest request);
        Task<HotelDetailsDto> GetHotelDetailsAsync(long hotelId, DateTime? checkIn = null, DateTime? checkOut = null);

        // 5. Image Upload & Management
        Task<ImageUploadResponse> UploadImageAsync(long userId, UploadImageRequest request);
        Task<ImageUploadResponse> UpdateImageAsync(long userId, long imageId, UpdateImageRequest request);
        Task<bool> DeleteImageAsync(long userId, long imageId);
        Task<List<ImageUploadResponse>> GetHotelImagesAsync(long hotelId);

        // 6. Reviews
        Task<HotelReviewDto> AddReviewAsync(long userId, CreateReviewRequest request);
        Task<HotelReviewDto> UpdateReviewAsync(long userId, long reviewId, UpdateReviewRequest request);
        Task<bool> DeleteReviewAsync(long userId, long reviewId);
        Task<HotelReviewsResponse> GetHotelReviewsAsync(long hotelId, int page, int pageSize, bool isRandom = false);

        // 7. Public Room Search
        Task<RoomSearchResponse> SearchRoomsAsync(RoomSearchRequest request);
        Task<RoomDetailsDto> GetRoomDetailsAsync(long roomId);

        // 8. Bookings
        Task<BookingDto> CreateBookingAsync(long userId, CreateBookingRequest request);
        Task<List<BookingDto>> GetMyBookingsAsync(long userId); // For regular users
        Task<List<BookingDto>> GetUserTripsAsync(long userId, string tab);
        Task<List<BookingDto>> GetHotelBookingsAsync(long userId, long hotelId); // For hotel owners
        Task<BookingDto> GetBookingByIdAsync(long userId, long bookingId);
        Task<bool> CancelBookingAsync(long userId, long bookingId);
        Task<BookingDto> UpdateBookingStatusAsync(long userId, long bookingId, string status); // For hotel owners (Confirm, CheckIn, etc.)

        // 9. Admin Dashboard
        Task<List<HotelDetailsDto>> GetAllApplicationsAsync();
        Task<List<HotelDetailsDto>> GetPendingApplicationsAsync();
        Task<bool> ApproveApplicationAsync(long hotelId);
        Task<bool> RejectApplicationAsync(long hotelId, string reason);
    }
}
