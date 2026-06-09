using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.DTOs.Hotel;
using TravAi.Models.Enums;

namespace TravAi.Services.HotelService
{
    public interface IHotelDashboardService
    {
        Task<HotelDashboardOverviewDto> GetOverviewAsync(long userId, long? hotelId = null);
        Task<List<DashboardBookingItemDto>> GetDashboardBookingsAsync(long userId, string? status, DateTime? from, DateTime? to, string? bedType, string? guestName, string? bookingId, DateTime? checkIn = null, DateTime? checkOut = null, long? hotelId = null);
        Task UpdateRoomConfigAsync(long userId, List<RoomTypeSummaryDto> rooms, List<long> deletedIds);
        Task<HotelReviewsResponse> GetHotelReviewsAsync(long userId, string? datePreset, int? starRating, int page, DateTime? startDate = null, long? hotelId = null);
        Task<HotelFinancialsDto> GetFinancialsAsync(long userId, int year, long? hotelId = null);
        
        // Admin Inbox
        Task<HotelInboxSummaryDto> GetInboxDashboardAsync(long userId);
        Task<HotelInboxPagedDto<HotelInboxItemDto>> GetInboxCategoryPagedAsync(long userId, InboxCategory category, int page, int pageSize);
        Task<HotelInboxItemDto> GetInboxMessageDetailsAsync(long userId, long messageId);
        Task ReplyToInboxMessageAsync(long userId, long messageId, string message);
        Task SendMessageToAdminAsync(long userId, string subject, string message, HotelToAdminCategory category);
        Task MarkInboxMessageAsReadAsync(long userId, long messageId);
        Task ResolveInboxMessageAsync(long userId, long messageId);

        // Configuration Split & Approval workflow
        Task SubmitProfileUpdateAsync(long userId, HotelProfileUpdateRequest request);
        Task SubmitPolicyUpdateAsync(long userId, HotelPolicyUpdateRequest request);
        Task SubmitLegalUpdateAsync(long userId, HotelLegalUpdateRequest request);
        Task<List<string>> GetPendingConfigSectionsAsync(long userId);
        Task<HotelDetailsDto?> GetPendingApplicationAsync(long userId);
    }
}
