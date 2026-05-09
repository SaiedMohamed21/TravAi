namespace TravAi.DTOs.AI
{
    /// <summary>
    /// Extends the estimate request with the user's chosen budget type and max budget.
    /// Sent after the user sees the budget ranges and selects their preferred option.
    /// </summary>
    public class TripPlanRequestDto : TripEstimateRequestDto
    {
        /// <summary>Economy | Premium | Luxury</summary>
        public string BudgetType { get; set; } = "Economy";

        /// <summary>Maximum budget the user is willing to spend (set via slider)</summary>
        public decimal MaxBudget { get; set; }
    }
}
