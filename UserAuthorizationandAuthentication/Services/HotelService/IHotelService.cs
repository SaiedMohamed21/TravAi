using TravAi.DTOs.Hotel;
using TravAi.Models.Hotels;
using TravAi.DTOs.AdminManagement;
using TravAi.Models.Enums;

namespace TravAi.Services.HotelService
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
        Task<BookingDto> CancelBookingAsync(long userId, long bookingId, string reason);
        Task<BookingDto> UpdateBookingStatusAsync(long userId, long bookingId, string status); // For hotel owners (Confirm, CheckIn, etc.)
        Task<PaymentResponseDto> ProcessPaymentAsync(long userId, ProcessPaymentRequest request);

        // 9. Admin Dashboard
        Task<List<HotelDetailsDto>> GetAllApplicationsAsync();
        Task<List<HotelDetailsDto>> GetPendingApplicationsAsync();
        Task<bool> ApproveApplicationAsync(long hotelId);
        Task<bool> RejectApplicationAsync(long hotelId, string reason);

        // 10. Complaints
        Task<List<UserBookingMinimalDto>> GetEligibleBookingsForComplaintAsync(long userId);
        Task<long> CreateComplaintAsync(long userId, ComplaintCreateDto dto);
        Task<bool> UpdateComplaintAsync(long userId, long complaintId, ComplaintCreateDto dto);
        Task<List<ComplaintSummaryDto>> GetMyComplaintsAsync(long userId);
        Task<ComplaintDetailsDto> GetComplaintDetailsAsync(long userId, long complaintId);
        // 11. Admin Complaints
        Task<List<AdminComplaintSummaryDto>> GetAdminComplaintsAsync(string? type, string? status, string? search, long? bookingId, string? hotelName);
        Task<AdminComplaintDetailsDto> GetAdminComplaintDetailsAsync(long complaintId);
        Task<bool> AdminReplyToComplaintAsync(long adminUserId, long complaintId, AdminReplyCreateDto dto);
        Task<bool> ResolveComplaintAsync(long complaintId);
        Task<bool> DeleteComplaintAsync(long userId, long complaintId);
        Task<bool> DeleteAdminReplyAsync(long adminUserId, long replyId);
        Task<bool> EditAdminReplyAsync(long adminUserId, long replyId, AdminReplyEditDto dto);

        // 12. Admin Reviews Management
        Task<PaginatedAdminReviewsResponse> GetAdminReviewsAsync(int pageNumber, int pageSize, string? userName, string? hotelName, string? keyword, int? rating);
        Task<bool> DeleteReviewAsync(long reviewId);

        // 13. Commission Settings
        Task<CommissionSettingDto?> GetCurrentCommissionSettingAsync();
        Task<List<CommissionSettingDto>> GetCommissionSettingsHistoryAsync();
        Task<CommissionSettingDto> SaveCommissionSettingAsync(long adminUserId, CreateCommissionSettingRequest request);

        // My Bookings
        Task<List<MyTripHotelDto>> GetMyTripsAsync(long userId, string tab);

        // Admin Management and Dashboard
        Task<AdminDashboardKpiDto> GetAdminDashboardKpisAsync();
        Task<AdminChartDataDto> GetAdminChartDataAsync(int? selectedYear);
        Task<List<HotelManageSummaryDto>> GetHotelManagementListAsync(string? searchQuery, VerificationStatus? status, string? city);
        Task<bool> SuspendHotelAsync(long hotelId);
        Task<bool> BanHotelAsync(long hotelId);
        Task<bool> ApproveHotelAsync(long hotelId);
        Task<AdminBookingPaginationResponse> GetAdminBookingsAsync(AdminBookingSearchRequest request);
    }
}
