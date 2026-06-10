namespace TravAi.TourGuide.DTOs.TourGuide
{
    public class TourGuideEarningsSummaryDto
    {
        public decimal TotalEarnings { get; set; }
        public decimal CurrentMonthEarnings { get; set; }
        public decimal PendingWithdrawals { get; set; }
        public int CompletedBookings { get; set; }
        public decimal AverageBookingValue { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
