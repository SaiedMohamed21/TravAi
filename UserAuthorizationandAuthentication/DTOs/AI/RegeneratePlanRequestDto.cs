using System;
using System.Collections.Generic;

namespace TravAi.DTOs.AI
{
    public class RegeneratePlanRequestDto : TripPlanRequestDto
    {
        public string? GoFlightSessionId { get; set; }
        public string? ReturnFlightSessionId { get; set; }
        public string? TourSessionId { get; set; }

        public bool IsGoFlightFixed { get; set; }
        public long? FixedGoFlightId { get; set; }

        public bool IsReturnFlightFixed { get; set; }
        public long? FixedReturnFlightId { get; set; }

        public List<string> FixedTourDates { get; set; } = new List<string>();

        public int HotelRegenerateIndex { get; set; } = 0;
        public bool IsHotelFixed { get; set; } = false;
        public long? FixedHotelId { get; set; }
    }
}
