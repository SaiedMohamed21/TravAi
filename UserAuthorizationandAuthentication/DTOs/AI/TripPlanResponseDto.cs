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
    }
}
