namespace TravAi.DTOs.AI
{
    public class PlannedTourDto
    {
        public long     Id               { get; set; }
        public string   TourTitle        { get; set; } = null!;
        public string   City             { get; set; } = null!;
        public string   GuideName        { get; set; } = null!;
        public string?  TourType         { get; set; }
        public string?  TourDescription  { get; set; }
        public decimal? Rating           { get; set; }
        public int?     NumberOfReviews  { get; set; }
        public int?     DurationHours    { get; set; }
        public string?  SitesCovered     { get; set; }
        public bool     TransportIncluded{ get; set; }
        public bool     MealsIncluded    { get; set; }
        public string?  ImageUrl         { get; set; }
        public DateTime? AvailableDate   { get; set; }

        /// <summary>Price per person</summary>
        public decimal PricePerPerson { get; set; }
        /// <summary>Total price for all participants (adults + children)</summary>
        public decimal TotalPrice     { get; set; }
    }
}
