using TravAi.Models.Enums;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;

namespace TravAi.DTOs.Hotel
{
    // --- Search & Filter DTOs ---
    
    public class HotelSearchRequest
    {
        // Location
        public string? Destination { get; set; } // City or Area
        
        // Dates
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        
        // Guests
        public int? Persons { get; set; }
        public int? Rooms { get; set; }
        public string? BedType { get; set; }
        
        // Filters
        public int? MinStarRating { get; set; }
        public int? MaxStarRating { get; set; }
        public decimal? MinReviewRating { get; set; } // For User Reviews (0-5)
        public decimal? AvgRating { get; set; } // For User Reviews (0-5)
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        
        public List<string>? Amenities { get; set; }
        
        // Sorting
        public string? SortBy { get; set; } // "popular", "recent", "price_low", "price_high", "rating"
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class HotelSearchResponse
    {
        public List<HotelCardDto> Hotels { get; set; } = new List<HotelCardDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class HotelCardDto
    {
        public long Id { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? Location { get; set; } // "Downtown, Cairo"
        public int? StarRating { get; set; }
        public decimal AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal? PricePerNight { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public List<string> AmenityTags { get; set; } = new List<string>(); // ["City View", "Free Wi-Fi"]
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public string? VerificationStatus { get; set; }
    }

    public class HotelDetailsDto
    {
        public long Id { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? CityArea { get; set; }
        public string? Governorate { get; set; }
        public int? StarRating { get; set; }
        public decimal AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public string? Description { get; set; }
        public string? DistanceFromLandmark { get; set; } // "2.5 km from Egyptian Museum"
        public decimal PriceUsd { get; set; }
        public string? TypeNorm { get; set; }

        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        
        // Images
        public List<HotelImageDto> Images { get; set; } = new List<HotelImageDto>();
        
        // Amenities
        public HotelAmenitiesDto Amenities { get; set; } = new HotelAmenitiesDto();
        public List<string> AmenityNames { get; set; } = new List<string>();
        public List<AmenityDto> AmenityDetails { get; set; } = new List<AmenityDto>();
        
        // Rooms
        public List<RoomDto> Rooms { get; set; } = new List<RoomDto>();

        // New separated structures
        public List<HotelContactDto> Contacts { get; set; } = new();
        public List<HotelDocumentDto> Documents { get; set; } = new();
        public List<HotelDynamicFieldValueDto> DynamicFields { get; set; } = new();
        public HotelPolicyDto? Policy { get; set; }

        public bool Verified { get; set; }
        public string? VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }
        public long UserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PropertyType { get; set; }
        public string? AccommodationType { get; set; }
        public string? AddressDetails { get; set; }
        public string? Country { get; set; }
        public string? AddressProofUrl { get; set; }
    }

    public class HotelImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class HotelAmenitiesDto
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

    public class HotelContactDto
    {
        public long Id { get; set; }
        public string ContactType { get; set; } = string.Empty;
        public string ContactValue { get; set; } = string.Empty;
    }

    public class HotelDocumentDto
    {
        public long Id { get; set; }
        public long DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string DocumentTypeKeyName { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class HotelDynamicFieldValueDto
    {
        public long Id { get; set; }
        public long FieldDefinitionId { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? Value { get; set; }
    }

    public class HotelPolicyDto
    {
        public long Id { get; set; }
        public decimal ServiceChargePct { get; set; }
        public bool IncludeServiceCharge { get; set; }
        public bool IncludeVat { get; set; }
        public bool IncludeCityTax { get; set; }
        public string CancellationStrategy { get; set; } = string.Empty;
        public List<HotelCancellationRuleDto> CancellationRules { get; set; } = new();
    }

    public class HotelCancellationRuleDto
    {
        public long Id { get; set; }
        public int? FromHoursBeforeCheckIn { get; set; }
        public int? ToHoursBeforeCheckIn { get; set; }
        public decimal PenaltyPct { get; set; }
    }
}
