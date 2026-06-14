using TravAi.TourGuide.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.Booking;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;
using BookingStatus = TravAi.TourGuide.Models.Enums.BookingStatus;

namespace TravAi.TourGuide.Services
{
    public interface IBookingService
    {
        // Direct booking - creates booking with Pending status (1 participant by default)
        Task<BookingResponseDto> CreateBookingAsync(long userId, long tourId, int participantsCount = 1);
        
        // Update booking participants
        Task<BookingResponseDto> UpdateBookingParticipantsAsync(long userId, long bookingId, List<ParticipantDto> participants);
        
        // Payment processing - changes status from Pending to Confirmed
        Task<BookingResponseDto> ProcessPaymentAsync(long userId, ProcessPaymentDto model);
        
        // Cancel booking (before or after payment)
        Task<bool> CancelBookingAsync(long userId, long bookingId, string? refundMethod = null);
        Task<TourCancelPreviewDto> PreviewCancelBookingAsync(long userId, long bookingId);
        
        // Get user's bookings (all statuses or specific status)
        Task<List<BookingResponseDto>> GetUserBookingsAsync(long userId, BookingStatus? status = null);
        
        // Get bookings assigned to a Tour Guide
        Task<List<BookingResponseDto>> GetAssignedBookingsAsync(long tourGuideId, BookingStatus? status = null);

        // Get a specific booking assigned to a Tour Guide
        Task<BookingResponseDto> GetAssignedBookingByIdAsync(long tourGuideId, long bookingId);

        Task<List<BookingResponseDto>> GetUserTripsAsync(long userId, string tab);
        Task<BookingResponseDto> GetBookingByIdAsync(long userId, long bookingId);
        
        // Admin - Get all bookings
        Task<List<BookingResponseDto>> GetAllBookingsAsync(BookingStatus? status = null);
        Task<BookingResponseDto> GetBookingByAdminAsync(long bookingId);
        
        // Check tour availability
        Task<bool> IsTourAvailableAsync(long tourId, int participantsCount);
    }
}



