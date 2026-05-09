namespace TravAi.DTOs.AI
{
    public class CityPlanDto
    {
        public string   City     { get; set; } = null!;
        public int      Days     { get; set; }
        public DateTime CheckIn  { get; set; }
        public DateTime CheckOut { get; set; }

        /// <summary>Allocated hotel budget for this city</summary>
        public decimal CityHotelBudget { get; set; }
        /// <summary>Allocated tours budget for this city</summary>
        public decimal CityToursBudget { get; set; }

        /// <summary>Best hotel selected within city hotel budget (null if excluded)</summary>
        public PlannedHotelDto? Hotel { get; set; }
        /// <summary>Best tour selected within city tours budget (null if excluded)</summary>
        public PlannedTourDto? Tour { get; set; }
    }
}
