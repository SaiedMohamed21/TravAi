using System.Collections.Generic;

namespace TravAi.DTOs.Hotel
{
    public class HotelFinancialsDto
    {
        public string HighestEarningMonth { get; set; } = string.Empty;
        public decimal HighestEarningMonthRevenue { get; set; }
        public decimal TotalCollectedRevenue { get; set; }
        public decimal YtdGrowthPercentage { get; set; }
        public List<ChartDataPoint> YearlyRevenueOverview { get; set; } = new();
        public List<MonthlyBreakdownRow> MonthlyBreakdown { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class MonthlyBreakdownRow
    {
        public string Month { get; set; } = string.Empty;
        public decimal GrossRevenue { get; set; }
        public double OccupancyPercentage { get; set; }
    }
}
