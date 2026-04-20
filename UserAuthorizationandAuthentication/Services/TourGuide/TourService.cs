using UserAuthorizationandAuthentication.TourGuide.Models;
using Tour = UserAuthorizationandAuthentication.TourGuide.Models.Tour;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using UserAuthorizationandAuthentication.TourGuide.DTOs.Tour;
using UserAuthorizationandAuthentication.TourGuide.DTOs.Review;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.Services
{
    public class TourService : ITourService
    {
        private readonly ApplicationDbContext _context;

        public TourService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TourResponseDto> CreateTourAsync(long tourGuideId, CreateTourDto model)
        {
            var tour = new Tour
            {
                TourGuideId = tourGuideId,
                City = model.City,
                TourTitle = model.TourTitle,
                TourType = model.TourType,
                TourDescription = model.TourDescription,
                BasePriceUsd = model.BasePriceUsd,
                Currency = model.Currency,
                DurationHours = model.DurationHours,
                GroupSizeMax = model.GroupSizeMax,
                SitesCovered = model.SitesCovered,
                StartingPoint = model.StartingPoint,
                AgeRestriction = model.AgeRestriction,
                TransportIncluded = model.TransportIncluded,
                MealsIncluded = model.MealsIncluded,
                IsAccessible = model.IsAccessible,
                Accessibility = model.Accessibility,
                Customizable = model.Customizable,
                Season = model.Season,
                IncludedServices = model.IncludedServices,
                ExcludedServices = model.ExcludedServices,
                SafetyMeasures = model.SafetyMeasures,
                BestTimeToVisit = model.BestTimeToVisit,
                PickupDetails = model.PickupDetails,
                AvailableDateTime = model.AvailableDateTime,
                CancellationPolicy = model.CancellationPolicy,
                Active = model.Active,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var img in model.Images)
            {
                tour.TourImages.Add(new TourImage
                {
                    ImageUrl = img.ImageUrl,
                    Caption = img.Caption,
                    IsPrimary = img.IsPrimary,
                    SortOrder = img.SortOrder
                });
            }

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return await MapToDto(tour, true);
        }

        public async Task<TourResponseDto> UpdateTourAsync(long tourId, long tourGuideId, UpdateTourDto model)
        {
            var tour = await _context.Tours
                .Include(t => t.TourImages)
                .FirstOrDefaultAsync(t => t.Id == tourId && t.TourGuideId == tourGuideId);

            if (tour == null) return null;

            tour.City = model.City;
            tour.TourTitle = model.TourTitle;
            tour.TourType = model.TourType;
            tour.TourDescription = model.TourDescription;
            tour.BasePriceUsd = model.BasePriceUsd;
            tour.Currency = model.Currency;
            tour.DurationHours = model.DurationHours;
            tour.GroupSizeMax = model.GroupSizeMax;
            tour.SitesCovered = model.SitesCovered;
            tour.StartingPoint = model.StartingPoint;
            tour.AgeRestriction = model.AgeRestriction;
            tour.TransportIncluded = model.TransportIncluded;
            tour.MealsIncluded = model.MealsIncluded;
            tour.IsAccessible = model.IsAccessible;
            tour.Accessibility = model.Accessibility;
            tour.Customizable = model.Customizable;
            tour.Season = model.Season;
            tour.IncludedServices = model.IncludedServices;
            tour.ExcludedServices = model.ExcludedServices;
            tour.SafetyMeasures = model.SafetyMeasures;
            tour.BestTimeToVisit = model.BestTimeToVisit;
            tour.PickupDetails = model.PickupDetails;
            tour.AvailableDateTime = model.AvailableDateTime;
            tour.CancellationPolicy = model.CancellationPolicy;
            tour.Active = model.Active;
            tour.UpdatedAt = DateTime.UtcNow;

            // Update images
            _context.TourImages.RemoveRange(tour.TourImages);
            tour.TourImages.Clear();

            foreach (var img in model.Images)
            {
                tour.TourImages.Add(new TourImage
                {
                    ImageUrl = img.ImageUrl,
                    Caption = img.Caption,
                    IsPrimary = img.IsPrimary,
                    SortOrder = img.SortOrder
                });
            }

            await _context.SaveChangesAsync();
            return await MapToDto(tour, true);
        }

        public async Task<bool> DeleteTourAsync(long tourId, long tourGuideId)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId && t.TourGuideId == tourGuideId);
            if (tour == null) return false;

            var hasBookings = await _context.TourBookings.AnyAsync(b => b.TourId == tourId);
            if (hasBookings)
            {
                throw new InvalidOperationException("Cannot delete a tour that has bookings.");
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdminDeleteTourAsync(long tourId)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId);
            if (tour == null) return false;

            var hasBookings = await _context.TourBookings.AnyAsync(b => b.TourId == tourId);
            if (hasBookings)
            {
                throw new InvalidOperationException("Cannot delete a tour that has bookings.");
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TourResponseDto> GetTourByIdAsync(long tourId)
        {
            var tour = await _context.Tours
                .Include(t => t.TourGuide)
                .Include(t => t.TourImages)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null) return null;
            return await MapToDto(tour, true);
        }

        public async Task<List<TourResponseDto>> GetToursByTourGuideAsync(long tourGuideId)
        {
            var tours = await _context.Tours
                .Include(t => t.TourGuide)
                .Include(t => t.TourImages)
                .Where(t => t.TourGuideId == tourGuideId)
                .ToListAsync();

            var dtos = new List<TourResponseDto>();
            foreach (var tour in tours)
            {
                dtos.Add(await MapToDto(tour));
            }
            return dtos;
        }

        public async Task<(List<TourResponseDto> Tours, int TotalCount)> GetToursWithFiltersAsync(TourFilterDto filters)
        {
            var query = _context.Tours
                .Include(t => t.TourGuide)
                    .ThenInclude(tg => tg.TourGuideLanguages)
                .Include(t => t.TourImages)
                .Where(t => t.Active)
                .AsQueryable();

            // Price filter
            if (filters.MinPrice.HasValue)
                query = query.Where(t => t.BasePriceUsd >= filters.MinPrice.Value);
            if (filters.MaxPrice.HasValue)
                query = query.Where(t => t.BasePriceUsd <= filters.MaxPrice.Value);

            // Date filter (based on CreatedAt for now, can be changed to tour dates if needed)
            if (filters.StartDate.HasValue)
                query = query.Where(t => t.CreatedAt >= filters.StartDate.Value);
            if (filters.EndDate.HasValue)
                query = query.Where(t => t.CreatedAt <= filters.EndDate.Value);

            // Language filter
            if (!string.IsNullOrEmpty(filters.Languages))
            {
                var enumLanguages = new List<Language>();
                foreach (var l in filters.Languages.Split(','))
                {
                    if (Enum.TryParse<Language>(l.Trim(), true, out var parsedEnum))
                    {
                        enumLanguages.Add(parsedEnum);
                    }
                }
                if (enumLanguages.Any())
                {
                    query = query.Where(t => t.TourGuide.TourGuideLanguages.Any(tgl => enumLanguages.Contains(tgl.Language)));
                }
            }

            // City filter
            if (!string.IsNullOrEmpty(filters.City))
                query = query.Where(t => t.City.ToLower().Contains(filters.City.ToLower()));

            // Tour type filter
            if (!string.IsNullOrEmpty(filters.TourType))
                query = query.Where(t => t.TourType.ToLower().Contains(filters.TourType.ToLower()));

            // Sorting
            query = filters.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(t => t.BasePriceUsd),
                "price_desc" => query.OrderByDescending(t => t.BasePriceUsd),
                "rating" => query.OrderByDescending(t => t.Rating),
                "popular" => query.OrderByDescending(t => t.NumberOfReviews),
                "recent" => query.OrderByDescending(t => t.CreatedAt),
                "tour_score" => query.OrderByDescending(t => t.TourScore),
                "duration" => query.OrderByDescending(t => t.DurationHours),
                _ => query.OrderByDescending(t => t.TourScore) // Default to TourScore
            };

            var totalCount = await query.CountAsync();

            // Pagination
            var tours = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync();

            var dtos = new List<TourResponseDto>();
            foreach (var tour in tours)
            {
                dtos.Add(await MapToDto(tour));
            }

            return (dtos, totalCount);
        }

        public async Task<List<ReviewDto>> GetTourReviewsAsync(long tourId)
        {
            var reviews = await _context.TourReviews
                .Include(r => r.User)
                .Where(r => r.TourId == tourId)
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

        private async Task<TourResponseDto> MapToDto(Tour tour, bool isDetail = false)
        {
            if (tour.TourGuide == null)
            {
                await _context.Entry(tour).Reference(t => t.TourGuide).LoadAsync();
            }

            if (tour.TourGuide != null)
            {
                await _context.Entry(tour.TourGuide).Collection(tg => tg.TourGuideLanguages).LoadAsync();
            }

            if (!tour.TourImages.Any())
            {
                await _context.Entry(tour).Collection(t => t.TourImages).LoadAsync();
            }

            var langs = tour.TourGuide?.TourGuideLanguages?.Select(l => l.Language.ToString()).ToList() ?? new List<string>();

            var features = string.IsNullOrWhiteSpace(tour.IncludedServices) 
                ? new List<string>() 
                : tour.IncludedServices.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList();

            var primaryImage = tour.TourImages.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsPrimary) 
                               ?? tour.TourImages.OrderBy(i => i.SortOrder).FirstOrDefault();

            TourGuideResponseInfo? guideInfo = null;

            if (isDetail && tour.TourGuide != null)
            {
                await _context.Entry(tour.TourGuide).Reference(tg => tg.User).LoadAsync();
                
                var guideTours = await _context.Tours.Where(t => t.TourGuideId == tour.TourGuideId).ToListAsync();
                decimal guideAvgRating = guideTours.Where(t => t.Rating.HasValue).Any() ? guideTours.Average(t => t.Rating.Value) : 0;
                int guideTotalReviews = guideTours.Sum(t => t.NumberOfReviews ?? 0);

                guideInfo = new TourGuideResponseInfo
                {
                    Name = tour.TourGuide.Name,
                    Image = tour.TourGuide.User?.ProfilePic,
                    Rating = Math.Round(guideAvgRating, 2),
                    ReviewsCount = guideTotalReviews,
                    Languages = langs
                };
            }

            return new TourResponseDto
            {
                Id = tour.Id,
                TourGuideId = tour.TourGuideId,
                TourGuideName = tour.TourGuide?.Name ?? "Unknown",
                City = tour.City,
                TourTitle = tour.TourTitle,
                TourType = tour.TourType,
                TourDescription = tour.TourDescription,
                BasePriceUsd = tour.BasePriceUsd,
                Currency = tour.Currency,
                DurationHours = tour.DurationHours,
                GroupSizeMax = tour.GroupSizeMax,
                SitesCovered = tour.SitesCovered,
                Rating = tour.Rating,
                NumberOfReviews = tour.NumberOfReviews,
                StartingPoint = tour.StartingPoint,
                AgeRestriction = tour.AgeRestriction,
                TransportIncluded = tour.TransportIncluded,
                MealsIncluded = tour.MealsIncluded,
                IsAccessible = tour.IsAccessible,
                Accessibility = tour.Accessibility,
                Customizable = tour.Customizable,
                Season = tour.Season,
                IncludedServices = tour.IncludedServices,
                ExcludedServices = tour.ExcludedServices,
                SafetyMeasures = tour.SafetyMeasures,
                BestTimeToVisit = tour.BestTimeToVisit,
                PickupDetails = tour.PickupDetails,
                AvailableDateTime = tour.AvailableDateTime,
                CancellationPolicy = tour.CancellationPolicy,
                Active = tour.Active,
                CreatedAt = tour.CreatedAt,
                UpdatedAt = tour.UpdatedAt,
                
                ImageUrl = primaryImage?.ImageUrl,
                ReviewsCount = tour.NumberOfReviews ?? 0,
                GuideName = tour.TourGuide?.Name ?? "Unknown",
                Languages = langs,
                StartTime = tour.AvailableDateTime,
                EndTime = tour.AvailableDateTime.HasValue && tour.DurationHours.HasValue ? tour.AvailableDateTime.Value.AddHours(tour.DurationHours.Value) : null,
                Date = tour.AvailableDateTime,
                Description = tour.TourDescription,
                IncludedFeatures = features,
                Guide = guideInfo,

                Images = tour.TourImages.OrderBy(i => i.SortOrder).Select(i => new TourImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    Caption = i.Caption,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                }).ToList()
            };
        }
        public async Task<(List<TourCardDto> Cards, int TotalCount)> GetTourCardsAsync(int page = 1, int pageSize = 10)
        {
            var query = _context.Tours
                .Include(t => t.TourImages)
                .Include(t => t.TourGuide)
                    .ThenInclude(tg => tg.TourGuideLanguages)
                .Where(t => t.Active)
                .OrderByDescending(t => t.TourScore);

            var totalCount = await query.CountAsync();

            var tours = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var cards = new List<TourCardDto>();

            foreach (var tour in tours)
            {
                var reviews = await _context.TourReviews
                    .Where(r => r.TourId == tour.Id)
                    .ToListAsync();

                var rating = reviews.Any() ? (decimal)reviews.Average(r => r.Rating) : 0;
                var reviewsCount = reviews.Count;

                var primaryImage = tour.TourImages.FirstOrDefault(i => i.IsPrimary)?.ImageUrl 
                                   ?? tour.TourImages.FirstOrDefault()?.ImageUrl;

                var languages = tour.TourGuide?.TourGuideLanguages
                    .Select(l => l.Language.ToString())
                    .ToList() ?? new List<string>();

                var date = tour.AvailableDateTime?.ToString("yyyy-MM-dd");
                var startTime = tour.AvailableDateTime?.ToString("HH:mm");
                var endTime = tour.AvailableDateTime.HasValue && tour.DurationHours.HasValue
                    ? tour.AvailableDateTime.Value.AddHours(tour.DurationHours.Value).ToString("HH:mm")
                    : null;

                cards.Add(new TourCardDto
                {
                    Id = tour.Id.ToString(),
                    Title = tour.TourTitle, // Using TourTitle as it fits the example "Pyramids Tour"
                    ImageUrl = primaryImage,
                    Rating = Math.Round(rating, 1),
                    ReviewsCount = reviewsCount,
                    GuideName = tour.TourGuide?.Name ?? "Unknown Guide",
                    Languages = languages,
                    Location = new TourLocationDto
                    {
                        City = tour.City,
                        Country = "Egypt"
                    },
                    Date = date,
                    StartTime = startTime,
                    EndTime = endTime,
                    Price = tour.BasePriceUsd ?? 0,
                    Currency = tour.Currency ?? "USD",
                    TourScore = tour.TourScore
                });
            }

            return (cards, totalCount);
        }
        public async Task<TourDetailsDto> GetTourDetailsAsync(long tourId)
        {
            var tour = await _context.Tours
                .Include(t => t.TourGuide)
                    .ThenInclude(tg => tg.TourGuideLanguages)
                .Include(t => t.TourGuide)
                    .ThenInclude(tg => tg.User)
                .Include(t => t.TourImages)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null) return null;

            // --- Images (slider) ---
            var imageUrls = tour.TourImages
                .OrderBy(i => i.SortOrder)
                .Select(i => i.ImageUrl)
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();

            // --- Guide languages ---
            var languages = tour.TourGuide?.TourGuideLanguages?
                .Select(l => l.Language.ToString())
                .ToList() ?? new List<string>();

            // --- Rating & reviews (from actual reviews table) ---
            var reviews = await _context.TourReviews
                .Where(r => r.TourId == tourId)
                .ToListAsync();

            var rating = reviews.Any() ? Math.Round((decimal)reviews.Average(r => r.Rating), 1) : (tour.Rating ?? 0);
            var reviewsCount = reviews.Any() ? reviews.Count : (tour.NumberOfReviews ?? 0);

            // --- Date / Time ---
            var date      = tour.AvailableDateTime?.ToString("yyyy-MM-dd");
            var startTime = tour.AvailableDateTime?.ToString("HH:mm");
            var endTime   = tour.AvailableDateTime.HasValue && tour.DurationHours.HasValue
                ? tour.AvailableDateTime.Value.AddHours(tour.DurationHours.Value).ToString("HH:mm")
                : null;

            // --- Included / NotIncluded ---
            // IncludedServices -> Included list
            var included = string.IsNullOrWhiteSpace(tour.IncludedServices)
                ? new List<string>()
                : tour.IncludedServices
                    .Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            // ExcludedServices -> NotIncluded list
            var notIncluded = string.IsNullOrWhiteSpace(tour.ExcludedServices)
                ? new List<string>()
                : tour.ExcludedServices
                    .Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            // --- Available seats (GroupSizeMax - confirmed bookings) ---
            var confirmedBookings = await _context.TourBookings
                .Where(b => b.TourId == tourId)
                .SumAsync(b => (int?)b.ParticipantsCount ?? 0);

            int? availableSeats = tour.GroupSizeMax.HasValue
                ? Math.Max(0, tour.GroupSizeMax.Value - confirmedBookings)
                : null;

            return new TourDetailsDto
            {
                Id           = tour.Id.ToString(),
                Title        = tour.TourTitle,
                ImageUrls    = imageUrls,

                Location = new TourLocationDto
                {
                    City    = tour.City,
                    Country = "Egypt"   // Country is not stored per-tour; defaulting to Egypt (the primary market).
                                        // Update this field once a Country column is added to the Tour model.
                },

                Rating       = rating,
                ReviewsCount = reviewsCount,
                TourScore    = tour.TourScore,

                Guide = new TourGuideInfoDto
                {
                    Name      = tour.TourGuide?.Name ?? "Unknown",
                    Image     = tour.TourGuide?.User?.ProfilePic,
                    Languages = languages
                },

                Date          = date,
                StartTime     = startTime,
                EndTime       = endTime,
                DurationHours = tour.DurationHours,
                GroupSize     = tour.GroupSizeMax,
                AvailableSeats = availableSeats,

                Price       = tour.BasePriceUsd ?? 0,
                Currency    = tour.Currency ?? "USD",

                Description = tour.TourDescription,
                Included    = included,
                NotIncluded = notIncluded
            };
        }
    }
}




