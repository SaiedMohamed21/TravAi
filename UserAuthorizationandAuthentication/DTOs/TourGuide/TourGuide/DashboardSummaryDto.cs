namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class DashboardSummaryDto
    {
        public int TotalTours { get; set; }
        public int UpcomingTours { get; set; }
        public int TotalBookings { get; set; }
        public decimal Earnings { get; set; }
    }

    public class EarningsChartDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
