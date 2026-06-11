using TravAi.TourGuide.Models;
using TourGuide = TravAi.TourGuide.Models.TourGuide;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using TravAi.TourGuide.DTOs.TourGuide;
using TravAi.TourGuide.DTOs.Review;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.TourGuide.Services
{
    public class TourGuideService : ITourGuideService
    {
        private readonly ApplicationDbContext _context;

        public TourGuideService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TourGuideResponseDto> ApplyAsync(long userId, TourGuideApplicationDto model)
        {
            var existingApp = await _context.TourGuides
                .Include(tg => tg.TourGuideEmails)
                .Include(tg => tg.TourGuidePhones)
                .Include(tg => tg.TourGuideLanguages)
                .Include(tg => tg.TourGuideCities)
                .FirstOrDefaultAsync(tg => tg.UserId == userId);
            
            if (existingApp != null)
            {
                if (existingApp.Status == TourGuideStatus.Pending || existingApp.Status == TourGuideStatus.Active)
                {
                    throw new InvalidOperationException("User already has a pending or active tour guide profile.");
                }

                // If rejected or banned, allow re-application by updating the existing profile
                existingApp.Name = model.Name;
                existingApp.Bio = model.Bio;
                existingApp.LicenseId = model.LicenseId;
                existingApp.LicenseCard = model.LicenseCard;
                existingApp.LicenseIdFrontPhoto = model.LicenseIdFrontPhoto;
                existingApp.LicenseIdBackPhoto = model.LicenseIdBackPhoto;
                existingApp.Certification = model.Certification;
                existingApp.ExperienceYears = model.ExperienceYears;
                existingApp.Status = TourGuideStatus.Pending;
                existingApp.RejectionReason = null;

                _context.TourGuideEmails.RemoveRange(existingApp.TourGuideEmails);
                if (model.Emails != null && model.Emails.Any())
                {
                    foreach (var email in model.Emails)
                        existingApp.TourGuideEmails.Add(new TourGuideEmail { Email = email, EmailVerified = false });
                }
                else
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                        existingApp.TourGuideEmails.Add(new TourGuideEmail { Email = user.Email, EmailVerified = false });
                }

                _context.TourGuidePhones.RemoveRange(existingApp.TourGuidePhones);
                if (model.PhoneNumbers != null)
                {
                    foreach (var phone in model.PhoneNumbers)
                        existingApp.TourGuidePhones.Add(new TourGuidePhone { PhoneNumber = phone, PhoneVerified = false });
                }

                _context.TourGuideLanguages.RemoveRange(existingApp.TourGuideLanguages);
                if (model.Languages != null)
                {
                    foreach (var lang in model.Languages)
                        existingApp.TourGuideLanguages.Add(new TourGuideLanguage { Language = lang });
                }

                _context.TourGuideCities.RemoveRange(existingApp.TourGuideCities);
                if (model.Cities != null)
                {
                    foreach (var city in model.Cities)
                        existingApp.TourGuideCities.Add(new TourGuideCity { City = city });
                }

                await _context.SaveChangesAsync();
                return await MapToDto(existingApp);
            }

            var tourGuide = new TravAi.TourGuide.Models.TourGuide
            {
                UserId = userId,
                Name = model.Name,
                Bio = model.Bio,
                LicenseId = model.LicenseId,
                LicenseCard = model.LicenseCard,
                LicenseIdFrontPhoto = model.LicenseIdFrontPhoto,
                LicenseIdBackPhoto = model.LicenseIdBackPhoto,
                Certification = model.Certification,
                ExperienceYears = model.ExperienceYears,
                Status = TourGuideStatus.Pending
            };

            // Add related entities
            if (model.Emails != null && model.Emails.Any())
            {
                foreach (var email in model.Emails)
                {
                    tourGuide.TourGuideEmails.Add(new TourGuideEmail { Email = email, EmailVerified = false });
                }
            }
            else
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    tourGuide.TourGuideEmails.Add(new TourGuideEmail { Email = user.Email, EmailVerified = false });
                }
            }

            foreach (var phone in model.PhoneNumbers)
            {
                tourGuide.TourGuidePhones.Add(new TourGuidePhone { PhoneNumber = phone, PhoneVerified = false });
            }

            foreach (var lang in model.Languages)
            {
                tourGuide.TourGuideLanguages.Add(new TourGuideLanguage { Language = lang });
            }

            if (model.Cities != null)
            {
                foreach (var city in model.Cities)
                {
                    tourGuide.TourGuideCities.Add(new TourGuideCity { City = city });
                }
            }

            _context.TourGuides.Add(tourGuide);
            await _context.SaveChangesAsync();

            return await MapToDto(tourGuide);
        }

        public async Task<List<TourGuideResponseDto>> GetAllApplicationsAsync()
        {
            var applications = await _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.TourGuideEmails)
                .Include(tg => tg.TourGuidePhones)
                .Include(tg => tg.TourGuideLanguages)
                .Include(tg => tg.TourGuideCities)
                .Where(tg => tg.Status == TourGuideStatus.Pending) // Show only pending applications
                .ToListAsync();

            var dtos = new List<TourGuideResponseDto>();
            foreach (var app in applications)
            {
                dtos.Add(await MapToDto(app));
            }
            return dtos;
        }

        public async Task<TourGuideResponseDto> GetApplicationByIdAsync(long id)
        {
            var app = await _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.TourGuideEmails)
                .Include(tg => tg.TourGuidePhones)
                .Include(tg => tg.TourGuideLanguages)
                .Include(tg => tg.TourGuideCities)
                .FirstOrDefaultAsync(tg => tg.Id == id);

            if (app == null) return null;

            return await MapToDto(app);
        }

        public async Task<TourGuideResponseDto> GetTourGuideByUserIdAsync(long userId)
        {
            var app = await _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.TourGuideEmails)
                .Include(tg => tg.TourGuidePhones)
                .Include(tg => tg.TourGuideLanguages)
                .Include(tg => tg.TourGuideCities)
                .FirstOrDefaultAsync(tg => tg.UserId == userId);

            if (app == null) return null;

            return await MapToDto(app);
        }

        public async Task<bool> ApproveApplicationAsync(long id)
        {
            var app = await _context.TourGuides.Include(tg => tg.User).FirstOrDefaultAsync(tg => tg.Id == id);
            if (app == null) return false;

            app.Status = TourGuideStatus.Active;
            app.RejectionReason = null; // Clear any previous rejection reasons
            
            // Should verification of emails/phones be enforced here? 
            // The requirement says "verify request", assuming this means admin approval overrides individual verifications or implies them.
            // For now, we just approve the TourGuide status.

            app.User.Role = UserRole.Tourguide;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectApplicationAsync(long id, string reason)
        {
            var app = await _context.TourGuides.FirstOrDefaultAsync(tg => tg.Id == id);
            if (app == null) return false;

            app.Status = TourGuideStatus.Rejected;
            app.RejectionReason = reason;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BanTourGuideAsync(long id)
        {
            var app = await _context.TourGuides.Include(tg => tg.User).FirstOrDefaultAsync(tg => tg.Id == id);
            if (app == null) return false;

            app.Status = TourGuideStatus.Banned;
            
            // Optionally, we could also revoke the "TourGuide" role from User, 
            // but keeping the role might be useful for tracking or if we want them to see they are banned.
            // But usually we should revoke so they can't access tourguide endpoints.
            // app.User.Role = UserRole.User; 
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SuspendTourGuideAsync(long tourGuideId, SuspendTourGuideDto model)
        {
            var app = await _context.TourGuides.FirstOrDefaultAsync(tg => tg.Id == tourGuideId);
            if (app == null) return false;

            app.Status = TourGuideStatus.Suspended;

            DateTime suspendTime = DateTime.UtcNow;
            switch (model.Unit)
            {
                case SuspensionUnit.Hours:
                    suspendTime = suspendTime.AddHours(model.Duration);
                    break;
                case SuspensionUnit.Days:
                    suspendTime = suspendTime.AddDays(model.Duration);
                    break;
                case SuspensionUnit.Weeks:
                    suspendTime = suspendTime.AddDays(model.Duration * 7);
                    break;
                case SuspensionUnit.Years:
                    suspendTime = suspendTime.AddYears(model.Duration);
                    break;
            }

            app.SuspendedUntil = suspendTime;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLicenseAsync(long tourGuideId, UpdateLicenseDto model)
        {
            var tourGuide = await _context.TourGuides.FirstOrDefaultAsync(tg => tg.Id == tourGuideId);
            if (tourGuide == null) return false;

            if (!string.IsNullOrEmpty(model.LicenseId)) tourGuide.LicenseId = model.LicenseId;
            if (!string.IsNullOrEmpty(model.LicenseCard)) tourGuide.LicenseCard = model.LicenseCard;

            if (!string.IsNullOrEmpty(model.LicenseIdFrontPhoto)) tourGuide.LicenseIdFrontPhoto = model.LicenseIdFrontPhoto;
            if (!string.IsNullOrEmpty(model.LicenseIdBackPhoto)) tourGuide.LicenseIdBackPhoto = model.LicenseIdBackPhoto;
            
            // Set status to Pending so admin must re-approve
            tourGuide.Status = TourGuideStatus.Pending;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(long tourGuideId, UpdateProfileDto model)
        {
            var tourGuide = await _context.TourGuides
                .Include(tg => tg.TourGuideEmails)
                .Include(tg => tg.TourGuidePhones)
                .Include(tg => tg.TourGuideLanguages)
                .Include(tg => tg.TourGuideCities)
                .FirstOrDefaultAsync(tg => tg.Id == tourGuideId);
            
            if (tourGuide == null) return false;

            if (!string.IsNullOrEmpty(model.Name)) tourGuide.Name = model.Name;
            if (model.Bio != null) tourGuide.Bio = model.Bio; // allows clearing if empty string
            if (model.Certification != null) tourGuide.Certification = model.Certification;
            if (model.ExperienceYears.HasValue) tourGuide.ExperienceYears = model.ExperienceYears.Value;

            if (model.Emails != null)
            {
                _context.TourGuideEmails.RemoveRange(tourGuide.TourGuideEmails);
                foreach (var email in model.Emails)
                    tourGuide.TourGuideEmails.Add(new TourGuideEmail { Email = email, EmailVerified = false });
            }

            if (model.PhoneNumbers != null)
            {
                _context.TourGuidePhones.RemoveRange(tourGuide.TourGuidePhones);
                foreach (var phone in model.PhoneNumbers)
                    tourGuide.TourGuidePhones.Add(new TourGuidePhone { PhoneNumber = phone, PhoneVerified = false });
            }

            if (model.Languages != null)
            {
                _context.TourGuideLanguages.RemoveRange(tourGuide.TourGuideLanguages);
                foreach (var lang in model.Languages)
                    tourGuide.TourGuideLanguages.Add(new TourGuideLanguage { Language = lang });
            }

            if (model.Cities != null)
            {
                _context.TourGuideCities.RemoveRange(tourGuide.TourGuideCities);
                foreach (var city in model.Cities)
                    tourGuide.TourGuideCities.Add(new TourGuideCity { City = city });
            }

            // Put them back in Pending status for Admin review
            tourGuide.Status = TourGuideStatus.Pending;
            tourGuide.RejectionReason = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReviewDto>> GetTourGuideReviewsAsync(long tourGuideId)
        {
            var reviews = await _context.TourReviews
                .Include(r => r.User)
                .Where(r => r.TourGuideId == tourGuideId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User?.UserName ?? "Unknown",
                TourId = r.TourId,
                TourGuideId = r.TourGuideId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<TourGuideReviewsDashboardDto> GetTourGuideReviewsDashboardAsync(long tourGuideId)
        {
            var reviewsQuery = _context.TourReviews
                .Include(r => r.User)
                .Include(r => r.Tour)
                .Where(r => r.TourGuideId == tourGuideId);

            var totalReviews = await reviewsQuery.CountAsync();
            var averageRating = totalReviews > 0 ? await reviewsQuery.AverageAsync(r => r.Rating) : 0;

            var fiveStars = await reviewsQuery.CountAsync(r => r.Rating == 5);
            var fourStars = await reviewsQuery.CountAsync(r => r.Rating == 4);
            var threeStars = await reviewsQuery.CountAsync(r => r.Rating == 3);
            var twoStars = await reviewsQuery.CountAsync(r => r.Rating == 2);
            var oneStars = await reviewsQuery.CountAsync(r => r.Rating == 1);

            var reviews = await reviewsQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(50) // limit to 50 for dashboard, or we can use pagination
                .ToListAsync();

            return new TourGuideReviewsDashboardDto
            {
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                RatingBreakdown = new RatingBreakdownDto
                {
                    FiveStars = fiveStars,
                    FourStars = fourStars,
                    ThreeStars = threeStars,
                    TwoStars = twoStars,
                    OneStars = oneStars
                },
                Reviews = reviews.Select(r => new TourGuideDashboardReviewDto
                {
                    Id = r.Id,
                    TourId = r.TourId,
                    TourTitle = r.Tour?.TourTitle,
                    ReviewerName = r.User?.UserName ?? "Unknown",
                    ReviewerImage = r.User?.ProfilePic,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
        }

        public async Task<TourGuideProfileDto> GetProfileAsync(long id)
        {
            var guide = await _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.TourGuideLanguages)
                .FirstOrDefaultAsync(tg => tg.Id == id);

            if (guide == null) return null;

            // Calculate rating and reviews count from tours
            var tours = await _context.Tours.Where(t => t.TourGuideId == id).ToListAsync();
            decimal avgRating = tours.Where(t => t.Rating.HasValue).Any() ? tours.Average(t => t.Rating.Value) : 0;
            int totalReviews = tours.Sum(t => t.NumberOfReviews ?? 0);

            return new TourGuideProfileDto
            {
                Id = guide.Id,
                Name = guide.Name,
                Image = guide.User?.ProfilePic,
                Languages = guide.TourGuideLanguages.Select(l => l.Language.ToString()).ToList(),
                Rating = Math.Round(avgRating, 1),
                ReviewsCount = totalReviews,
                ExperienceYears = guide.ExperienceYears
            };
        }

        private async Task<TourGuideResponseDto> MapToDto(TravAi.TourGuide.Models.TourGuide tg)
        {
            // Ensure user is loaded if not already
            if (tg.User == null)
            {
                await _context.Entry(tg).Reference(t => t.User).LoadAsync();
            }

            return new TourGuideResponseDto
            {
                Id = tg.Id,
                UserId = tg.UserId,
                UserName = tg.User.UserName,
                Name = tg.Name,
                Bio = tg.Bio,
                LicenseId = tg.LicenseId,
                LicenseCard = tg.LicenseCard,
                LicenseIdFrontPhoto = tg.LicenseIdFrontPhoto,
                LicenseIdBackPhoto = tg.LicenseIdBackPhoto,
                Certification = tg.Certification,
                Status = tg.Status.ToString(),
                RejectionReason = tg.RejectionReason,
                ExperienceYears = tg.ExperienceYears,
                Emails = tg.TourGuideEmails.Select(e => new TourGuideEmailDto { Email = e.Email, Verified = e.EmailVerified }).ToList(),
                Phones = tg.TourGuidePhones.Select(p => new TourGuidePhoneDto { PhoneNumber = p.PhoneNumber, Verified = p.PhoneVerified }).ToList(),
                Languages = tg.TourGuideLanguages.Select(l => new TourGuideLanguageDto { Language = l.Language }).ToList(),
                Cities = tg.TourGuideCities?.Select(c => new TourGuideCityDto { City = c.City }).ToList() ?? new List<TourGuideCityDto>()
            };
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(long tourGuideId)
        {
            var tours = await _context.Tours.Where(t => t.TourGuideId == tourGuideId).ToListAsync();
            var bookings = await _context.TourBookings.Where(b => b.TourGuideId == tourGuideId).ToListAsync();

            var totalTours = tours.Count;
            var upcomingTours = tours.Count(t => t.AvailableDateTime > DateTime.UtcNow);
            var totalBookings = bookings.Count(b => b.Status != TravAi.TourGuide.Models.Enums.BookingStatus.Cancelled);
            var earnings = bookings
                .Where(b => b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Completed || b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Confirmed)
                .Sum(b => b.TotalPrice);

            return new DashboardSummaryDto
            {
                TotalTours = totalTours,
                UpcomingTours = upcomingTours,
                TotalBookings = totalBookings,
                Earnings = earnings
            };
        }

        public async Task<List<EarningsChartDto>> GetEarningsChartAsync(long tourGuideId)
        {
            var bookings = await _context.TourBookings
                .Where(b => b.TourGuideId == tourGuideId && 
                           (b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Completed || b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Confirmed) &&
                           b.CreatedAt >= DateTime.UtcNow.AddMonths(-5))
                .ToListAsync();

            var chart = bookings
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new EarningsChartDto
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Amount = g.Sum(b => b.TotalPrice)
                })
                .ToList();

            var result = new List<EarningsChartDto>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.UtcNow.AddMonths(-i);
                var monthStr = targetMonth.ToString("MMM");
                var existing = chart.FirstOrDefault(c => c.Month == monthStr);
                if (existing != null)
                {
                    result.Add(existing);
                }
                else
                {
                    result.Add(new EarningsChartDto { Month = monthStr, Amount = 0 });
                }
            }

            return result;
        }

        public async Task<TourGuideEarningsSummaryDto> GetEarningsSummaryAsync(long tourGuideId)
        {
            var bookings = await _context.TourBookings
                .Where(b => b.TourGuideId == tourGuideId)
                .ToListAsync();

            var completedBookingsList = bookings
                .Where(b => b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Completed || b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Confirmed)
                .ToList();

            var totalEarnings = completedBookingsList.Sum(b => b.TotalPrice);
            var completedCount = completedBookingsList.Count;
            var averageBookingValue = completedCount > 0 ? totalEarnings / completedCount : 0;

            var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var currentMonthEarnings = completedBookingsList
                .Where(b => b.CreatedAt >= currentMonthStart)
                .Sum(b => b.TotalPrice);

            var pendingWithdrawals = await _context.WithdrawRequests
                .Where(w => w.TourGuideId == tourGuideId && w.Status == TravAi.TourGuide.Models.Enums.WithdrawRequestStatus.Pending)
                .SumAsync(w => w.Amount);

            return new TourGuideEarningsSummaryDto
            {
                TotalEarnings = totalEarnings,
                CurrentMonthEarnings = currentMonthEarnings,
                PendingWithdrawals = pendingWithdrawals,
                CompletedBookings = completedCount,
                AverageBookingValue = Math.Round(averageBookingValue, 2),
                Currency = "USD"
            };
        }
    }
}
