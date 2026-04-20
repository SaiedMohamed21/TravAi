using UserAuthorizationandAuthentication.TourGuide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Tour
{
    public class CreateTourDto
    {
        public string? City { get; set; }
        
        [Required]
        public string TourTitle { get; set; }
        
        public string? TourType { get; set; }
        public string? TourDescription { get; set; }
        public decimal? BasePriceUsd { get; set; }
        public string Currency { get; set; } = "USD";
        public int? DurationHours { get; set; }
        public int? GroupSizeMax { get; set; }
        public string? SitesCovered { get; set; }
        public string? StartingPoint { get; set; }
        public string? AgeRestriction { get; set; }
        public bool TransportIncluded { get; set; } = false;
        public bool MealsIncluded { get; set; } = false;
        public bool IsAccessible { get; set; } = false;
        public string? Accessibility { get; set; }
        public bool Customizable { get; set; } = false;
        public string? Season { get; set; }
        public string? IncludedServices { get; set; }
        public string? ExcludedServices { get; set; }
        public string? SafetyMeasures { get; set; }
        public string? BestTimeToVisit { get; set; }
        public string? PickupDetails { get; set; }
        public DateTime? AvailableDateTime { get; set; }
        
        [Required]
        public CancellationPolicy CancellationPolicy { get; set; }
        
        public bool Active { get; set; } = true;

        public List<TourImageDto> Images { get; set; } = new List<TourImageDto>();
    }

    public class UpdateTourDto : CreateTourDto
    {
        // Inherits all fields from CreateTourDto
    }

    public class TourImageDto
    {
        public string ImageUrl { get; set; }
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}



