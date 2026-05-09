namespace TravAi.DTOs.AI
{
    public class BudgetTypeRangeDto
    {
        /// <summary>Economy | Premium | Luxury</summary>
        public string Type { get; set; } = null!;

        /// <summary>Minimum estimated cost calculated from actual DB data</summary>
        public decimal MinEstimate { get; set; }

        /// <summary>Maximum estimated cost calculated from actual DB data</summary>
        public decimal MaxEstimate { get; set; }

        /// <summary>True if real data exists for this type on the requested route/dates</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Breakdown for transparency</summary>
        public decimal FlightMinEstimate { get; set; }
        public decimal FlightMaxEstimate { get; set; }
        public decimal HotelMinEstimate  { get; set; }
        public decimal HotelMaxEstimate  { get; set; }
        public decimal ToursMinEstimate  { get; set; }
        public decimal ToursMaxEstimate  { get; set; }
    }
}
