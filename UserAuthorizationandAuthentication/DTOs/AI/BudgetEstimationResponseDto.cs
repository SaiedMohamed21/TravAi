namespace TravAi.DTOs.AI
{
    public class BudgetEstimationResponseDto
    {
        public BudgetTypeRangeDto Economy { get; set; } = null!;
        public BudgetTypeRangeDto Premium { get; set; } = null!;
        public BudgetTypeRangeDto Luxury  { get; set; } = null!;

        public int TotalDays { get; set; }

        /// <summary>The itinerary used (after defaulting to single city if needed)</summary>
        public List<ItineraryCityDto> Itinerary { get; set; } = new();
    }
}
