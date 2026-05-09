using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravAi.Models.Enums;
using TravAi.Models.Hotels.Bookings;
using TravAi.Models.Auth;


namespace TravAi.Models.Hotels
{
    public class Hotel
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        // Core Hotel Info
        public string HotelName { get; set; }
        public string? Description { get; set; }
        public bool Verified { get; set; } = false;
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime? VerificationDate { get; set; }
        public bool Active { get; set; } = true;

        // Static location/classification
        public string? Country { get; set; } = "Egypt";
        public string? Governorate { get; set; }
        public string? CityArea { get; set; }
        public string? AddressDetails { get; set; }
        public int? StarRating { get; set; }
        public PropertyType? PropertyType { get; set; }
        public AccommodationType? AccommodationType { get; set; }
        public string? AddressProofUrl { get; set; }
        public decimal PriceUsd { get; set; }
        public string? TypeNorm { get; set; }

        // Review cache
        public int NumReviews { get; set; } = 0;
        public decimal AvgReviewScore { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        public HotelPolicy? Policy { get; set; }
        public ICollection<HotelContact> Contacts { get; set; } = new List<HotelContact>();
        public ICollection<HotelDocument> Documents { get; set; } = new List<HotelDocument>();
        public ICollection<HotelFieldValue> FieldValues { get; set; } = new List<HotelFieldValue>();
        public ICollection<HotelRoom> Rooms { get; set; } = new List<HotelRoom>();
        public ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();
        public ICollection<HotelReview> Reviews { get; set; } = new List<HotelReview>();
        public ICollection<HotelBooking> Bookings { get; set; } = new List<HotelBooking>();
        public ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();

        // Legacy compatibility (not persisted)
        [NotMapped] public int ReviewCount { get => NumReviews; set => NumReviews = value; }
        [NotMapped] public decimal AvgRating { get => AvgReviewScore; set => AvgReviewScore = value; }
    }

    public class HotelContact
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public HotelContactType ContactType { get; set; }
        public string ContactValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DocumentTypeDefinition
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string KeyName { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HotelDocument> HotelDocuments { get; set; } = new List<HotelDocument>();
    }

    public class HotelDocument
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long DocumentTypeId { get; set; }
        [ForeignKey("DocumentTypeId")]
        public DocumentTypeDefinition DocumentType { get; set; }

        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class HotelFieldDefinition
    {
        [Key]
        public long Id { get; set; }
        public string KeyName { get; set; }
        public string DisplayName { get; set; }
        public HotelFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HotelFieldValue> Values { get; set; } = new List<HotelFieldValue>();
    }

    public class HotelFieldValue
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long FieldDefinitionId { get; set; }
        [ForeignKey("FieldDefinitionId")]
        public HotelFieldDefinition FieldDefinition { get; set; }

        public string? Value { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Amenity
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Category { get; set; }
        public bool IsHighlighted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();
    }

    public class HotelAmenity
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long AmenityId { get; set; }
        [ForeignKey("AmenityId")]
        public Amenity Amenity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Legacy compatibility (not persisted)
        [NotMapped] public bool FreeWifi { get; set; }
        [NotMapped] public bool SwimmingPool { get; set; }
        [NotMapped] public bool Parking { get; set; }
        [NotMapped] public bool AirConditioning { get; set; }
        [NotMapped] public bool Breakfast { get; set; }
        [NotMapped] public bool Gym { get; set; }
        [NotMapped] public bool Restaurant { get; set; }
        [NotMapped] public bool Spa { get; set; }
        [NotMapped] public bool RoomService { get; set; }
    }

    public class HotelRoom
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public HotelRoomName Name { get; set; } = HotelRoomName.StandardRoom;
        public int? Occupancy { get; set; }
        public BedType? BedType { get; set; } = Enums.BedType.Single;
        public decimal? ROPrice { get; set; }
        public decimal? BBPrice { get; set; }
        public decimal? HBPrice { get; set; }
        public decimal? FBPrice { get; set; }
        public decimal? AIPrice { get; set; }
        public int Quantity { get; set; } = 0;
        public RoomState State { get; set; } = RoomState.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string? RoomName { get => Name.ToString(); set { if (Enum.TryParse<HotelRoomName>(value, out var res)) Name = res; } }
        [NotMapped]
        public decimal? Price { get => ROPrice; set => ROPrice = value; }
        [NotMapped]
        public int Availability { get => Quantity; set => Quantity = value; }
        [NotMapped]
        public string? RoomCode { get; set; }
    }

    public class HotelImage
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public string ImageUrl { get; set; }
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class HotelReview
    {
        [Key]
        public long Id { get; set; }

        public long HotelId { get; set; }
        [ForeignKey("HotelId")]
        public Hotel Hotel { get; set; }

        public long UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class HotelCancellationRule
    {
        [Key]
        public long Id { get; set; }

        public long HotelPolicyId { get; set; }
        [ForeignKey("HotelPolicyId")]
        public HotelPolicy HotelPolicy { get; set; }

        public int? FromHoursBeforeCheckIn { get; set; }
        public int? ToHoursBeforeCheckIn { get; set; }
        public decimal PenaltyPct { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
