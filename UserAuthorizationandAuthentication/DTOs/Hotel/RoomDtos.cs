using System.ComponentModel.DataAnnotations;
using TravAi.Models.Enums;

namespace TravAi.DTOs.Hotel
{
    // --- Room DTOs ---
    
    public class CreateRoomRequest
    {
        public string? RoomCode { get; set; }
        [Required]
        public HotelRoomName Name { get; set; } = HotelRoomName.StandardRoom;
        public int? Occupancy { get; set; }
        [Required]
        public BedType BedType { get; set; } = BedType.Single;
        
        [Range(0, double.MaxValue)]
        public decimal? ROPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? BBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? HBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? FBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? AIPrice { get; set; }
        public int? Quantity { get; set; }
    }

    public class UpdateRoomRequest
    {
        public string? RoomCode { get; set; }
        public HotelRoomName? Name { get; set; }
        public int? Occupancy { get; set; }
        public BedType? BedType { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? ROPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? BBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? HBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? FBPrice { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? AIPrice { get; set; }
        public int? Quantity { get; set; }
    }

    public class RoomDto
    {
        public long Id { get; set; }
        public long HotelId { get; set; }
        public string? RoomCode { get; set; }
        public HotelRoomName Name { get; set; }
        public int? Occupancy { get; set; }
        public BedType? BedType { get; set; }
        public decimal? ROPrice { get; set; }
        public decimal? BBPrice { get; set; }
        public decimal? HBPrice { get; set; }
        public decimal? FBPrice { get; set; }
        public decimal? AIPrice { get; set; }
        public int Quantity { get; set; }

        public decimal Price { get; set; }
        public int Availability { get; set; }
        public int InventoryCount { get; set; }
    }
}
