namespace TravAi.DTOs.Hotel
{
    // Search Request
    public class RoomSearchRequest
    {
        public long? HotelId { get; set; } // Filter by specific hotel
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinOccupancy { get; set; }
        public string? BedType { get; set; }
        public bool? OnlyAvailable { get; set; } // Filter only Available rooms
        
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Search Response
    public class RoomSearchResponse
    {
        public List<RoomCardDto> Rooms { get; set; } = new List<RoomCardDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // Room Card (for search results)
    public class RoomCardDto
    {
        public long Id { get; set; }
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? RoomCode { get; set; }
        public string? RoomName { get; set; }
        public int? Occupancy { get; set; }
        public string? BedType { get; set; }
        public decimal? Price { get; set; }
        public string State { get; set; } = "Available"; // Available or Booked
    }

    // Room Details (for single room view)
    public class RoomDetailsDto
    {
        public long Id { get; set; }
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? HotelLocation { get; set; }
        public int? HotelStarRating { get; set; }
        public string? RoomCode { get; set; }
        public string? RoomName { get; set; }
        public int? Occupancy { get; set; }
        public string? BedType { get; set; }
        public decimal? Price { get; set; }
        public int Availability { get; set; }
        public string State { get; set; } = "Available";
    }
}
