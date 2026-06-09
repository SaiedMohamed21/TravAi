using System.Collections.Generic;

namespace TravAi.DTOs.Hotel
{
    public class HotelDashboardOverviewDto
    {
        public string HotelName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public double RevenueChangePercent { get; set; }
        public int OccupiedToday { get; set; }
        public double OccupancyRateToday { get; set; }
        public int AvailableToday { get; set; }
        public int TotalUnits { get; set; }
        public int RoomTypesCount { get; set; }
        public int TotalGuests { get; set; }
        public string AccessMode { get; set; } = "owner"; // "owner" or "admin_readonly"

        public List<BookingDto> RecentBookings { get; set; } = new();
        public List<RoomTypeSummaryDto> RoomTypesSummary { get; set; } = new();
        public List<HotelReviewDto> LatestReviews { get; set; } = new();
    }

    public class RoomTypeSummaryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BedType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Occupied { get; set; }
        public int Available { get; set; }
        public int Occupancy { get; set; }

        public decimal? ROPrice { get; set; }
        public decimal? BBPrice { get; set; }
        public decimal? HBPrice { get; set; }
        public decimal? FBPrice { get; set; }
        public decimal? AIPrice { get; set; }
        public string State { get; set; } = "Available";
    }
}
