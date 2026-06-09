namespace TravAi.DTOs.AI
{
    public class TripPlanResponseDto
    {
        // --- Summary ---
        public string  BudgetType          { get; set; } = null!;
        public decimal MaxBudget           { get; set; }
        public decimal EstimatedTotalCost  { get; set; }
        public int     TotalDays           { get; set; }
        public int     Adults              { get; set; }
        public int     Children            { get; set; }

        // --- Budget Allocation (from budget_divider) ---
        public decimal FlightBudget { get; set; }
        public decimal HotelBudget  { get; set; }
        public decimal ToursBudget  { get; set; }

        // --- Flights ---
        /// <summary>Best go flight (null if excluded or not found)</summary>
        public PlannedFlightDto? GoFlight     { get; set; }
        /// <summary>Best return flight (null if excluded or not found)</summary>
        public PlannedFlightDto? ReturnFlight { get; set; }

        // --- Per-city Plan ---
        public List<CityPlanDto> CityPlans { get; set; } = new();

        // --- Itinerary used ---
        public List<ItineraryCityDto> Itinerary { get; set; } = new();

        // --- Debug Info ---
        public PlanDebugDto? DebugData { get; set; }
    }

    public class PlanDebugDto
    {
        public decimal MedianGo { get; set; }
        public decimal MedianReturn { get; set; }
        public int NumGo { get; set; }
        public int NumReturn { get; set; }
        public decimal MedianHotelsSingle { get; set; }
        public decimal MedianHotelsDouble { get; set; }
        public int NumHotelsSingle { get; set; }
        public int NumHotelsDouble { get; set; }
        public int NumberHotels { get; set; }
    }
}
