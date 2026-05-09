using TravAi.TourGuide.Models;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.UrgentRequest;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;
using PaymentStatus = TravAi.TourGuide.Models.Enums.PaymentStatus;
using BookingStatus = TravAi.TourGuide.Models.Enums.BookingStatus;

namespace TravAi.TourGuide.Services
{
    public class UrgentRequestService : IUrgentRequestService
    {
        private readonly ApplicationDbContext _context;

        public UrgentRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UrgentRequestResponseDto> CreateUrgentRequestAsync(long tourGuideId, CreateUrgentRequestDto model)
        {
            // Verify tour exists and belongs to guide
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == model.TourId && t.TourGuideId == tourGuideId);
            if (tour == null)
            {
                throw new Exception("Tour not found or does not belong to you.");
            }

            var request = new UrgentRequest
            {
                TourGuideId = tourGuideId,
                TourId = model.TourId,
                Reason = model.Reason,
                DocumentationUrl = model.DocumentationUrl,
                Status = UrgentRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.UrgentRequests.Add(request);
            await _context.SaveChangesAsync();

            return await MapToDto(request);
        }

        public async Task<List<UrgentRequestResponseDto>> GetAllPendingRequestsAsync()
        {
            var requests = await _context.UrgentRequests
                .Include(r => r.TourGuide)
                .Include(r => r.Tour)
                .Where(r => r.Status == UrgentRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<UrgentRequestResponseDto>();
            foreach (var r in requests)
            {
                result.Add(await MapToDto(r));
            }
            return result;
        }

        public async Task<List<UrgentRequestResponseDto>> GetMyRequestsAsync(long tourGuideId)
        {
            var requests = await _context.UrgentRequests
                .Include(r => r.TourGuide)
                .Include(r => r.Tour)
                .Where(r => r.TourGuideId == tourGuideId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<UrgentRequestResponseDto>();
            foreach (var r in requests)
            {
                result.Add(await MapToDto(r));
            }
            return result;
        }

        public async Task<UrgentRequestResponseDto> ProcessRequestAsync(long requestId, AdminProcessUrgentRequestDto model)
        {
            var request = await _context.UrgentRequests
                .Include(r => r.TourGuide)
                .Include(r => r.Tour)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("Request not found.");
            }

            if (Enum.TryParse<UrgentRequestStatus>(model.Status, true, out var newStatus))
            {
                request.Status = newStatus;
            }
            else
            {
                throw new Exception("Invalid status provided.");
            }
            
            request.AdminNotes = model.AdminNotes;
            request.ProcessedAt = DateTime.UtcNow;

            // Handle the logic for approved cancellations
            if (request.Status == UrgentRequestStatus.Approved)
            {
                if (request.Tour != null)
                {
                    var originalTour = request.Tour;
                    
                    // Find an alternative tour with the same specifications and time
                    var alternativeTour = await _context.Tours.FirstOrDefaultAsync(t => 
                        t.Id != originalTour.Id && 
                        t.Active == true &&
                        t.AvailableDateTime == originalTour.AvailableDateTime &&
                        t.City == originalTour.City &&
                        t.TourTitle == originalTour.TourTitle &&
                        t.TourType == originalTour.TourType &&
                        t.TourDescription == originalTour.TourDescription &&
                        t.BasePriceUsd == originalTour.BasePriceUsd &&
                        t.DurationHours == originalTour.DurationHours &&
                        t.GroupSizeMax == originalTour.GroupSizeMax &&
                        t.SitesCovered == originalTour.SitesCovered &&
                        t.StartingPoint == originalTour.StartingPoint &&
                        t.AgeRestriction == originalTour.AgeRestriction &&
                        t.TransportIncluded == originalTour.TransportIncluded &&
                        t.MealsIncluded == originalTour.MealsIncluded &&
                        t.IsAccessible == originalTour.IsAccessible &&
                        t.Accessibility == originalTour.Accessibility &&
                        t.Customizable == originalTour.Customizable &&
                        t.Season == originalTour.Season &&
                        t.IncludedServices == originalTour.IncludedServices &&
                        t.ExcludedServices == originalTour.ExcludedServices &&
                        t.SafetyMeasures == originalTour.SafetyMeasures &&
                        t.BestTimeToVisit == originalTour.BestTimeToVisit &&
                        t.PickupDetails == originalTour.PickupDetails &&
                        t.CancellationPolicy == originalTour.CancellationPolicy
                    );

                    var bookings = await _context.TourBookings
                        .Where(b => b.TourId == originalTour.Id && b.Status != BookingStatus.Cancelled)
                        .ToListAsync();

                    if (bookings.Any())
                    {
                        foreach (var booking in bookings)
                        {
                            if (alternativeTour != null)
                            {
                                // Rebook to the alternative tour
                                booking.TourId = alternativeTour.Id;
                                booking.TourGuideId = alternativeTour.TourGuideId;
                                booking.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                // Refund the money paid (by updating statuses)
                                booking.Status = BookingStatus.Cancelled;
                                booking.PaymentStatus = PaymentStatus.Refunded;
                                booking.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }

                    // Deactivate the original tour
                    originalTour.Active = false;
                    originalTour.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return await MapToDto(request);
        }

        private async Task<UrgentRequestResponseDto> MapToDto(UrgentRequest request)
        {
            if (request.TourGuide == null)
            {
                await _context.Entry(request).Reference(r => r.TourGuide).LoadAsync();
            }
            if (request.Tour == null)
            {
                await _context.Entry(request).Reference(r => r.Tour).LoadAsync();
            }

            return new UrgentRequestResponseDto
            {
                Id = request.Id,
                TourGuideId = request.TourGuideId,
                TourGuideName = request.TourGuide?.Name ?? "Unknown",
                TourId = request.TourId,
                TourTitle = request.Tour?.TourTitle ?? "Unknown",
                Reason = request.Reason,
                DocumentationUrl = request.DocumentationUrl,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                ProcessedAt = request.ProcessedAt,
                AdminNotes = request.AdminNotes
            };
        }
    }
}



