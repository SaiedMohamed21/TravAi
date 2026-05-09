using TravAi.DTOs.AI;

namespace TravAi.Services.AI
{
    public interface IAiTripPlannerService
    {
        /// <summary>
        /// Step 2: Calculate budget ranges (Economy/Premium/Luxury) based on actual DB data
        /// filtered by the user's route, dates, and cities.
        /// </summary>
        Task<BudgetEstimationResponseDto> EstimateBudgetAsync(TripEstimateRequestDto request);

        /// <summary>
        /// Step 3: Generate a complete trip plan by:
        /// 1. Filtering DB data by budget class and constraints
        /// 2. Running the budget_divider algorithm to split the budget
        /// 3. Selecting the best flight/hotel/tour per category using scoring
        /// </summary>
        Task<TripPlanResponseDto> GeneratePlanAsync(TripPlanRequestDto request);
    }
}
