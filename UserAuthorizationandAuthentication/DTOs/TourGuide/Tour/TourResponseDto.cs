using UserAuthorizationandAuthentication.TourGuide.Models;
using System;
using System.Collections.Generic;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Tour
{
    public class TourResponseDto
    {
        public long Id { get; set; }
        public long TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public string? City { get; set; }
        public string TourTitle { get; set; }
        public string? TourType { get; set; }
        public string? TourDescription { get; set; }
        public decimal? BasePriceUsd { get; set; }
        public string Currency { get; set; }
        public int? DurationHours { get; set; }
        public int? GroupSizeMax { get; set; }
        public string? SitesCovered { get; set; }
        public decimal? Rating { get; set; }
        public int? NumberOfReviews { get; set; }
        public string? StartingPoint { get; set; }
        public string? AgeRestriction { get; set; }
        public bool TransportIncluded { get; set; }
        public bool MealsIncluded { get; set; }
        public bool IsAccessible { get; set; }
        public string? Accessibility { get; set; }
        public bool Customizable { get; set; }
        public string? Season { get; set; }
        public string? IncludedServices { get; set; }
        public string? ExcludedServices { get; set; }
        public string? SafetyMeasures { get; set; }
        public string? BestTimeToVisit { get; set; }
        public string? PickupDetails { get; set; }
        public DateTime? AvailableDateTime { get; set; }
        public CancellationPolicy CancellationPolicy { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? ImageUrl { get; set; }
        public int ReviewsCount { get; set; }
        public string? GuideName { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? Date { get; set; }
        public string? Description { get; set; }
        public List<string> IncludedFeatures { get; set; } = new List<string>();

        public TourGuideResponseInfo? Guide { get; set; }

        public List<TourImageResponseDto> Images { get; set; } = new List<TourImageResponseDto>();
    }

    public class TourGuideResponseInfo
    {
        public string Name { get; set; }
        public string? Image { get; set; }
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
    }

    public class TourImageResponseDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; }
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}



