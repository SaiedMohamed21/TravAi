using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.Models
{
    public class Tour
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("TourGuide")]
        public long TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        public string? City { get; set; }
        
        [Required]
        public string TourTitle { get; set; }
        
        public string? TourType { get; set; }
        public string? TourDescription { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BasePriceUsd { get; set; }
        
        public string Currency { get; set; } = "USD";
        public int? DurationHours { get; set; }
        public int? GroupSizeMax { get; set; }
        public string? SitesCovered { get; set; }
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; }
        
        public int? NumberOfReviews { get; set; }
        
        [Column(TypeName = "decimal(5,4)")]
        public decimal? TourScore { get; set; }

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
        public string? SourcePlatform { get; set; }
        public DateTime? AvailableDateTime { get; set; }
        public CancellationPolicy CancellationPolicy { get; set; } = CancellationPolicy.Hours24;
        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<TourImage> TourImages { get; set; } = new List<TourImage>();
    }
}
