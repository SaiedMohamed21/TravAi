using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.DTOs.Hotel
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
}
