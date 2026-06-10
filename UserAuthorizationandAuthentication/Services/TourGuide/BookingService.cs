using TravAi.TourGuide.Models;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.Booking;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;
using PaymentStatus = TravAi.TourGuide.Models.Enums.PaymentStatus;
using BookingStatus = TravAi.TourGuide.Models.Enums.BookingStatus;

namespace TravAi.TourGuide.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingResponseDto> CreateBookingAsync(long userId, long tourId)
        {
            // Check if tour exists and is active
            var tour = await _context.Tours
                .Include(t => t.TourGuide)
                .FirstOrDefaultAsync(t => t.Id == tourId && t.Active);

            if (tour == null)
                throw new Exception("Tour not found or not available");

            // Default to 1 participant
            int participantsCount = 1;

            // Check if tour is available
            if (!await IsTourAvailableAsync(tourId, participantsCount))
                throw new Exception($"Tour is fully booked");

            // Calculate total price based on tour base price
            decimal totalPrice = (tour.BasePriceUsd ?? 0) * participantsCount;

            var booking = new TourBooking
            {
                UserId = userId,
                TourId = tourId,
                TourGuideId = tour.TourGuideId,
                BookingDate = DateTime.UtcNow,
                TourDate = null,
                TourTime = null,
                ParticipantsCount = participantsCount,
                TotalPrice = totalPrice,
                Currency = tour.Currency,
                SpecialRequests = null,
                PaymentStatus = PaymentStatus.Pending,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.TourBookings.Add(booking);
            await _context.SaveChangesAsync();

            return await MapToDto(booking);
        }

        public async Task<BookingResponseDto> UpdateBookingParticipantsAsync(long userId, long bookingId, List<ParticipantDto> participants)
        {
            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Phones)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.EmergencyNumbers)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId && b.Status == BookingStatus.Pending);

            if (booking == null)
                throw new Exception("Booking not found or already processed");

            // Validate participants count matches
            if (booking.ParticipantsCount != participants.Count)
                throw new Exception($"ParticipantsCount ({booking.ParticipantsCount}) must match the number of participants provided ({participants.Count})");

            // Remove existing participants
            _context.TourBookingParticipants.RemoveRange(booking.Participants);

            // Add new participants
            decimal totalPrice = 0;
            foreach (var participantDto in participants)
            {
                var participant = new TourBookingParticipant
                {
                    ParticipantType = participantDto.ParticipantType,
                    AgeType = participantDto.AgeType,
                    FirstName = participantDto.FirstName,
                    MiddleName = participantDto.MiddleName,
                    LastName = participantDto.LastName,
                    DateOfBirth = participantDto.DateOfBirth,
                    Gender = participantDto.Gender,
                    Nationality = participantDto.Nationality,
                    SpecialNeeds = participantDto.SpecialNeeds,
                    DietaryRequirements = participantDto.DietaryRequirements,
                    Price = participantDto.Price
                };

                // Add phone numbers
                if (participantDto.PhoneNumbers != null)
                {
                    foreach (var phone in participantDto.PhoneNumbers)
                    {
                        participant.Phones.Add(new TourParticipantPhone { PhoneNumber = phone });
                    }
                }

                // Add emergency contacts
                if (participantDto.EmergencyContacts != null)
                {
                    foreach (var emergency in participantDto.EmergencyContacts)
                    {
                        participant.EmergencyNumbers.Add(new TourParticipantEmergencyNumber
                        {
                            EmergencyName = emergency.EmergencyName,
                            PhoneNumber = emergency.PhoneNumber
                        });
                    }
                }

                booking.Participants.Add(participant);
                totalPrice += participantDto.Price;
            }

            // Update total price
            booking.TotalPrice = totalPrice;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToDto(booking);
        }

        public async Task<BookingResponseDto> ProcessPaymentAsync(long userId, ProcessPaymentDto model)
        {
            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.Participants)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.UserId == userId && b.Status == BookingStatus.Pending);

            if (booking == null)
                throw new Exception("Booking not found or already processed");

            // Check availability again before payment
            if (!await IsTourAvailableAsync(booking.TourId, booking.ParticipantsCount))
                throw new Exception("Tour is no longer available");

            // Calculate commission
            decimal platformCommission = booking.TotalPrice * (model.PlatformCommissionPercentage / 100);
            decimal providerNetAmount = booking.TotalPrice - platformCommission;

            // Create payment record
            var payment = new TourBookingPayment
            {
                UserId = userId,
                BookingId = booking.Id,
                StripePaymentIntentId = model.StripePaymentIntentId,
                UserSavedCardId = model.UserSavedCardId,
                AmountPaid = booking.TotalPrice,
                Currency = booking.Currency,
                Status = PaymentStatus.Completed,
                PlatformCommission = platformCommission,
                ProviderNetAmount = providerNetAmount,
                PayoutStatus = PayoutStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Update booking status from Pending to Confirmed
            booking.PaymentStatus = PaymentStatus.Completed;
            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.TourBookingPayments.Add(payment);
            await _context.SaveChangesAsync();

            return await MapToDto(booking);
        }

        public async Task<bool> CancelBookingAsync(long userId, long bookingId)
        {
            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return false;

            // Check cancellation policy with AvailableDateTime
            if (booking.Tour != null && booking.Tour.AvailableDateTime.HasValue)
            {
                int policyHours = (int)booking.Tour.CancellationPolicy;
                var timeUntilTour = booking.Tour.AvailableDateTime.Value - DateTime.UtcNow;

                if (timeUntilTour.TotalHours < policyHours && timeUntilTour.TotalHours > 0)
                {
                    throw new Exception($"Cannot cancel booking. The cancellation policy requires at least {policyHours} hours notice before the tour starts.");
                }
                else if (timeUntilTour.TotalHours <= 0)
                {
                    throw new Exception("Cannot cancel booking. The tour has already started or passed.");
                }
            }

            // If booking is confirmed, we might want to handle refunds here
            // For now, we'll just mark it as cancelled
            if (booking.Status == BookingStatus.Confirmed)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                // If still pending, remove it completely
                _context.TourBookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            
            return true;
        }

        public async Task<List<BookingResponseDto>> GetUserBookingsAsync(long userId, BookingStatus? status = null)
        {
            var query = _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Phones)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.EmergencyNumbers)
                .Where(b => b.UserId == userId);

            // Filter by status if provided
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var dtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                dtos.Add(await MapToDto(booking));
            }
            return dtos;
        }

        public async Task<List<BookingResponseDto>> GetAssignedBookingsAsync(long tourGuideId, BookingStatus? status = null)
        {
            var query = _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Phones)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.EmergencyNumbers)
                .Where(b => b.TourGuideId == tourGuideId);

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var dtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                dtos.Add(await MapToDto(booking));
            }
            return dtos;
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(long userId, long bookingId)
        {
            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.Phones)
                .Include(b => b.Participants)
                    .ThenInclude(p => p.EmergencyNumbers)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return null;
            return await MapToDto(booking);
        }

        public async Task<List<BookingResponseDto>> GetAllBookingsAsync(BookingStatus? status = null)
        {
            var query = _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var dtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                dtos.Add(await MapToDto(booking));
            }
            return dtos;
        }

        public async Task<BookingResponseDto> GetBookingByAdminAsync(long bookingId)
        {
            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;
            return await MapToDto(booking);
        }

        public async Task<bool> IsTourAvailableAsync(long tourId, int participantsCount)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId);
            if (tour == null || !tour.Active) return false;

            if (!tour.GroupSizeMax.HasValue) return true; // No limit

            // Count confirmed bookings
            var confirmedParticipants = await _context.TourBookings
                .Where(b => b.TourId == tourId && b.Status == BookingStatus.Confirmed)
                .SumAsync(b => b.ParticipantsCount);

            return (confirmedParticipants + participantsCount) <= tour.GroupSizeMax.Value;
        }

        public async Task<List<BookingResponseDto>> GetUserTripsAsync(long userId, string tab)
        {
            var query = _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Include(b => b.Participants)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (tab == "upcoming")
                query = query.Where(b => b.Status != BookingStatus.Cancelled && b.TourDate.HasValue && b.TourDate.Value > DateTime.UtcNow);
            else if (tab == "past")
                query = query.Where(b => b.Status != BookingStatus.Cancelled && b.TourDate.HasValue && b.TourDate.Value <= DateTime.UtcNow);
            else if (tab == "cancelled")
                query = query.Where(b => b.Status == BookingStatus.Cancelled);

            var bookings = await query.OrderByDescending(b => b.TourDate).ToListAsync();
            var dtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                dtos.Add(await MapToDto(booking));
            }
            return dtos;
        }

        private async Task<BookingResponseDto> MapToDto(TourBooking booking)
        {
            if (booking.Tour == null)
                await _context.Entry(booking).Reference(b => b.Tour).LoadAsync();
            if (booking.User == null)
                await _context.Entry(booking).Reference(b => b.User).LoadAsync();
            if (booking.TourGuide == null)
                await _context.Entry(booking).Reference(b => b.TourGuide).LoadAsync();
            if (!booking.Participants.Any())
                await _context.Entry(booking).Collection(b => b.Participants).LoadAsync();

            var tourDate = booking.TourDate ?? DateTime.Now;

            return new BookingResponseDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                UserName = booking.User?.Name ?? "Unknown User",
                TourId = booking.TourId,
                TourTitle = booking.Tour?.TourTitle ?? "Unknown",
                TourGuideId = booking.TourGuideId,
                TourGuideName = booking.TourGuide?.Name ?? "Unknown",
                BookingDate = booking.BookingDate,
                TourDate = booking.TourDate,
                TourTime = booking.TourTime,
                ParticipantsCount = booking.ParticipantsCount,
                TotalPrice = booking.TotalPrice,
                Currency = booking.Currency,
                SpecialRequests = booking.SpecialRequests,
                PaymentStatus = booking.PaymentStatus,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                PrimaryImageUrl = booking.Tour?.TourImages?.FirstOrDefault()?.ImageUrl,
                UiBadge = booking.Status == BookingStatus.Cancelled ? "Cancelled" : (tourDate > DateTime.UtcNow ? "Upcoming" : "Completed"),
                CanCancel = booking.Status != BookingStatus.Cancelled && tourDate > DateTime.UtcNow.AddHours(48),
                CanReview = booking.Status == BookingStatus.Completed || (booking.Status == BookingStatus.Confirmed && tourDate <= DateTime.UtcNow),
                Participants = booking.Participants.Select(p => new ParticipantResponseDto
                {
                    Id = p.Id,
                    ParticipantType = p.ParticipantType,
                    AgeType = p.AgeType,
                    FirstName = p.FirstName,
                    MiddleName = p.MiddleName,
                    LastName = p.LastName,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    Nationality = p.Nationality,
                    SpecialNeeds = p.SpecialNeeds,
                    DietaryRequirements = p.DietaryRequirements,
                    Price = p.Price,
                    PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList(),
                    EmergencyContacts = p.EmergencyNumbers.Select(e => new EmergencyContactResponseDto
                    {
                        Id = e.Id,
                        EmergencyName = e.EmergencyName,
                        PhoneNumber = e.PhoneNumber
                    }).ToList()
                }).ToList()
            };
        }
    }
}




