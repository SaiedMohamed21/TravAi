using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TravAi.Models.Enums;

namespace TravAi.DTOs.Hotel
{
    // --- Application DTOs ---
    public class HotelApplicationRequest
    {
        [Required]
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        [Required]
        public PropertyType PropertyType { get; set; }
        [Required]
        public AccommodationType AccommodationType { get; set; }
        public int StarRating { get; set; } = 1;

        [Required]
        public string Country { get; set; } = "Egypt";
        [Required]
        public string Governorate { get; set; } = string.Empty;
        [Required]
        public string CityArea { get; set; } = string.Empty;
        [Required]
        public string AddressDetails { get; set; } = string.Empty;
        public string? AddressProofUrl { get; set; }
        public IFormFile? AddressProofFile { get; set; }

        public List<CreateRoomRequest>? InitialRooms { get; set; }
        
        // Detailed Data
        public List<HotelContactInputDto>? Contacts { get; set; }
        public List<HotelDocumentInputDto>? Documents { get; set; }
        public List<HotelFieldValueInputDto>? DynamicFields { get; set; }
        public UpdateHotelPolicyInputDto? Policy { get; set; }
        public List<HotelImageInputDto>? Images { get; set; }
        public List<long>? AmenityIds { get; set; }
    }

    public class HotelImageInputDto
    {
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    // --- Active Hotel Management DTOs ---

    public class UpdateHotelRequest
    {
        [Required]
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CityArea { get; set; }
        public string? Governorate { get; set; }
        public string? Country { get; set; }
        public int? StarRating { get; set; }
        
        public PropertyType? PropertyType { get; set; }
        public AccommodationType? AccommodationType { get; set; }
        public string? AddressDetails { get; set; }
        public string? AddressProofUrl { get; set; }

        public List<HotelContactInputDto>? Contacts { get; set; }
        public List<HotelDocumentInputDto>? Documents { get; set; }
        public List<HotelFieldValueInputDto>? DynamicFields { get; set; }
        public UpdateHotelPolicyInputDto? Policy { get; set; }
        public List<long>? AmenityIds { get; set; }
    }

    public class UpdateAmenitiesRequest
    {
        public bool FreeWifi { get; set; }
        public bool SwimmingPool { get; set; }
        public bool Parking { get; set; }
        public bool AirConditioning { get; set; }
        public bool Breakfast { get; set; }
        public bool Gym { get; set; }
        public bool Restaurant { get; set; }
        public bool Spa { get; set; }
        public bool RoomService { get; set; }
    }

    public class HotelContactInputDto
    {
        [Required]
        public HotelContactType ContactType { get; set; }
        [Required]
        public string ContactValue { get; set; } = string.Empty;
    }

    public class HotelDocumentInputDto
    {
        [Required]
        public long DocumentTypeId { get; set; }
        public string? FileUrl { get; set; }
        public IFormFile? File { get; set; }
        public string? Notes { get; set; }
    }

    public class HotelFieldValueInputDto
    {
        [Required]
        public long FieldDefinitionId { get; set; }
        public string? Value { get; set; }
    }

    public class UpdateHotelPolicyInputDto
    {
        public decimal? ServiceChargePct { get; set; }
        public bool? IncludeServiceCharge { get; set; }
        public bool? IncludeVat { get; set; }
        public bool? IncludeCityTax { get; set; }
        public CancellationStrategy? CancellationStrategy { get; set; }
        public List<UpdateHotelCancellationRuleInputDto>? CancellationRules { get; set; }
    }

    public class UpdateHotelCancellationRuleInputDto
    {
        public int? FromHoursBeforeCheckIn { get; set; }
        public int? ToHoursBeforeCheckIn { get; set; }
        [Range(0, 100)]
        public decimal PenaltyPct { get; set; }
    }

    public class HotelProfileUpdateRequest
    {
        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public PropertyType? PropertyType { get; set; }
        public AccommodationType? AccommodationType { get; set; }
        public int? StarRating { get; set; }
        public string? Country { get; set; }
        public string? Governorate { get; set; }
        public string? CityArea { get; set; }
        public string? AddressDetails { get; set; }
        public List<long>? AmenityIds { get; set; }
        public List<HotelFieldValueInputDto>? DynamicFields { get; set; }
        public List<HotelImageInputDto>? Images { get; set; }
        public List<CreateRoomRequest>? Rooms { get; set; }
        public List<HotelContactInputDto>? Contacts { get; set; }
    }

    public class HotelApplicationSummaryDto
    {
        public long Id { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? CityArea { get; set; }
        public DateTime CreatedAt { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
        public bool Verified { get; set; }
    }

    public class AdminDashboardKpiDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal? PlatformCommission { get; set; }
        public bool PlatformCommissionSupported { get; set; } = false;
        public decimal RevenueThisMonth { get; set; }
        public decimal? CommissionThisMonth { get; set; }
        public bool CommissionThisMonthSupported { get; set; } = false;
        public int TotalBookings { get; set; }
        public int TotalHotels { get; set; }
        public List<TopHotelDto> TopHotels { get; set; } = new();
        public List<TopCityDto> TopCities { get; set; } = new();
    }

    public class TopHotelDto
    {
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public int BookingsCount { get; set; }
    }

    public class TopCityDto
    {
        public string CityName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
    }

    public class HotelPolicyUpdateRequest
    {
        public decimal? ServiceChargePct { get; set; }
        public bool? IncludeServiceCharge { get; set; }
        public bool? IncludeVat { get; set; }
        public bool? IncludeCityTax { get; set; }
        public CancellationStrategy? CancellationStrategy { get; set; }
        public List<UpdateHotelCancellationRuleInputDto>? CancellationRules { get; set; }
    }

    public class HotelLegalUpdateRequest
    {
        public List<HotelDocumentInputDto>? Documents { get; set; }
        public List<HotelContactInputDto>? Contacts { get; set; }
    }

    public class AdminChartDataDto
    {
        public List<YearlyGrowthDto> PlatformGrowth { get; set; } = new();
        public List<MonthlyTrendDto> BookingsTrend { get; set; } = new();
        public List<DistributionRangeDto> BookingValueDistribution { get; set; } = new();
    }

    public class YearlyGrowthDto
    {
        public int Year { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    public class MonthlyTrendDto
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int BookingsCount { get; set; }
    }

    public class DistributionRangeDto
    {
        public string RangeLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
