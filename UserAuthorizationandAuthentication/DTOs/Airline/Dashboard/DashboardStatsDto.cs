namespace TravAi.Airline.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalFlights { get; set; }
        public int ActiveFlights { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public List<RecentBookingDto> RecentBookings { get; set; } = new();
        public List<RevenueByMonthDto> RevenueChart { get; set; } = new();
    }

    public class RecentBookingDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Route { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
    }

    public class RevenueByMonthDto
    {
        public string Month { get; set; } = null!;
        public decimal Revenue { get; set; }
    }
}


