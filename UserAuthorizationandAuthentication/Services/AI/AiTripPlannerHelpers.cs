using System.IO;
using TravAi.DTOs.AI;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.Services.AI
{
    public static class AiTripPlannerHelpers
    {
        // Budget type → Flight class string
        public static string GetFlightClass(string budgetType) => budgetType switch
        {
            "Premium" => "Premium economy",
            "Luxury"  => "Business",
            _         => "Economy"
        };

        // Budget type → Hotel star rating range (min, max)
        public static (int Min, int Max) GetStarRange(string budgetType) => budgetType switch
        {
            "Premium" => (3, 4),
            "Luxury"  => (4, 5),
            _         => (1, 3)
        };

        // Budget type → Tour price range per person (min, max); -1 = no upper limit
        public static (decimal Min, decimal Max) GetTourPriceRange(string budgetType) => budgetType switch
        {
            "Premium" => (50m,  150m),
            "Luxury"  => (150m, -1m),
            _         => (0m,   50m)
        };

        // Build itinerary: if null → single city for full duration
        public static List<ItineraryCityDto> BuildItinerary(TripEstimateRequestDto req)
        {
            if (req.Itinerary != null && req.Itinerary.Any())
                return req.Itinerary;

            int days = (req.ReturnDate.Date - req.DepartureDate.Date).Days;
            return new List<ItineraryCityDto> { new() { City = req.ToCity, Days = Math.Max(days, 1) } };
        }

        // Get the check-in date for a specific city in the itinerary
        public static DateTime GetCityCheckIn(DateTime departureDate, List<ItineraryCityDto> itinerary, string city)
        {
            var date = departureDate.Date;
            foreach (var c in itinerary)
            {
                if (c.City.Equals(city, StringComparison.OrdinalIgnoreCase))
                    return date;
                date = date.AddDays(c.Days);
            }
            return date;
        }

        // Median of a list
        public static decimal Median(List<decimal> values)
        {
            if (!values.Any()) return 0;
            var s = values.OrderBy(v => v).ToList();
            int m = s.Count / 2;
            return s.Count % 2 == 0 ? (s[m - 1] + s[m]) / 2m : s[m];
        }

        // Parse tourist language string to Language enum
        public static Language? ParseLanguage(string lang)
        {
            return Enum.TryParse<Language>(lang, true, out var result) ? result : null;
        }

        // Hotel city match: Governorate or CityArea
        public static bool MatchesCity(string? governorate, string? cityArea, string city)
        {
            return (governorate != null && governorate.Equals(city, StringComparison.OrdinalIgnoreCase))
                || (cityArea    != null && cityArea.Equals(city, StringComparison.OrdinalIgnoreCase));
        }

        // Compute total flight price for all travelers
        public static decimal FlightTotalPrice(decimal pricePerAdult, int adults, int children)
            => pricePerAdult * adults + pricePerAdult * 0.75m * children;

        // Dual-Logging: Console.WriteLine + local workspace file
        public static void LogDebug(string msg)
        {
            try
            {
                Console.WriteLine(msg);
                string logFilePath = @"C:\Users\saied mohamed\Desktop\hotel\TravAi\budget_divider_debug.log";
                File.AppendAllText(logFilePath, msg + Environment.NewLine);
            }
            catch {}
        }
    }
}
