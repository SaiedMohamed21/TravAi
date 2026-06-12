using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.DTOs.AI;
using TravAi.Models.Enums;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using static TravAi.Services.AI.AiTripPlannerHelpers;

namespace TravAi.Services.AI
{
    public class AiTripPlannerService : IAiTripPlannerService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AiTripPlannerService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STEP 1 ─ Estimate budget ranges from actual DB data
        // ─────────────────────────────────────────────────────────────────────
        public async Task<BudgetEstimationResponseDto> EstimateBudgetAsync(TripEstimateRequestDto req)
        {
            var itinerary = BuildItinerary(req);
            var totalDays = itinerary.Sum(c => c.Days);

            var economy = await CalcRange("Economy", req, itinerary);
            var premium = await CalcRange("Premium", req, itinerary);
            var luxury  = await CalcRange("Luxury",  req, itinerary);

            // Fix overlapping ranges to create continuous, logical tiers
            // Economy: [Economy.Min, Premium.Min - 1]
            // Premium: [Premium.Min, Luxury.Min - 1]
            // Luxury:  [Luxury.Min, Absolute_Max]

            decimal absoluteMax = Math.Max(economy.MaxEstimate, Math.Max(premium.MaxEstimate, luxury.MaxEstimate));

            // Ensure Premium Min is strictly greater than Economy Min
            decimal premiumMin = Math.Max(premium.MinEstimate, economy.MinEstimate + 100);
            // Ensure Luxury Min is strictly greater than Premium Min
            decimal luxuryMin = Math.Max(luxury.MinEstimate, premiumMin + 100);

            // Adjust Economy Max
            if (economy.IsAvailable && premium.IsAvailable)
            {
                economy.MaxEstimate = premiumMin - 1;
            }
            else if (economy.IsAvailable && !premium.IsAvailable && luxury.IsAvailable)
            {
                economy.MaxEstimate = luxuryMin - 1;
            }
            else if (economy.IsAvailable)
            {
                economy.MaxEstimate = absoluteMax;
            }
            ScaleBreakdownToMatchTotal(economy);

            // Adjust Premium Max
            if (premium.IsAvailable && luxury.IsAvailable)
            {
                premium.MinEstimate = premiumMin;
                premium.MaxEstimate = luxuryMin - 1;
            }
            else if (premium.IsAvailable)
            {
                premium.MinEstimate = premiumMin;
                premium.MaxEstimate = absoluteMax;
            }
            ScaleBreakdownToMatchTotal(premium);

            // Adjust Luxury
            if (luxury.IsAvailable)
            {
                luxury.MinEstimate = luxuryMin;
                luxury.MaxEstimate = absoluteMax;
            }
            ScaleBreakdownToMatchTotal(luxury);

            return new BudgetEstimationResponseDto
            {
                Economy   = economy,
                Premium   = premium,
                Luxury    = luxury,
                TotalDays = totalDays,
                Itinerary = itinerary
            };
        }

        private void ScaleBreakdownToMatchTotal(BudgetTypeRangeDto budget)
        {
            if (!budget.IsAvailable) return;

            // Scale Max
            decimal oldMaxSum = budget.FlightMaxEstimate + budget.HotelMaxEstimate + budget.ToursMaxEstimate;
            if (oldMaxSum > 0 && oldMaxSum != budget.MaxEstimate)
            {
                decimal ratio = budget.MaxEstimate / oldMaxSum;
                budget.FlightMaxEstimate = Math.Round(budget.FlightMaxEstimate * ratio, 2);
                budget.HotelMaxEstimate  = Math.Round(budget.HotelMaxEstimate * ratio, 2);
                budget.ToursMaxEstimate  = Math.Round(budget.ToursMaxEstimate * ratio, 2);
            }

            // Scale Min
            decimal oldMinSum = budget.FlightMinEstimate + budget.HotelMinEstimate + budget.ToursMinEstimate;
            if (oldMinSum > 0 && oldMinSum != budget.MinEstimate)
            {
                decimal ratio = budget.MinEstimate / oldMinSum;
                budget.FlightMinEstimate = Math.Round(budget.FlightMinEstimate * ratio, 2);
                budget.HotelMinEstimate  = Math.Round(budget.HotelMinEstimate * ratio, 2);
                budget.ToursMinEstimate  = Math.Round(budget.ToursMinEstimate * ratio, 2);
            }
        }

        private async Task<BudgetTypeRangeDto> CalcRange(
            string budgetType, TripEstimateRequestDto req, List<ItineraryCityDto> itinerary)
        {
            var flightClass = GetFlightClass(budgetType);
            var firstCity   = itinerary.First().City;
            var lastCity    = itinerary.Last().City;
            var (starMin, starMax) = GetStarRange(budgetType);
            var lang = ParseLanguage(req.TouristLanguage);
            string targetCluster = budgetType.ToLower() switch
            {
                "economy" => "economic",
                "premium" => "premium",
                "luxury"  => "business",
                _         => "economic"
            };
            string targetTourType = budgetType.ToLower() switch
            {
                "economy" => "economy",
                "premium" => "premium",
                "luxury"  => "luxury",
                _         => "economy"
            };

            // ── Tier Availability Validation ─────────────────────────────────
            // --- 1. Flights Validation ---
            if (!req.ExcludeFlights)
            {
                var fromCodes = await _db.Airports.Where(a => a.City.ToLower() == req.FromCity.ToLower()).Select(a => a.Code).ToListAsync();
                var toCodes   = await _db.Airports.Where(a => a.City.ToLower() == firstCity.ToLower()).Select(a => a.Code).ToListAsync();
                var lastCodes = await _db.Airports.Where(a => a.City.ToLower() == lastCity.ToLower()).Select(a => a.Code).ToListAsync();

                int totalTravelers = req.Adults + req.Children;
                var goDate = req.DepartureDate.Date;
                var retDate = req.ReturnDate.Date;

                var outboundExist = await _db.Flights
                    .AnyAsync(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == goDate
                        && fromCodes.Contains(f.DepartureAirportCode!)
                        && toCodes.Contains(f.ArrivalAirportCode!)
                        && f.AvailableSeats >= totalTravelers);

                if (!outboundExist)
                {
                    return new BudgetTypeRangeDto
                    {
                        Type = budgetType,
                        IsAvailable = false,
                        Available = false,
                        Reason = "No matching outbound flights with sufficient seats"
                    };
                }

                var returnExist = await _db.Flights
                    .AnyAsync(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == retDate
                        && lastCodes.Contains(f.DepartureAirportCode!)
                        && fromCodes.Contains(f.ArrivalAirportCode!)
                        && f.AvailableSeats >= totalTravelers);

                if (!returnExist)
                {
                    return new BudgetTypeRangeDto
                    {
                        Type = budgetType,
                        IsAvailable = false,
                        Available = false,
                        Reason = "No matching return flights with sufficient seats"
                    };
                }
            }

            // --- 2. Hotels Validation ---
            if (!req.ExcludeHotels)
            {
                foreach (var city in itinerary)
                {
                    var cityLower = city.City.ToLower();
                    var hasValidHotel = await _db.Hotels
                        .Include(h => h.Rooms)
                        .AnyAsync(h => h.Verified && h.Active
                            && h.StarRating >= starMin && h.StarRating <= starMax
                            && h.ClusterSegment != null && h.ClusterSegment.ToLower() == targetCluster
                            && (h.Governorate != null && h.Governorate.ToLower() == cityLower
                             || h.CityArea    != null && h.CityArea.ToLower()    == cityLower)
                            && (req.SingleRooms <= 0 || h.Rooms.Where(r => r.BedType == BedType.Single && r.State == RoomState.Active && r.FBPrice.HasValue).Sum(r => r.Quantity) >= req.SingleRooms)
                            && (req.DoubleRooms <= 0 || h.Rooms.Where(r => r.BedType == BedType.Double && r.State == RoomState.Active && r.FBPrice.HasValue).Sum(r => r.Quantity) >= req.DoubleRooms));

                    if (!hasValidHotel)
                    {
                        return new BudgetTypeRangeDto
                        {
                            Type = budgetType,
                            IsAvailable = false,
                            Available = false,
                            Reason = "Hotel inventory cannot satisfy requested room configuration"
                        };
                    }
                }
            }

            // --- 3. Tours Validation ---
            if (!req.ExcludeTours)
            {
                int totalTravelers = req.Adults + req.Children;
                foreach (var city in itinerary)
                {
                    var cityLower = city.City.ToLower();
                    var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                    var co = ci.AddDays(city.Days);

                    var toursInCity = await _db.Tours
                        .Where(t => t.Active && t.BasePriceUsd.HasValue
                            && t.City != null && t.City.ToLower() == cityLower
                            && t.TourType != null && t.TourType.ToLower() == targetTourType
                            && t.AvailableDateTime >= ci && t.AvailableDateTime < co)
                        .Select(t => new {
                            t.Id,
                            GroupSizeMax = t.GroupSizeMax ?? 10
                        })
                        .ToListAsync();

                    if (!toursInCity.Any())
                    {
                        return new BudgetTypeRangeDto
                        {
                            Type = budgetType,
                            IsAvailable = false,
                            Available = false,
                            Reason = "No tours with sufficient remaining capacity"
                        };
                    }

                    var tourIds = toursInCity.Select(t => t.Id).ToList();
                    var bookings = await _db.TourBookings
                        .Where(tb => tourIds.Contains(tb.TourId) && tb.Status != TravAi.TourGuide.Models.Enums.BookingStatus.Cancelled)
                        .GroupBy(tb => tb.TourId)
                        .Select(g => new {
                            TourId = g.Key,
                            BookedCount = g.Sum(tb => tb.ParticipantsCount)
                        })
                        .ToDictionaryAsync(x => x.TourId, x => x.BookedCount);

                    bool hasTourWithCapacity = false;
                    foreach (var tour in toursInCity)
                    {
                        int booked = bookings.ContainsKey(tour.Id) ? bookings[tour.Id] : 0;
                        int remaining = tour.GroupSizeMax - booked;
                        if (remaining >= totalTravelers)
                        {
                            hasTourWithCapacity = true;
                            break;
                        }
                    }

                    if (!hasTourWithCapacity)
                    {
                        return new BudgetTypeRangeDto
                        {
                            Type = budgetType,
                            IsAvailable = false,
                            Available = false,
                            Reason = "No tours with sufficient remaining capacity"
                        };
                    }
                }
            }

            decimal flightMin = 0, flightMax = 0;
            decimal hotelMin  = 0, hotelMax  = 0;
            decimal toursMin  = 0, toursMax  = 0;

            // ── Flights ──────────────────────────────────────────────────────
            if (!req.ExcludeFlights)
            {
                // Pre-fetch airport codes by city name to avoid EF navigation-property translation issues
                var fromCodes  = await _db.Airports.Where(a => a.City.ToLower() == req.FromCity.ToLower()).Select(a => a.Code).ToListAsync();
                var toCodes    = await _db.Airports.Where(a => a.City.ToLower() == firstCity.ToLower()).Select(a => a.Code).ToListAsync();
                var lastCodes  = await _db.Airports.Where(a => a.City.ToLower() == lastCity.ToLower()).Select(a => a.Code).ToListAsync();

                // Search on the exact departure date
                var goDate = req.DepartureDate.Date;
                var goPrices = await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == goDate
                        && fromCodes.Contains(f.DepartureAirportCode!)
                        && toCodes.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

                // Search on the exact return date
                var retDate = req.ReturnDate.Date;
                var retPrices = await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == retDate
                        && lastCodes.Contains(f.DepartureAirportCode!)
                        && fromCodes.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

                if (goPrices.Any())
                {
                    flightMin += FlightTotalPrice(AiTripPlannerHelpers.GetPercentile(goPrices, 0.25), req.Adults, req.Children);
                    flightMax += FlightTotalPrice(AiTripPlannerHelpers.GetPercentile(goPrices, 0.75), req.Adults, req.Children);
                }
                if (retPrices.Any())
                {
                    flightMin += FlightTotalPrice(AiTripPlannerHelpers.GetPercentile(retPrices, 0.25), req.Adults, req.Children);
                    flightMax += FlightTotalPrice(AiTripPlannerHelpers.GetPercentile(retPrices, 0.75), req.Adults, req.Children);
                }
            }

            // ── Hotels ───────────────────────────────────────────────────────
            if (!req.ExcludeHotels && (req.SingleRooms > 0 || req.DoubleRooms > 0))
            {
                foreach (var city in itinerary)
                {
                    var cityLower = city.City.ToLower();
                    var rooms = await _db.HotelRooms
                        .Include(r => r.Hotel)
                        .Where(r => r.Hotel.Verified && r.Hotel.Active
                            && r.State == RoomState.Active && r.FBPrice.HasValue
                            && (r.Hotel.Governorate != null && r.Hotel.Governorate.ToLower() == cityLower
                             || r.Hotel.CityArea    != null && r.Hotel.CityArea.ToLower()    == cityLower)
                            && r.Hotel.ClusterSegment != null && r.Hotel.ClusterSegment.ToLower() == targetCluster)
                        .ToListAsync();

                    var singles = rooms.Where(r => r.BedType == BedType.Single)
                                       .Select(r => r.FBPrice!.Value).ToList();
                    var doubles = rooms.Where(r => r.BedType == BedType.Double)
                                       .Select(r => r.FBPrice!.Value).ToList();

                    if (singles.Any() && req.SingleRooms > 0)
                    {
                        hotelMin += AiTripPlannerHelpers.GetPercentile(singles, 0.25) * req.SingleRooms * city.Days;
                        hotelMax += AiTripPlannerHelpers.GetPercentile(singles, 0.75) * req.SingleRooms * city.Days;
                    }
                    if (doubles.Any() && req.DoubleRooms > 0)
                    {
                        hotelMin += AiTripPlannerHelpers.GetPercentile(doubles, 0.25) * req.DoubleRooms * city.Days;
                        hotelMax += AiTripPlannerHelpers.GetPercentile(doubles, 0.75) * req.DoubleRooms * city.Days;
                    }
                }
            }

            // ── Tours ────────────────────────────────────────────────────────
            if (!req.ExcludeTours)
            {
                int totalPeople = req.Adults + req.Children;
                foreach (var city in itinerary)
                {
                    var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                    var co = ci.AddDays(city.Days);
                    var cityLower = city.City.ToLower();

                    var prices = await _db.Tours
                        .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                        .Where(t => t.Active && t.BasePriceUsd.HasValue
                            && t.City!.ToLower() == cityLower
                            && t.TourType != null && t.TourType.ToLower() == targetTourType
                            && t.AvailableDateTime.HasValue
                            && t.AvailableDateTime >= ci && t.AvailableDateTime < co
                            && (lang == null || t.TourGuide.TourGuideLanguages
                                .Any(l => l.Language == lang.Value)))
                        .Select(t => t.BasePriceUsd!.Value).ToListAsync();

                    if (prices.Any())
                    {
                        toursMin += AiTripPlannerHelpers.GetPercentile(prices, 0.25) * totalPeople * city.Days;
                        toursMax += AiTripPlannerHelpers.GetPercentile(prices, 0.75) * totalPeople * city.Days;
                    }
                }
            }

            decimal totalMin = flightMin + hotelMin + toursMin;
            decimal totalMax = flightMax + hotelMax + toursMax;
            bool available   = totalMin > 0 || totalMax > 0;

            return new BudgetTypeRangeDto
            {
                Type               = budgetType,
                MinEstimate        = Math.Round(totalMin, 2),
                MaxEstimate        = Math.Round(totalMax, 2),
                IsAvailable        = available,
                Available          = true,
                Reason             = null,
                FlightMinEstimate  = Math.Round(flightMin, 2),
                FlightMaxEstimate  = Math.Round(flightMax, 2),
                HotelMinEstimate   = Math.Round(hotelMin, 2),
                HotelMaxEstimate   = Math.Round(hotelMax, 2),
                ToursMinEstimate   = Math.Round(toursMin, 2),
                ToursMaxEstimate   = Math.Round(toursMax, 2),
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STEP 2 ─ Generate full trip plan (budget_divider + selection)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TripPlanResponseDto> GeneratePlanAsync(TripPlanRequestDto req)
        {
            var itinerary   = BuildItinerary(req);
            var flightClass = GetFlightClass(req.BudgetType);
            var firstCity   = itinerary.First().City;
            var lastCity    = itinerary.Last().City;
            var (starMin, starMax)   = GetStarRange(req.BudgetType);
            var lang        = ParseLanguage(req.TouristLanguage);
            string targetCluster = req.BudgetType.ToLower() switch
            {
                "economy" => "economic",
                "premium" => "premium",
                "luxury"  => "business",
                _         => "economic"
            };
            string targetTourType = req.BudgetType.ToLower() switch
            {
                "economy" => "economy",
                "premium" => "premium",
                "luxury"  => "luxury",
                _         => "economy"
            };
            int totalPeople = req.Adults + req.Children;

            // ── Fetch filtered flight prices for budget_divider ───────────────
            // Pre-fetch airport codes by city name
            var fromCodesP  = await _db.Airports.Where(a => a.City.ToLower() == req.FromCity.ToLower()).Select(a => a.Code).ToListAsync();
            var toCodesP    = await _db.Airports.Where(a => a.City.ToLower() == firstCity.ToLower()).Select(a => a.Code).ToListAsync();
            var lastCodesP  = await _db.Airports.Where(a => a.City.ToLower() == lastCity.ToLower()).Select(a => a.Code).ToListAsync();

            // Exact departure date matching
            var goDate = req.DepartureDate.Date;
            var goPriceList = req.ExcludeFlights ? new List<decimal>() :
                await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == goDate
                        && fromCodesP.Contains(f.DepartureAirportCode!)
                        && toCodesP.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

            var retDate = req.ReturnDate.Date;
            var retPriceList = req.ExcludeFlights ? new List<decimal>() :
                await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date == retDate
                        && lastCodesP.Contains(f.DepartureAirportCode!)
                        && fromCodesP.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

            // ── budget_divider ─────────────────────────────────────────────
            LogDebug("\n" + new string('=', 60));
            LogDebug(" [DEBUG: BUDGET DIVIDER START]");
            LogDebug($"   - Target Budget Type : {req.BudgetType}");
            LogDebug($"   - Input Total Budget  : {req.MaxBudget}");
            LogDebug($"   - Route               : {req.FromCity} -> {firstCity} (Last: {lastCity})");
            LogDebug($"   - Travelers           : Adults={req.Adults}, Children={req.Children}");
            LogDebug($"   - Requested Rooms     : Single={req.SingleRooms}, Double={req.DoubleRooms}");
            LogDebug(new string('-', 60));

            decimal goMedian = Median(goPriceList);
            decimal retMedian = Median(retPriceList);
            decimal avgGoFlight  = FlightTotalPrice(goMedian,  req.Adults, req.Children);
            decimal avgRetFlight = FlightTotalPrice(retMedian, req.Adults, req.Children);

            LogDebug($" [FLIGHT CALCULATIONS]");
            LogDebug($"   - Go Flights Found   : {goPriceList.Count}");
            LogDebug($"   - Go Price Median    : {goMedian}");
            LogDebug($"   - avgGoFlight Eq     : ({goMedian} * {req.Adults}) + ({goMedian} * 0.75 * {req.Children}) = {avgGoFlight}");
            LogDebug($"   - Return Flights Fnd : {retPriceList.Count}");
            LogDebug($"   - Return Price Median: {retMedian}");
            LogDebug($"   - avgRetFlight Eq    : ({retMedian} * {req.Adults}) + ({retMedian} * 0.75 * {req.Children}) = {avgRetFlight}");
            LogDebug(new string('-', 60));

            PlannedFlightDto? goFlight = null;
            PlannedFlightDto? retFlight = null;

            if (!req.ExcludeFlights)
            {
                goFlight = await GetAiRecommendedFlightAsync(fromCodesP, toCodesP, req.DepartureDate, flightClass, req.MaxBudget, req.Adults, req.Children, "Outbound");
                retFlight = await GetAiRecommendedFlightAsync(lastCodesP, fromCodesP, req.ReturnDate, flightClass, req.MaxBudget, req.Adults, req.Children, "Return");
            }

            DateTime tourStartDate = req.DepartureDate.Date.AddDays(1);
            if (!req.ExcludeFlights && goFlight != null && goFlight.ArrivalTime.HasValue)
            {
                tourStartDate = goFlight.ArrivalTime.Value.Date.AddDays(1);
            }

            DateTime tourEndDate = req.ReturnDate.Date.AddDays(-1);
            if (!req.ExcludeFlights && retFlight != null && retFlight.DepartureTime.HasValue)
            {
                tourEndDate = retFlight.DepartureTime.Value.Date.AddDays(-1);
            }

            // Adjust days array to match the new start_date and end_date
            List<int> originalDays = itinerary.Select(c => c.Days).ToList();
            List<int> adjustedTourDays = new List<int>(originalDays);

            if (adjustedTourDays.Count == 1)
            {
                adjustedTourDays[0] = Math.Max(1, (int)(tourEndDate - tourStartDate).TotalDays + 1);
            }
            else if (adjustedTourDays.Count > 1)
            {
                int startDiff = (int)(tourStartDate - req.DepartureDate.Date).TotalDays;
                int endDiff = (int)(req.ReturnDate.Date - tourEndDate).TotalDays;
                
                adjustedTourDays[0] = Math.Max(1, adjustedTourDays[0] - startDiff);
                int lastIdx = adjustedTourDays.Count - 1;
                adjustedTourDays[lastIdx] = Math.Max(1, adjustedTourDays[lastIdx] - endDiff);
            }

            var tourDates = new List<(DateTime CheckIn, DateTime CheckOut)>();
            var currentTourDate = tourStartDate;
            for (int i = 0; i < itinerary.Count; i++)
            {
                var days = adjustedTourDays[i];
                var nextTourDate = currentTourDate.AddDays(days);
                tourDates.Add((currentTourDate, nextTourDate));
                currentTourDate = nextTourDate;
            }

            // Per-city hotel & tour market averages
            var hotelSingleMedians = new List<decimal>();
            var hotelDoubleMedians = new List<decimal>();
            var tourMeans          = new List<(string city, decimal mean, int days)>();
            var allTourPrices      = new List<decimal>();
            var tourDebugPerCity   = new Dictionary<string, string>();
            var hotelDebugPerCity  = new Dictionary<string, string>();
            var hotelCosts         = new Dictionary<string, decimal>();
            var tourCosts          = new Dictionary<string, decimal>();

            // Debug storage
            decimal debugSMed = 0, debugDMed = 0;
            int debugNumSingle = 0, debugNumDouble = 0, debugTotalRooms = 0;

            int cityIdx = 0;
            foreach (var city in itinerary)
            {
                LogDebug($" [CITY SCAN: {city.City.ToUpper()} for {city.Days} Days]");
                if (!req.ExcludeHotels)
                {
                    var cityLowerP = city.City.ToLower();
                    var rooms = await _db.HotelRooms
                        .Include(r => r.Hotel)
                        .Where(r => r.Hotel.Verified && r.Hotel.Active
                            && r.State == RoomState.Active && r.FBPrice.HasValue
                            && (r.Hotel.Governorate != null && r.Hotel.Governorate.ToLower() == cityLowerP
                             || r.Hotel.CityArea    != null && r.Hotel.CityArea.ToLower()    == cityLowerP)
                            && r.Hotel.ClusterSegment != null && r.Hotel.ClusterSegment.ToLower() == targetCluster)
                        .ToListAsync();

                    var sm = rooms.Where(r => r.BedType == BedType.Single)
                                  .Select(r => r.FBPrice!.Value).ToList();
                    var dm = rooms.Where(r => r.BedType == BedType.Double)
                                  .Select(r => r.FBPrice!.Value).ToList();

                    decimal sMed = Median(sm);
                    decimal dMed = Median(dm);

                    // Track first city's (or overall) debug info
                    if (debugTotalRooms == 0) {
                        debugSMed = sMed; debugDMed = dMed;
                        debugNumSingle = sm.Count; debugNumDouble = dm.Count;
                        debugTotalRooms = rooms.Count;
                    }

                    LogDebug($"   - Hotels Selected    : Total Rooms={rooms.Count}");
                    LogDebug($"   - Single Rooms Count : {sm.Count} (Median={sMed})");
                    LogDebug($"   - Double Rooms Count : {dm.Count} (Median={dMed})");

                    decimal cityHotelCost = 0;
                    decimal sPart = 0;
                    decimal dPart = 0;
                    if (sm.Any())
                    {
                        sPart = sMed * city.Days * req.SingleRooms;
                        hotelSingleMedians.Add(sPart);
                        cityHotelCost += sPart;
                        LogDebug($"   - Single Room Part   : {sMed} * {city.Days} days * {req.SingleRooms} rooms = {sPart}");
                    }
                    if (dm.Any())
                    {
                        dPart = dMed * city.Days * req.DoubleRooms;
                        hotelDoubleMedians.Add(dPart);
                        cityHotelCost += dPart;
                        LogDebug($"   - Double Room Part   : {dMed} * {city.Days} days * {req.DoubleRooms} rooms = {dPart}");
                    }

                    hotelDebugPerCity[city.City] = $"Single Med: {sMed} * {req.SingleRooms} Rms * {city.Days} Days = {sPart} | Double Med: {dMed} * {req.DoubleRooms} Rms * {city.Days} Days = {dPart} | Total Est: {cityHotelCost} (Rooms: {rooms.Count})";
                    hotelCosts[city.City] = cityHotelCost;
                }
                else
                {
                    hotelDebugPerCity[city.City] = "Excluded";
                    hotelCosts[city.City] = 0;
                }

                if (!req.ExcludeTours)
                {
                    var origCi = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                    var origCo = origCi.AddDays(city.Days);
                    var cityLower = city.City.ToLower();
                    var tp = await _db.Tours
                        .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                        .Where(t => t.Active && t.BasePriceUsd.HasValue
                            && t.City!.ToLower() == cityLower
                            && t.TourType != null && t.TourType.ToLower() == targetTourType
                            && t.AvailableDateTime >= origCi && t.AvailableDateTime < origCo
                            && (lang == null || t.TourGuide.TourGuideLanguages.Any(l => l.Language == lang.Value)))
                        .Select(t => t.BasePriceUsd!.Value).ToListAsync();

                    decimal cityTourCost = 0;
                    decimal cityMedian = 0;
                    int cityCount = tp.Count;
                    if (tp.Any())
                    {
                        cityMedian = Median(tp);
                        tourMeans.Add((city.City, cityMedian, city.Days));
                        allTourPrices.AddRange(tp);
                        cityTourCost = cityMedian * city.Days * totalPeople;
                        LogDebug($"   - Tours Found in City: {tp.Count} (Median Price={cityMedian})");
                    }
                    tourCosts[city.City] = cityTourCost;
                    tourDebugPerCity[city.City] = $"Median: {cityMedian} * {totalPeople} Pax * {city.Days} Days = {cityTourCost} (Count: {cityCount})";
                }
                else
                {
                    tourDebugPerCity[city.City] = "Excluded";
                    tourCosts[city.City] = 0;
                }
                LogDebug(new string('-', 60));
                cityIdx++;
            }

            decimal avgHotel = hotelSingleMedians.Sum() + hotelDoubleMedians.Sum();
            decimal avgTours = tourMeans.Sum(t => t.mean * t.days * totalPeople);
            decimal marketSum = avgGoFlight + avgRetFlight + avgHotel + avgTours;

            LogDebug($" [GLOBAL MARKET SUMMARY]");
            LogDebug($"   - avgHotel (Sum)     : {avgHotel}");
            LogDebug($"   - avgTours (Sum)     : {avgTours}");
            LogDebug($"   - marketSum Eq       : {avgGoFlight} + {avgRetFlight} + {avgHotel} + {avgTours} = {marketSum}");
            LogDebug(new string('-', 60));

            // Allocate budgets proportionally using the unified DivideBudget function (budget_divider logic)
            decimal budget = req.MaxBudget;
            var initialAllocation = DivideBudget(
                budget,
                req.ExcludeFlights,
                req.ExcludeHotels,
                req.ExcludeTours,
                avgGoFlight + avgRetFlight,
                avgHotel,
                avgTours);

            decimal flightBudget = initialAllocation.flightAlloc;
            decimal hotelBudget  = initialAllocation.hotelAlloc;
            decimal toursBudget  = initialAllocation.toursAlloc;

            LogDebug($" [PROPORTIONAL ALLOCATION (budget_divider)]");
            LogDebug($"   - flightBudget       : {flightBudget}");
            LogDebug($"   - hotelBudget        : {hotelBudget}");
            LogDebug($"   - toursBudget        : {toursBudget}");
            LogDebug(" [DEBUG: BUDGET DIVIDER END]");
            LogDebug(new string('=', 60) + "\n");

            decimal actualGoPrice = goFlight?.TotalPrice ?? 0;
            decimal actualRetPrice = retFlight?.TotalPrice ?? 0;
            decimal actualFlightsCost = actualGoPrice + actualRetPrice;
            decimal remainingBudget = Math.Max(0, req.MaxBudget - actualFlightsCost);

            decimal goBudget = 0;
            decimal retBudget = 0;

            if (req.ExcludeHotels && req.ExcludeTours)
            {
                // Flight only scenario: flight budget remains MaxBudget
                flightBudget = req.MaxBudget;
                goBudget = (avgGoFlight + avgRetFlight) > 0
                    ? flightBudget * (avgGoFlight / (avgGoFlight + avgRetFlight)) : flightBudget / 2;
                retBudget = flightBudget - goBudget;
                hotelBudget = 0;
                toursBudget = 0;
            }
            else
            {
                // Dynamic reallocation of flight surplus: exclude flights, distribute remainingBudget among active non-flight categories
                var reallocation = DivideBudget(
                    remainingBudget,
                    excludeFlights: true,
                    req.ExcludeHotels,
                    req.ExcludeTours,
                    0,
                    avgHotel,
                    avgTours);

                flightBudget = actualFlightsCost;
                goBudget = actualGoPrice;
                retBudget = actualRetPrice;
                hotelBudget = reallocation.hotelAlloc;
                toursBudget = reallocation.toursAlloc;
            }

            // Per-city budgets for hotels/tours (proportional to days)
            int totalDays = itinerary.Sum(c => c.Days);
            var cityPlans = new List<CityPlanDto>();

            foreach (var city in itinerary)
            {
                decimal cityHotelBudget = 0;
                if (avgHotel > 0 && hotelCosts.ContainsKey(city.City))
                {
                    cityHotelBudget = (hotelCosts[city.City] / avgHotel) * hotelBudget;
                }
                else
                {
                    double dayRatio = totalDays > 0 ? (double)city.Days / totalDays : 0;
                    cityHotelBudget = (decimal)dayRatio * hotelBudget;
                }

                decimal cityToursBudget = 0;

                var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                var co = ci.AddDays(city.Days);

                cityPlans.Add(new CityPlanDto
                {
                    City            = city.City,
                    Days            = city.Days,
                    CheckIn         = ci,
                    CheckOut        = co,
                    CityHotelBudget = Math.Round(cityHotelBudget, 2),
                    CityToursBudget = Math.Round(cityToursBudget, 2),
                    Hotel           = null,
                    Tour            = null
                });
            }

            // --- 1. CALL HOTEL RECOMMENDATION API ---
            string hotelApiRequestJsonStr = "";
            string hotelApiResponseJsonStr = null;
            decimal actualHotelsCost = 0;

            if (!req.ExcludeHotels)
            {
                var hotelReq = new HotelRecommendationRequestDto
                {
                    TripPlan = itinerary.Select(city => {
                        var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                        var co = ci.AddDays(city.Days);
                        return new HotelTripPlanItemDto
                        {
                            City = city.City,
                            CheckIn = ci.ToString("yyyy-MM-dd"),
                            CheckOut = co.ToString("yyyy-MM-dd")
                        };
                    }).ToList(),
                    Cluster = targetCluster,
                    TotalBudget = (double)hotelBudget,
                    NumPeople = totalPeople,
                    SingleRooms = req.SingleRooms,
                    DoubleRooms = req.DoubleRooms,
                    TopKPerCity = 8,
                    QualityThreshold = 0.45,
                    RegenerateIndex = 0
                };

                hotelApiRequestJsonStr = JsonSerializer.Serialize(hotelReq, new JsonSerializerOptions { WriteIndented = true });
                
                var client = _httpClientFactory.CreateClient();
                var hotelBaseUrl = _config["HotelRecommendationApi:BaseUrl"] ?? "https://travai-python-hotel.onrender.com";
                
                var content = new StringContent(hotelApiRequestJsonStr, System.Text.Encoding.UTF8, "application/json");
                try {
                    var response = await client.PostAsync($"{hotelBaseUrl.TrimEnd('/')}/api/hotels/recommend", content);
                    
                    if (response.IsSuccessStatusCode) {
                        var responseStr = await response.Content.ReadAsStringAsync();
                        hotelApiResponseJsonStr = responseStr;
                        
                        var apiResp = JsonSerializer.Deserialize<HotelRecommendationResponseDto>(responseStr);
                        
                        if (apiResp != null && apiResp.Hotels != null) {
                            var hotelNames = apiResp.Hotels.Select(h => h.HotelName).Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();
                            var dbHotels = await _db.Hotels
                                .Include(h => h.Rooms).Include(h => h.Images)
                                .Where(h => hotelNames.Contains(h.HotelName))
                                .ToDictionaryAsync(h => h.HotelName, StringComparer.OrdinalIgnoreCase);

                            foreach (var h in apiResp.Hotels) {
                                if (string.IsNullOrEmpty(h.HotelName)) continue;

                                int nights = 1;
                                if (DateTime.TryParse(h.CheckOut, out var coDate) && DateTime.TryParse(h.CheckIn, out var ciDate)) {
                                    nights = (coDate - ciDate).Days;
                                }

                                PlannedHotelDto pHotel;
                                if (dbHotels.TryGetValue(h.HotelName, out var dbHotel)) {
                                    var singleRoomsList = dbHotel.Rooms.Where(r => r.BedType == BedType.Single && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();
                                    var doubleRoomsList = dbHotel.Rooms.Where(r => r.BedType == BedType.Double && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();

                                    decimal singlePrice = singleRoomsList.Any() ? singleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                                    decimal doublePrice = doubleRoomsList.Any() ? doubleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                                    decimal total = (singlePrice * req.SingleRooms + doublePrice * req.DoubleRooms) * nights;

                                    pHotel = new PlannedHotelDto {
                                        Id = dbHotel.Id,
                                        HotelName = dbHotel.HotelName,
                                        City = dbHotel.CityArea ?? dbHotel.Governorate ?? h.CityArea ?? h.Governorate ?? "",
                                        Country = dbHotel.Country ?? "Egypt",
                                        StarRating = dbHotel.StarRating ?? h.StarRating,
                                        AvgReviewScore = dbHotel.AvgReviewScore,
                                        NumReviews = dbHotel.NumReviews,
                                        ImageUrl = dbHotel.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? dbHotel.Images.FirstOrDefault()?.ImageUrl,
                                        Nights = nights,
                                        SingleRooms = req.SingleRooms,
                                        DoubleRooms = req.DoubleRooms,
                                        SingleRoomPricePerNight = singlePrice,
                                        DoubleRoomPricePerNight = doublePrice,
                                        TotalPrice = Math.Round(total, 2)
                                    };
                                } else {
                                    pHotel = new PlannedHotelDto {
                                        Id = 0,
                                        HotelName = h.HotelName,
                                        City = h.CityArea ?? h.Governorate ?? "",
                                        Country = "Egypt",
                                        StarRating = h.StarRating,
                                        AvgReviewScore = (decimal)(h.AvgReviewScore ?? 0.0),
                                        NumReviews = h.NumReviews ?? 0,
                                        Nights = nights,
                                        SingleRooms = req.SingleRooms,
                                        DoubleRooms = req.DoubleRooms,
                                        TotalPrice = 0
                                    };
                                }

                                actualHotelsCost += pHotel.TotalPrice;

                                var cityPlan = cityPlans.FirstOrDefault(cp => 
                                    cp.City.Equals(h.CityArea, StringComparison.OrdinalIgnoreCase) || 
                                    cp.City.Equals(h.Governorate, StringComparison.OrdinalIgnoreCase) || 
                                    cp.City.Equals(pHotel.City, StringComparison.OrdinalIgnoreCase));
                                
                                if (cityPlan != null) {
                                    cityPlan.Hotel = pHotel;
                                    cityPlan.CityHotelBudget = pHotel.TotalPrice;
                                }
                            }
                        }
                    } else {
                        hotelApiResponseJsonStr = $"HTTP Error: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}";
                    }
                } catch (Exception ex) {
                    hotelApiResponseJsonStr = $"Exception: {ex.Message}\n{ex.StackTrace}";
                }
            }

            // --- 2. REALLOCATE HOTEL SURPLUS TO TOURS BUDGET ---
            decimal hotelSurplus = Math.Max(0, hotelBudget - actualHotelsCost);
            if (!req.ExcludeTours)
            {
                toursBudget += hotelSurplus;
            }

            // --- 3. CALL TOUR RECOMMENDATION API ---
            string tourApiRequestJsonStr = "";
            string tourApiResponseJsonStr = null;
            string tourSessionId = null;
            decimal actualToursCost = 0;

            if (!req.ExcludeTours)
            {
                var tourReq = new TourRecommendationRequestDto
                {
                    BudgetType = req.BudgetType,
                    Cities = itinerary.Select(c => c.City).ToList(),
                    Days = adjustedTourDays,
                    StartDate = tourStartDate.ToString("yyyy-MM-dd"),
                    EndDate = tourEndDate.ToString("yyyy-MM-dd"),
                    Preferences = new List<string>(),
                    TourBudget = toursBudget,
                    Travelers = totalPeople
                };

                tourApiRequestJsonStr = JsonSerializer.Serialize(tourReq, new JsonSerializerOptions { WriteIndented = true });
                
                var client = _httpClientFactory.CreateClient();
                var baseUrl = _config["TourRecommendationApi:BaseUrl"];
                if (!string.IsNullOrEmpty(baseUrl)) {
                    var content = new StringContent(tourApiRequestJsonStr, System.Text.Encoding.UTF8, "application/json");
                    try {
                        var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/tour/recommend", content);
                        
                        if (response.IsSuccessStatusCode) {
                            var responseStr = await response.Content.ReadAsStringAsync();
                            tourApiResponseJsonStr = responseStr;
                            
                            var apiResp = JsonSerializer.Deserialize<TourRecommendationResponseDto>(responseStr);
                            
                            if (apiResp != null && apiResp.Recommendations != null) {
                                tourSessionId = apiResp.SessionId;
                                var tourIds = apiResp.Recommendations.Where(r => r.Tour != null).Select(r => r.Tour.TourId).Distinct().ToList();
                                var dbTours = await _db.Tours
                                    .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                                    .Include(t => t.TourImages)
                                    .Where(t => tourIds.Contains(t.Id))
                                    .ToDictionaryAsync(t => t.Id);

                                foreach (var rec in apiResp.Recommendations) {
                                    if (rec.Tour != null) {
                                        dbTours.TryGetValue(rec.Tour.TourId, out var dbTour);
                                        
                                        var pTour = new PlannedTourDto {
                                            Id              = dbTour?.Id ?? rec.Tour.TourId,
                                            TourTitle       = dbTour?.TourTitle ?? rec.Tour.TourTitle ?? "AI Recommended Tour",
                                            City            = dbTour?.City ?? rec.City,
                                            GuideName       = dbTour?.TourGuide?.Name ?? rec.Tour.GuideName ?? "Auto-Assigned",
                                            TourType        = dbTour?.TourType ?? "Standard",
                                            TourDescription = dbTour?.TourDescription ?? "A great tour around the city.",
                                            Rating          = dbTour?.Rating ?? rec.Tour.Rating ?? 4.5m,
                                            NumberOfReviews = dbTour?.NumberOfReviews ?? rec.Tour.NumberOfReviews ?? 10,
                                            DurationHours   = dbTour?.DurationHours ?? (rec.Tour.DurationHours.HasValue ? (int?)Math.Round(rec.Tour.DurationHours.Value) : 4),
                                            SitesCovered    = dbTour?.SitesCovered ?? "Various attractions",
                                            TransportIncluded = dbTour?.TransportIncluded ?? false,
                                            MealsIncluded   = dbTour?.MealsIncluded ?? false,
                                            ImageUrl        = dbTour?.TourImages.FirstOrDefault()?.ImageUrl,
                                            AvailableDate   = DateTime.TryParse(rec.Date, out var d) ? d : (dbTour?.AvailableDateTime ?? DateTime.Now),
                                            PricePerPerson  = dbTour?.BasePriceUsd ?? rec.Tour.BasePriceUsd,
                                            TotalPrice      = Math.Round((dbTour?.BasePriceUsd ?? rec.Tour.BasePriceUsd) * totalPeople, 2)
                                        };
                                        
                                        actualToursCost += pTour.TotalPrice;

                                        var cityPlan = cityPlans.FirstOrDefault(cp => cp.City.Equals(rec.City, StringComparison.OrdinalIgnoreCase));
                                        if (cityPlan != null) {
                                            cityPlan.Tours.Add(pTour);
                                        }
                                    }
                                }

                                foreach (var cityPlan in cityPlans) {
                                    cityPlan.CityToursBudget = Math.Round(cityPlan.Tours.Sum(t => t.TotalPrice), 2);
                                }
                            }
                        } else {
                            tourApiResponseJsonStr = $"HTTP Error: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}";
                        }
                    } catch (Exception ex) { 
                        tourApiResponseJsonStr = $"Exception: {ex.Message}\n{ex.StackTrace}";
                    }
                }
            }

            return new TripPlanResponseDto
            {
                BudgetType         = req.BudgetType,
                MaxBudget          = req.MaxBudget,
                EstimatedTotalCost = Math.Round(actualFlightsCost + actualHotelsCost + actualToursCost, 2),
                TotalDays          = totalDays,
                Adults             = req.Adults,
                Children           = req.Children,
                FlightBudget       = Math.Round(flightBudget, 2),
                GoFlightBudget     = Math.Round(goBudget, 2),
                ReturnFlightBudget = Math.Round(retBudget, 2),
                HotelBudget        = Math.Round(hotelBudget,  2),
                ToursBudget        = Math.Round(toursBudget,  2),
                GoFlight           = goFlight,
                ReturnFlight       = retFlight,
                TourSessionId      = tourSessionId,
                CityPlans          = cityPlans,
                Itinerary          = itinerary,
                DebugData          = new PlanDebugDto
                {
                    TourApiRequestJson = tourApiRequestJsonStr,
                    TourApiResponseJson = tourApiResponseJsonStr,
                    HotelApiRequestJson = hotelApiRequestJsonStr,
                    HotelApiResponseJson = hotelApiResponseJsonStr,
                    MedianGo = goMedian,
                    MedianReturn = retMedian,
                    NumGo = goPriceList.Count,
                    NumReturn = retPriceList.Count,
                    MedianHotelsSingle = debugSMed,
                    MedianHotelsDouble = debugDMed,
                    NumHotelsSingle = debugNumSingle,
                    NumHotelsDouble = debugNumDouble,
                    NumberHotels = debugTotalRooms,
                    MedianTour = Median(allTourPrices),
                    NumTours = allTourPrices.Count,
                    TourDebugPerCity = tourDebugPerCity,
                    HotelDebugPerCity = hotelDebugPerCity
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Selection: AI-Powered Best Flight within budget
        // ─────────────────────────────────────────────────────────────────────
        private async Task<PlannedFlightDto?> GetAiRecommendedFlightAsync(
            List<string> fromCodes, List<string> toCodes, DateTime date, string flightClass,
            decimal budget, int adults, int children, string direction)
        {
            var exactDate = date.Date;

            var dbFlights = await _db.Flights
                .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                .Include(f => f.Airline)
                .Include(f => f.Segments)
                .Include(f => f.Layovers)
                .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                    && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                    && f.DepartureTime!.Value.Date == exactDate
                    && fromCodes.Contains(f.DepartureAirportCode!)
                    && toCodes.Contains(f.ArrivalAirportCode!))
                .ToListAsync();

            // Filter out flights that exceed the calculated group budget
            var validFlights = dbFlights
                .Where(f => FlightTotalPrice(f.Price!.Value, adults, children) <= budget)
                .ToList();

            if (!validFlights.Any() && dbFlights.Any())
            {
                // Fallback: If the allocated budget is too strict, take the absolute cheapest available 
                // flights so the user at least gets a recommendation (even if it exceeds the budget).
                validFlights = dbFlights.OrderBy(f => f.Price).Take(5).ToList();
            }

            if (!validFlights.Any()) return null;

            // IMPORTANT: Limit to top 10 flights to prevent exceeding LLM token limits (which causes 400 Bad Request)
            validFlights = validFlights.OrderBy(f => f.Price).Take(10).ToList();

            var requestDto = new FlightRecommendationRequestDto();
            foreach (var f in validFlights)
            {
                var segments = f.Segments?.OrderBy(s => s.SegmentNumber).ToList() ?? new List<TravAi.Airline.Models.Airlines.FlightSegment>();
                var layovers = f.Layovers?.OrderBy(l => l.LayoverOrder).ToList() ?? new List<TravAi.Airline.Models.Airlines.FlightLayover>();

                var model = new FlightRecommendationModelDto
                {
                    DepartureCity = f.DepartureAirport?.City,
                    ArrivalCity = f.ArrivalAirport?.City,
                    FlightClass = f.FlightClass,
                    Airline = f.Airline?.Name,
                    Duration = f.Duration,
                    Stops = f.NumberOfStops ?? 0,
                    PriceUsd = (double)f.Price!.Value,
                    Route = direction == "Return" ? $"{f.ArrivalAirport?.City}_{f.DepartureAirport?.City}" : $"{f.DepartureAirport?.City}_{f.ArrivalAirport?.City}",
                    DepartureDatetime = f.DepartureTime?.ToString("M/d/yyyy H:mm"),
                    ArrivalDatetime = f.ArrivalTime?.ToString("M/d/yyyy H:mm"),
                    DurationMinutes = f.DurationMinutes ?? 0
                };

                if (segments.Count > 0)
                {
                    model.AmenitiesSegment1 = segments[0].Amenities;
                    model.LegroomSegment1 = segments[0].LegroomInches;
                }
                if (segments.Count > 1)
                {
                    model.AmenitiesSegment2 = segments[1].Amenities;
                    model.LegroomSegment2 = segments[1].LegroomInches;
                }
                if (segments.Count > 2)
                {
                    model.AmenitiesSegment3 = segments[2].Amenities;
                    model.LegroomSegment3 = segments[2].LegroomInches;
                }

                if (layovers.Count > 0) model.Stop1Airline = layovers[0].AirportName;
                if (layovers.Count > 1) model.Stop2Airline = layovers[1].AirportName;
                if (layovers.Count > 2) model.Stop3Airline = layovers[2].AirportName;

                requestDto.Flights.Add(model);
            }

            var baseUrl = _config["FlightRecommendationApi:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) return null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(2); // Increase timeout
                var jsonRequest = JsonSerializer.Serialize(requestDto);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                LogDebug($" [AI FLIGHT API REQUEST: {direction}]");
                LogDebug($"   - Sending {requestDto.Flights.Count} valid flights to API...");
                LogDebug($"   - API JSON Request Payload: {jsonRequest}");
                
                var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/recommend-flight", content);
                
                LogDebug($"   - API Response Status: {response.StatusCode}");

                var jsonResponse = await response.Content.ReadAsStringAsync();
                LogDebug($"   - API JSON Response: {jsonResponse}");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<FlightRecommendationResponseDto>(jsonResponse);
                    var bestFlightModel = apiResponse?.Flight;

                    if (bestFlightModel != null)
                    {
                        // Match the best flight back to our db flights (relaxed matching)
                        var bestDbFlight = validFlights.FirstOrDefault(f => 
                            Math.Abs((double)f.Price!.Value - bestFlightModel.PriceUsd) < 0.01 &&
                            f.Airline?.Name == bestFlightModel.Airline);

                        if (bestDbFlight == null) 
                        {
                            throw new Exception($"AI returned a flight, but it could not be matched to the local DB for {direction}.");
                        }
                        else
                        {
                            LogDebug($"   - SUCCESS: Matched DB Flight {bestDbFlight.Id} for {direction}");
                            return new PlannedFlightDto
                            {
                                SessionId            = apiResponse?.SessionId,
                                Id                   = bestDbFlight.Id,
                                FlightNumber         = bestDbFlight.FlightNumber ?? "",
                                AirlineName          = bestDbFlight.Airline?.Name ?? "",
                                DepartureAirportCode = bestDbFlight.DepartureAirportCode ?? "",
                                DepartureCity        = bestDbFlight.DepartureAirport?.City ?? "",
                                ArrivalAirportCode   = bestDbFlight.ArrivalAirportCode ?? "",
                                ArrivalCity          = bestDbFlight.ArrivalAirport?.City ?? "",
                                DepartureTime        = bestDbFlight.DepartureTime,
                                ArrivalTime          = bestDbFlight.ArrivalTime,
                                FlightClass          = bestDbFlight.FlightClass,
                                Duration             = bestDbFlight.Duration,
                                NumberOfStops        = bestDbFlight.NumberOfStops,
                                DestinationImageUrl  = bestDbFlight.DestinationImageUrl,
                                PricePerAdult        = bestDbFlight.Price!.Value,
                                PricePerChild        = bestDbFlight.Price!.Value * 0.75m,
                                TotalPrice           = Math.Round(FlightTotalPrice(bestDbFlight.Price!.Value, adults, children), 2),
                                Direction            = direction
                            };
                        }
                    }
                    
                    throw new Exception($"AI API returned success but no flight was found in the response for {direction}.");
                }
                else
                {
                    throw new Exception($"AI API Error for {direction}: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get flight recommendation for {direction}: {ex.Message}");
            }

        }

        // ─────────────────────────────────────────────────────────────────────
        //  Selection: Best Hotel within budget (highest rating within budget)
        // ─────────────────────────────────────────────────────────────────────
        private async Task<PlannedHotelDto?> SelectBestHotel(
            string city, int starMin, int starMax, decimal budget, int nights,
            int singleRooms, int doubleRooms)
        {
            var cityL = city.ToLower();
            var hotels = await _db.Hotels
                .Include(h => h.Rooms).Include(h => h.Images)
                .Where(h => h.Verified && h.Active
                    && h.StarRating >= starMin && h.StarRating <= starMax
                    && (h.Governorate != null && h.Governorate.ToLower() == cityL
                     || h.CityArea    != null && h.CityArea.ToLower()    == cityL))
                .ToListAsync();

            if (!hotels.Any()) return null;

            PlannedHotelDto? best = null;
            decimal bestScore = -1;

            foreach (var hotel in hotels)
            {
                var singleRoomsList = hotel.Rooms.Where(r => r.BedType == BedType.Single && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();
                var doubleRoomsList = hotel.Rooms.Where(r => r.BedType == BedType.Double && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();

                if (singleRooms > 0 && !singleRoomsList.Any()) continue;
                if (doubleRooms > 0 && !doubleRoomsList.Any()) continue;

                var singlePrice = singleRoomsList.Any() ? singleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                var doublePrice = doubleRoomsList.Any() ? doubleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;

                decimal total = (singlePrice * singleRooms + doublePrice * doubleRooms) * nights;
                if (total <= 0 || total > budget) continue;

                // Score: 60% rating + 40% budget efficiency
                double ratingScore  = (double)(hotel.AvgReviewScore / 5m);
                double budgetScore  = budget > 0 ? 1.0 - (double)(total / budget) : 0;
                double score        = ratingScore * 0.6 + budgetScore * 0.4;

                if ((decimal)score > bestScore)
                {
                    bestScore = (decimal)score;
                    best = new PlannedHotelDto
                    {
                        Id                     = hotel.Id,
                        HotelName              = hotel.HotelName,
                        City                   = hotel.Governorate ?? hotel.CityArea ?? city,
                        Country                = hotel.Country,
                        StarRating             = hotel.StarRating,
                        AvgReviewScore         = hotel.AvgReviewScore,
                        NumReviews             = hotel.NumReviews,
                        ImageUrl               = hotel.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                                              ?? hotel.Images.FirstOrDefault()?.ImageUrl,
                        Nights                 = nights,
                        SingleRooms            = singleRooms,
                        DoubleRooms            = doubleRooms,
                        SingleRoomPricePerNight= singlePrice,
                        DoubleRoomPricePerNight= doublePrice,
                        TotalPrice             = Math.Round(total, 2)
                    };
                }
            }

            return best;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Selection: Best Tour within budget (highest TourScore)
        // ─────────────────────────────────────────────────────────────────────
        private async Task<PlannedTourDto?> SelectBestTour(
            string city, decimal priceMin, decimal priceMax,
            TourGuide.Models.Enums.Language? lang,
            DateTime checkIn, DateTime checkOut,
            decimal budget, int totalPeople)
        {
            var tours = await _db.Tours
                .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                .Include(t => t.TourImages)
                .Where(t => t.Active && t.BasePriceUsd.HasValue
                    && t.City!.ToLower() == city.ToLower()
                    && t.BasePriceUsd >= priceMin
                    && (priceMax < 0 || t.BasePriceUsd <= priceMax)
                    && t.AvailableDateTime >= checkIn && t.AvailableDateTime < checkOut
                    && (lang == null || t.TourGuide.TourGuideLanguages.Any(l => l.Language == lang.Value)))
                .ToListAsync();

            if (!tours.Any()) return null;

            var best = tours
                .Select(t => new { Tour = t, Total = t.BasePriceUsd!.Value * totalPeople })
                .Where(x => x.Total <= budget)
                .OrderByDescending(x => x.Tour.TourScore ?? 0)
                .FirstOrDefault();

            if (best == null) return null;
            var t2 = best.Tour;

            return new PlannedTourDto
            {
                Id              = t2.Id,
                TourTitle       = t2.TourTitle,
                City            = t2.City ?? city,
                GuideName       = t2.TourGuide.Name,
                TourType        = t2.TourType,
                TourDescription = t2.TourDescription,
                Rating          = t2.Rating,
                NumberOfReviews = t2.NumberOfReviews,
                DurationHours   = t2.DurationHours,
                SitesCovered    = t2.SitesCovered,
                TransportIncluded = t2.TransportIncluded,
                MealsIncluded   = t2.MealsIncluded,
                ImageUrl        = t2.TourImages.FirstOrDefault()?.ImageUrl,
                AvailableDate   = t2.AvailableDateTime,
                PricePerPerson  = t2.BasePriceUsd!.Value,
                TotalPrice      = Math.Round(best.Total, 2)
            };
        }


        // ─────────────────────────────────────────────────────────────────────
        //  Regenerate Flight Alternative
        // ─────────────────────────────────────────────────────────────────────
        public async Task<PlannedFlightDto?> RegenerateFlightAsync(string sessionId, int adults, int children, string direction)
        {


            var baseUrl = _config["FlightRecommendationApi:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) return null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestObj = new FlightRegenerateRequestDto { SessionId = sessionId };
                var jsonRequest = JsonSerializer.Serialize(requestObj);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/regenerate-flight", content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<FlightRegenerateResponseDto>(jsonResponse);
                    var altFlightModel = apiResponse?.Flight;

                    if (altFlightModel != null)
                    {
                        decimal targetPrice = (decimal)altFlightModel.PriceUsd;
                        decimal lowerBound = targetPrice - 1m;
                        decimal upperBound = targetPrice + 1m;

                        var candidateFlights = await _db.Flights
                            .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                            .Include(f => f.Airline)
                            .Where(f => f.Airline != null && f.Airline.Name == altFlightModel.Airline &&
                                        f.Price >= lowerBound && f.Price <= upperBound)
                            .ToListAsync();

                        var altDbFlight = candidateFlights.FirstOrDefault(f => 
                            f.DepartureTime?.ToString("M/d/yyyy H:mm") == altFlightModel.DepartureDatetime);

                        if (altDbFlight != null)
                        {
                            return new PlannedFlightDto
                            {
                                SessionId            = sessionId, // keep session id so they can regenerate again!
                                Id                   = altDbFlight.Id,
                                FlightNumber         = altDbFlight.FlightNumber ?? "",
                                AirlineName          = altDbFlight.Airline?.Name ?? "",
                                DepartureAirportCode = altDbFlight.DepartureAirportCode ?? "",
                                DepartureCity        = altDbFlight.DepartureAirport?.City ?? "",
                                ArrivalAirportCode   = altDbFlight.ArrivalAirportCode ?? "",
                                ArrivalCity          = altDbFlight.ArrivalAirport?.City ?? "",
                                DepartureTime        = altDbFlight.DepartureTime,
                                ArrivalTime          = altDbFlight.ArrivalTime,
                                FlightClass          = altDbFlight.FlightClass,
                                Duration             = altDbFlight.Duration,
                                NumberOfStops        = altDbFlight.NumberOfStops,
                                DestinationImageUrl  = altDbFlight.DestinationImageUrl,
                                PricePerAdult        = altDbFlight.Price!.Value,
                                PricePerChild        = altDbFlight.Price!.Value * 0.75m,
                                TotalPrice           = Math.Round(FlightTotalPrice(altDbFlight.Price!.Value, adults, children), 2),
                                Direction            = direction
                            };
                        }
                        else
                        {
                            throw new Exception("AI returned a flight, but it could not be matched to the local DB.");
                        }
                    }
                    
                    throw new Exception("AI API returned success but no flight was found in the response.");
                }
                else
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<(List<PlannedTourDto> Tours, string RequestJson, string ResponseJson)> RegenerateTourAsync(string sessionId, List<string> fixedDates, int totalPeople)
        {
            var reqObj = new RegenerateTourPythonRequestDto
            {
                SessionId = sessionId,
                FixedDates = fixedDates ?? new List<string>()
            };

            var jsonReq = JsonSerializer.Serialize(reqObj, new JsonSerializerOptions { WriteIndented = true });
            var content = new StringContent(jsonReq, System.Text.Encoding.UTF8, "application/json");

            var baseUrl = _config["TourRecommendationApi:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) return (new List<PlannedTourDto>(), jsonReq, "Base URL not configured.");

            var client = _httpClientFactory.CreateClient();
            
            try
            {
                var response = await client.PostAsync($"{baseUrl.TrimEnd('/')}/tour/regenerate", content);

                if (!response.IsSuccessStatusCode)
                {
                    var failResponse = await response.Content.ReadAsStringAsync();
                    return (new List<PlannedTourDto>(), jsonReq, failResponse);
                }

                var responseStr = await response.Content.ReadAsStringAsync();
                var apiResp = JsonSerializer.Deserialize<RegenerateTourPythonResponseDto>(responseStr);

                if (apiResp == null || apiResp.Recommendations == null || apiResp.Recommendations.Count == 0)
                    return (new List<PlannedTourDto>(), jsonReq, responseStr);

                var tourIds = apiResp.Recommendations.Where(r => r.Tour != null).Select(r => r.Tour.TourId).Distinct().ToList();
                var dbTours = await _db.Tours
                    .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                    .Include(t => t.TourImages)
                    .Where(t => tourIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id);

                var generatedTours = new List<PlannedTourDto>();

                foreach (var rec in apiResp.Recommendations)
                {
                    if (rec.Tour != null)
                    {
                        dbTours.TryGetValue(rec.Tour.TourId, out var dbTour);
                        
                        var pTour = new PlannedTourDto
                        {
                            Id = dbTour?.Id ?? rec.Tour.TourId,
                            TourTitle = dbTour?.TourTitle ?? rec.Tour.TourTitle ?? "AI Recommended Tour",
                            City = dbTour?.City ?? rec.City,
                            GuideName = dbTour?.TourGuide?.Name ?? rec.Tour.GuideName ?? "Auto-Assigned",
                            TourType = dbTour?.TourType ?? "Standard",
                            TourDescription = dbTour?.TourDescription ?? "A great tour around the city.",
                            Rating = dbTour?.Rating ?? rec.Tour.Rating ?? 4.5m,
                            NumberOfReviews = dbTour?.NumberOfReviews ?? rec.Tour.NumberOfReviews ?? 10,
                            DurationHours = dbTour?.DurationHours ?? (rec.Tour.DurationHours.HasValue ? (int?)Math.Round(rec.Tour.DurationHours.Value) : 4),
                            SitesCovered = dbTour?.SitesCovered ?? "Various attractions",
                            TransportIncluded = dbTour?.TransportIncluded ?? false,
                            MealsIncluded = dbTour?.MealsIncluded ?? false,
                            ImageUrl = dbTour?.TourImages.FirstOrDefault()?.ImageUrl,
                            AvailableDate = DateTime.TryParse(rec.Date, out var d) ? d : (dbTour?.AvailableDateTime ?? DateTime.Now),
                            PricePerPerson = dbTour?.BasePriceUsd ?? rec.Tour.BasePriceUsd,
                            TotalPrice = Math.Round((dbTour?.BasePriceUsd ?? rec.Tour.BasePriceUsd) * totalPeople, 2)
                        };
                        generatedTours.Add(pTour);
                    }
                }

                return (generatedTours, jsonReq, responseStr);
            }
            catch (Exception ex)
            {
                return (new List<PlannedTourDto>(), jsonReq, ex.ToString());
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Regenerate Complete/Partial Trip Plan (Unified Locks & Two-Pass Budget)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TripPlanResponseDto> RegeneratePlanAsync(RegeneratePlanRequestDto req)
        {
            var itinerary = BuildItinerary(req);
            int totalDays = itinerary.Sum(c => c.Days);
            int totalPeople = req.Adults + req.Children;
            var flightClass = GetFlightClass(req.BudgetType);
            string targetTourType = req.BudgetType.ToLower() switch
            {
                "economy" => "economy",
                "premium" => "premium",
                "luxury"  => "luxury",
                _         => "economy"
            };

            var fromCodesP = await _db.Airports.Where(a => a.City.ToLower() == req.FromCity.ToLower()).Select(a => a.Code).ToListAsync();
            var firstCity = itinerary.First().City;
            var toCodesP = await _db.Airports.Where(a => a.City.ToLower() == firstCity.ToLower()).Select(a => a.Code).ToListAsync();
            var lastCity = itinerary.Last().City;
            var lastCodesP = await _db.Airports.Where(a => a.City.ToLower() == lastCity.ToLower()).Select(a => a.Code).ToListAsync();

            // 1. Flights Resolution (Use Fixed or Regenerate)
            PlannedFlightDto? goFlight = null;
            if (req.IsGoFlightFixed && req.FixedGoFlightId.HasValue)
            {
                var f = await _db.Flights
                    .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                    .Include(f => f.Airline)
                    .FirstOrDefaultAsync(fl => fl.Id == req.FixedGoFlightId.Value);
                if (f != null)
                {
                    goFlight = new PlannedFlightDto
                    {
                        SessionId            = req.GoFlightSessionId,
                        Id                   = f.Id,
                        FlightNumber         = f.FlightNumber ?? "",
                        AirlineName          = f.Airline?.Name ?? "",
                        DepartureAirportCode = f.DepartureAirportCode ?? "",
                        DepartureCity        = f.DepartureAirport?.City ?? "",
                        ArrivalAirportCode   = f.ArrivalAirportCode ?? "",
                        ArrivalCity          = f.ArrivalAirport?.City ?? "",
                        DepartureTime        = f.DepartureTime,
                        ArrivalTime          = f.ArrivalTime,
                        FlightClass          = f.FlightClass,
                        Duration             = f.Duration,
                        NumberOfStops        = f.NumberOfStops,
                        DestinationImageUrl  = f.DestinationImageUrl,
                        PricePerAdult        = f.Price!.Value,
                        PricePerChild        = f.Price!.Value * 0.75m,
                        TotalPrice           = Math.Round(FlightTotalPrice(f.Price!.Value, req.Adults, req.Children), 2),
                        Direction            = "Outbound"
                    };
                }
            }
            else if (!req.ExcludeFlights && !string.IsNullOrEmpty(req.GoFlightSessionId))
            {
                goFlight = await RegenerateFlightAsync(req.GoFlightSessionId, req.Adults, req.Children, "Outbound");
            }

            PlannedFlightDto? retFlight = null;
            if (req.IsReturnFlightFixed && req.FixedReturnFlightId.HasValue)
            {
                var f = await _db.Flights
                    .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                    .Include(f => f.Airline)
                    .FirstOrDefaultAsync(fl => fl.Id == req.FixedReturnFlightId.Value);
                if (f != null)
                {
                    retFlight = new PlannedFlightDto
                    {
                        SessionId            = req.ReturnFlightSessionId,
                        Id                   = f.Id,
                        FlightNumber         = f.FlightNumber ?? "",
                        AirlineName          = f.Airline?.Name ?? "",
                        DepartureAirportCode = f.DepartureAirportCode ?? "",
                        DepartureCity        = f.DepartureAirport?.City ?? "",
                        ArrivalAirportCode   = f.ArrivalAirportCode ?? "",
                        ArrivalCity          = f.ArrivalAirport?.City ?? "",
                        DepartureTime        = f.DepartureTime,
                        ArrivalTime          = f.ArrivalTime,
                        FlightClass          = f.FlightClass,
                        Duration             = f.Duration,
                        NumberOfStops        = f.NumberOfStops,
                        DestinationImageUrl  = f.DestinationImageUrl,
                        PricePerAdult        = f.Price!.Value,
                        PricePerChild        = f.Price!.Value * 0.75m,
                        TotalPrice           = Math.Round(FlightTotalPrice(f.Price!.Value, req.Adults, req.Children), 2),
                        Direction            = "Return"
                    };
                }
            }
            else if (!req.ExcludeFlights && !string.IsNullOrEmpty(req.ReturnFlightSessionId))
            {
                retFlight = await RegenerateFlightAsync(req.ReturnFlightSessionId, req.Adults, req.Children, "Return");
            }

            // 2. Tours Resolution
            var generatedTours = new List<PlannedTourDto>();
            string tourApiRequestJsonStr = "";
            string tourApiResponseJsonStr = null;
            if (!req.ExcludeTours && !string.IsNullOrEmpty(req.TourSessionId))
            {
                var tourResult = await RegenerateTourAsync(req.TourSessionId, req.FixedTourDates, totalPeople);
                generatedTours = tourResult.Tours ?? new List<PlannedTourDto>();
                tourApiRequestJsonStr = tourResult.RequestJson;
                tourApiResponseJsonStr = tourResult.ResponseJson;
            }

            // 3. Tour & Hotel Date Calculations
            DateTime tourStartDate = req.DepartureDate.Date.AddDays(1);
            if (!req.ExcludeFlights && goFlight != null && goFlight.ArrivalTime.HasValue)
            {
                tourStartDate = goFlight.ArrivalTime.Value.Date.AddDays(1);
            }

            DateTime tourEndDate = req.ReturnDate.Date.AddDays(-1);
            if (!req.ExcludeFlights && retFlight != null && retFlight.DepartureTime.HasValue)
            {
                tourEndDate = retFlight.DepartureTime.Value.Date.AddDays(-1);
            }

            List<int> originalDays = itinerary.Select(c => c.Days).ToList();
            List<int> adjustedTourDays = new List<int>(originalDays);

            if (adjustedTourDays.Count == 1)
            {
                adjustedTourDays[0] = Math.Max(1, (int)(tourEndDate - tourStartDate).TotalDays + 1);
            }
            else if (adjustedTourDays.Count > 1)
            {
                int startDiff = (int)(tourStartDate - req.DepartureDate.Date).TotalDays;
                int endDiff = (int)(req.ReturnDate.Date - tourEndDate).TotalDays;
                
                adjustedTourDays[0] = Math.Max(1, adjustedTourDays[0] - startDiff);
                int lastIdx = adjustedTourDays.Count - 1;
                adjustedTourDays[lastIdx] = Math.Max(1, adjustedTourDays[lastIdx] - endDiff);
            }

            var tourDates = new List<(DateTime CheckIn, DateTime CheckOut)>();
            var currentTourDate = tourStartDate;
            for (int i = 0; i < itinerary.Count; i++)
            {
                var days = adjustedTourDays[i];
                var nextTourDate = currentTourDate.AddDays(days);
                tourDates.Add((currentTourDate, nextTourDate));
                currentTourDate = nextTourDate;
            }

            // 4. DB Hotel & Tours Scanning (For budget weight calculations)
            var hotelSingleMedians = new List<decimal>();
            var hotelDoubleMedians = new List<decimal>();
            var tourMeans          = new List<(string city, decimal mean, int days)>();
            var allTourPrices      = new List<decimal>();
            var tourDebugPerCity   = new Dictionary<string, string>();
            var hotelDebugPerCity  = new Dictionary<string, string>();
            var hotelCosts         = new Dictionary<string, decimal>();
            var tourCosts          = new Dictionary<string, decimal>();

            decimal debugSMed = 0, debugDMed = 0;
            int debugNumSingle = 0, debugNumDouble = 0, debugTotalRooms = 0;

            int cityIdx = 0;
            foreach (var city in itinerary)
            {
                if (!req.ExcludeHotels)
                {
                    var rooms = await _db.HotelRooms
                        .Include(r => r.Hotel)
                        .Where(r => r.Hotel!.Active && r.Hotel.Verified && r.Hotel.Governorate!.ToLower() == city.City.ToLower()
                                    && r.FBPrice.HasValue && r.FBPrice > 0)
                        .ToListAsync();

                    var sm = rooms.Where(r => r.BedType == BedType.Single).Select(r => r.FBPrice!.Value).ToList();
                    var dm = rooms.Where(r => r.BedType == BedType.Double).Select(r => r.FBPrice!.Value).ToList();

                    decimal sMed = Median(sm);
                    decimal dMed = Median(dm);

                    if (debugTotalRooms == 0) {
                        debugSMed = sMed; debugDMed = dMed;
                        debugNumSingle = sm.Count; debugNumDouble = dm.Count;
                        debugTotalRooms = rooms.Count;
                    }

                    decimal cityHotelCost = 0;
                    decimal sPart = 0;
                    decimal dPart = 0;
                    if (sm.Any())
                    {
                        sPart = sMed * city.Days * req.SingleRooms;
                        hotelSingleMedians.Add(sPart);
                        cityHotelCost += sPart;
                    }
                    if (dm.Any())
                    {
                        dPart = dMed * city.Days * req.DoubleRooms;
                        hotelDoubleMedians.Add(dPart);
                        cityHotelCost += dPart;
                    }

                    hotelDebugPerCity[city.City] = $"Single Med: {sMed} * {req.SingleRooms} Rms * {city.Days} Days = {sPart} | Double Med: {dMed} * {req.DoubleRooms} Rms * {city.Days} Days = {dPart} | Total Est: {cityHotelCost} (Rooms: {rooms.Count})";
                    hotelCosts[city.City] = cityHotelCost;
                }
                else
                {
                    hotelDebugPerCity[city.City] = "Excluded";
                    hotelCosts[city.City] = 0;
                }

                if (!req.ExcludeTours)
                {
                    var tourCi = tourDates[cityIdx].CheckIn;
                    var tourCo = tourDates[cityIdx].CheckOut;
                    var cityLower = city.City.ToLower();
                    
                    var lang = ParseLanguage(req.TouristLanguage);

                    var tp = await _db.Tours
                        .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                        .Where(t => t.Active && t.BasePriceUsd.HasValue
                            && t.City!.ToLower() == cityLower
                            && t.TourType != null && t.TourType.ToLower() == targetTourType
                            && t.AvailableDateTime >= tourCi && t.AvailableDateTime < tourCo
                            && (lang == null || t.TourGuide.TourGuideLanguages.Any(l => l.Language == lang.Value)))
                        .Select(t => t.BasePriceUsd!.Value).ToListAsync();

                    decimal cityTourCost = 0;
                    decimal cityMedian = 0;
                    int cityCount = tp.Count;
                    if (tp.Any())
                    {
                        cityMedian = Median(tp);
                        tourMeans.Add((city.City, cityMedian, adjustedTourDays[cityIdx]));
                        allTourPrices.AddRange(tp);
                        cityTourCost = cityMedian * adjustedTourDays[cityIdx] * totalPeople;
                    }
                    tourCosts[city.City] = cityTourCost;
                    tourDebugPerCity[city.City] = $"Median: {cityMedian} * {totalPeople} Pax * {adjustedTourDays[cityIdx]} Days = {cityTourCost} (Count: {cityCount})";
                }
                else
                {
                    tourDebugPerCity[city.City] = "Excluded";
                    tourCosts[city.City] = 0;
                }
                cityIdx++;
            }

            // 5. Dynamic Budget Re-allocation
            decimal actualGoFlightCost = goFlight?.TotalPrice ?? 0;
            decimal actualRetFlightCost = retFlight?.TotalPrice ?? 0;
            decimal actualFlightsCost = actualGoFlightCost + actualRetFlightCost;
            decimal actualToursCost = generatedTours.Sum(t => t.TotalPrice);

            decimal remainingBudget = Math.Max(0, req.MaxBudget - actualFlightsCost - actualToursCost);
            decimal flightBudget = actualFlightsCost;
            decimal goBudget = actualGoFlightCost;
            decimal retBudget = actualRetFlightCost;
            decimal hotelBudget = 0;
            decimal toursBudget = actualToursCost;

            decimal avgHotel = hotelSingleMedians.Sum() + hotelDoubleMedians.Sum();
            decimal avgTours = tourMeans.Sum(t => t.mean * t.days * totalPeople);

            if (req.ExcludeHotels && req.ExcludeTours)
            {
                hotelBudget = 0;
                toursBudget = 0;
                flightBudget = req.MaxBudget;
                goBudget = actualFlightsCost > 0
                    ? flightBudget * (actualGoFlightCost / actualFlightsCost) : flightBudget / 2;
                retBudget = flightBudget - goBudget;
            }
            else
            {
                var reallocation = DivideBudget(
                    remainingBudget,
                    excludeFlights: true,
                    req.ExcludeHotels,
                    req.ExcludeTours,
                    0,
                    avgHotel,
                    avgTours);

                flightBudget = actualFlightsCost;
                goBudget = actualGoFlightCost;
                retBudget = actualRetFlightCost;
                hotelBudget = reallocation.hotelAlloc;
                toursBudget = actualToursCost + reallocation.toursAlloc;
            }

            // 6. Build City Plans structure
            var cityPlans = new List<CityPlanDto>();
            foreach (var city in itinerary)
            {
                decimal cityHotelBudget = 0;
                if (avgHotel > 0 && hotelCosts.ContainsKey(city.City))
                {
                    cityHotelBudget = (hotelCosts[city.City] / avgHotel) * hotelBudget;
                }
                else
                {
                    double dayRatio = totalDays > 0 ? (double)city.Days / totalDays : 0;
                    cityHotelBudget = (decimal)dayRatio * hotelBudget;
                }

                var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                var co = ci.AddDays(city.Days);

                cityPlans.Add(new CityPlanDto
                {
                    City            = city.City,
                    Days            = city.Days,
                    CheckIn         = ci,
                    CheckOut        = co,
                    CityHotelBudget = Math.Round(cityHotelBudget, 2),
                    CityToursBudget = 0,
                    Hotel           = null,
                    Tour            = null
                });
            }

            // --- 1. RESOLVE HOTELS ---
            string hotelApiRequestJsonStr = "";
            string hotelApiResponseJsonStr = null;
            decimal actualHotelsCost = 0;

            if (!req.ExcludeHotels)
            {
                if (req.IsHotelFixed && req.FixedHotelId.HasValue)
                {
                    var h = await _db.Hotels
                        .Include(h => h.Rooms).Include(h => h.Images)
                        .FirstOrDefaultAsync(hot => hot.Id == req.FixedHotelId.Value);
                    if (h != null)
                    {
                        var city = itinerary.FirstOrDefault(c => c.City.Equals(h.Governorate, StringComparison.OrdinalIgnoreCase) 
                                                              || c.City.Equals(h.CityArea, StringComparison.OrdinalIgnoreCase));
                        int nights = city?.Days ?? 1;

                        var singleRoomsList = h.Rooms.Where(r => r.BedType == BedType.Single && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();
                        var doubleRoomsList = h.Rooms.Where(r => r.BedType == BedType.Double && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();

                        decimal singlePrice = singleRoomsList.Any() ? singleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                        decimal doublePrice = doubleRoomsList.Any() ? doubleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                        decimal total = (singlePrice * req.SingleRooms + doublePrice * req.DoubleRooms) * nights;

                        var pHotel = new PlannedHotelDto
                        {
                            Id = h.Id,
                            HotelName = h.HotelName,
                            City = h.CityArea ?? h.Governorate ?? "",
                            Country = h.Country ?? "Egypt",
                            StarRating = h.StarRating,
                            AvgReviewScore = h.AvgReviewScore,
                            NumReviews = h.NumReviews,
                            ImageUrl = h.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? h.Images.FirstOrDefault()?.ImageUrl,
                            Nights = nights,
                            SingleRooms = req.SingleRooms,
                            DoubleRooms = req.DoubleRooms,
                            SingleRoomPricePerNight = singlePrice,
                            DoubleRoomPricePerNight = doublePrice,
                            TotalPrice = Math.Round(total, 2)
                        };

                        actualHotelsCost += pHotel.TotalPrice;

                        var cityPlan = cityPlans.FirstOrDefault(cp => 
                            cp.City.Equals(h.CityArea, StringComparison.OrdinalIgnoreCase) || 
                            cp.City.Equals(h.Governorate, StringComparison.OrdinalIgnoreCase) || 
                            cp.City.Equals(pHotel.City, StringComparison.OrdinalIgnoreCase));
                        
                        if (cityPlan != null)
                        {
                            cityPlan.Hotel = pHotel;
                            cityPlan.CityHotelBudget = pHotel.TotalPrice;
                        }
                    }
                }
                else
                {
                    string targetCluster = req.BudgetType.ToLower() switch
                    {
                        "economy" => "economic",
                        "premium" => "premium",
                        "luxury"  => "business",
                        _         => "economic"
                    };

                    var hotelReq = new HotelRecommendationRequestDto
                    {
                        TripPlan = itinerary.Select(city => {
                            var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                            var co = ci.AddDays(city.Days);
                            return new HotelTripPlanItemDto
                            {
                                City = city.City,
                                CheckIn = ci.ToString("yyyy-MM-dd"),
                                CheckOut = co.ToString("yyyy-MM-dd")
                            };
                        }).ToList(),
                        Cluster = targetCluster,
                        TotalBudget = (double)hotelBudget,
                        NumPeople = totalPeople,
                        SingleRooms = req.SingleRooms,
                        DoubleRooms = req.DoubleRooms,
                        TopKPerCity = 8,
                        QualityThreshold = 0.45,
                        RegenerateIndex = req.HotelRegenerateIndex
                    };

                    hotelApiRequestJsonStr = JsonSerializer.Serialize(hotelReq, new JsonSerializerOptions { WriteIndented = true });
                    
                    var client = _httpClientFactory.CreateClient();
                    var hotelBaseUrl = _config["HotelRecommendationApi:BaseUrl"] ?? "https://travai-python-hotel.onrender.com";
                    
                    var content = new StringContent(hotelApiRequestJsonStr, System.Text.Encoding.UTF8, "application/json");
                    try {
                        var response = await client.PostAsync($"{hotelBaseUrl.TrimEnd('/')}/api/hotels/recommend", content);
                        
                        if (response.IsSuccessStatusCode) {
                            var responseStr = await response.Content.ReadAsStringAsync();
                            hotelApiResponseJsonStr = responseStr;
                            
                            var apiResp = JsonSerializer.Deserialize<HotelRecommendationResponseDto>(responseStr);
                            
                            if (apiResp != null && apiResp.Hotels != null) {
                                var hotelNames = apiResp.Hotels.Select(h => h.HotelName).Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();
                                var dbHotels = await _db.Hotels
                                    .Include(h => h.Rooms).Include(h => h.Images)
                                    .Where(h => hotelNames.Contains(h.HotelName))
                                    .ToDictionaryAsync(h => h.HotelName, StringComparer.OrdinalIgnoreCase);

                                foreach (var h in apiResp.Hotels) {
                                    if (string.IsNullOrEmpty(h.HotelName)) continue;

                                    int nights = 1;
                                    if (DateTime.TryParse(h.CheckOut, out var coDate) && DateTime.TryParse(h.CheckIn, out var ciDate)) {
                                        nights = (coDate - ciDate).Days;
                                    }

                                    PlannedHotelDto pHotel;
                                    if (dbHotels.TryGetValue(h.HotelName, out var dbHotel)) {
                                        var singleRoomsList = dbHotel.Rooms.Where(r => r.BedType == BedType.Single && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();
                                        var doubleRoomsList = dbHotel.Rooms.Where(r => r.BedType == BedType.Double && r.FBPrice.HasValue && r.State == RoomState.Active).ToList();

                                        decimal singlePrice = singleRoomsList.Any() ? singleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                                        decimal doublePrice = doubleRoomsList.Any() ? doubleRoomsList.Select(r => r.FBPrice!.Value).Min() : 0;
                                        decimal total = (singlePrice * req.SingleRooms + doublePrice * req.DoubleRooms) * nights;

                                        pHotel = new PlannedHotelDto {
                                            Id = dbHotel.Id,
                                            HotelName = dbHotel.HotelName,
                                            City = dbHotel.CityArea ?? dbHotel.Governorate ?? h.CityArea ?? h.Governorate ?? "",
                                            Country = dbHotel.Country ?? "Egypt",
                                            StarRating = dbHotel.StarRating ?? h.StarRating,
                                            AvgReviewScore = dbHotel.AvgReviewScore,
                                            NumReviews = dbHotel.NumReviews,
                                            ImageUrl = dbHotel.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? dbHotel.Images.FirstOrDefault()?.ImageUrl,
                                            Nights = nights,
                                            SingleRooms = req.SingleRooms,
                                            DoubleRooms = req.DoubleRooms,
                                            SingleRoomPricePerNight = singlePrice,
                                            DoubleRoomPricePerNight = doublePrice,
                                            TotalPrice = Math.Round(total, 2)
                                        };
                                    } else {
                                        pHotel = new PlannedHotelDto {
                                            Id = 0,
                                            HotelName = h.HotelName,
                                            City = h.CityArea ?? h.Governorate ?? "",
                                            Country = "Egypt",
                                            StarRating = h.StarRating,
                                            AvgReviewScore = (decimal)(h.AvgReviewScore ?? 0.0),
                                            NumReviews = h.NumReviews ?? 0,
                                            Nights = nights,
                                            SingleRooms = req.SingleRooms,
                                            DoubleRooms = req.DoubleRooms,
                                            TotalPrice = 0
                                        };
                                    }

                                    actualHotelsCost += pHotel.TotalPrice;

                                    var cityPlan = cityPlans.FirstOrDefault(cp => 
                                        cp.City.Equals(h.CityArea, StringComparison.OrdinalIgnoreCase) || 
                                        cp.City.Equals(h.Governorate, StringComparison.OrdinalIgnoreCase) || 
                                        cp.City.Equals(pHotel.City, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (cityPlan != null) {
                                        cityPlan.Hotel = pHotel;
                                        cityPlan.CityHotelBudget = pHotel.TotalPrice;
                                    }
                                }
                            }
                        } else {
                            hotelApiResponseJsonStr = $"HTTP Error: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}";
                        }
                    } catch (Exception ex) {
                        hotelApiResponseJsonStr = $"Exception: {ex.Message}\n{ex.StackTrace}";
                    }
                }
            }

            // --- 2. REALLOCATE SURPLUS TO TOURS ---
            decimal hotelSurplus = Math.Max(0, hotelBudget - actualHotelsCost);
            if (!req.ExcludeTours)
            {
                toursBudget += hotelSurplus;
            }

            // Map the regenerated tours to cityPlans
            foreach (var pTour in generatedTours)
            {
                var cityPlan = cityPlans.FirstOrDefault(cp => cp.City.Equals(pTour.City, StringComparison.OrdinalIgnoreCase));
                if (cityPlan != null)
                {
                    cityPlan.Tours.Add(pTour);
                }
            }

            // Set each city's tours budget to the sum of recommended tours in that city
            foreach (var cityPlan in cityPlans)
            {
                cityPlan.CityToursBudget = Math.Round(cityPlan.Tours.Sum(t => t.TotalPrice), 2);
            }

            return new TripPlanResponseDto
            {
                BudgetType         = req.BudgetType,
                MaxBudget          = req.MaxBudget,
                EstimatedTotalCost = Math.Round(actualFlightsCost + actualHotelsCost + actualToursCost, 2),
                TotalDays          = totalDays,
                Adults             = req.Adults,
                Children           = req.Children,
                FlightBudget       = Math.Round(flightBudget, 2),
                GoFlightBudget     = Math.Round(goBudget, 2),
                ReturnFlightBudget = Math.Round(retBudget, 2),
                HotelBudget        = Math.Round(hotelBudget,  2),
                ToursBudget        = Math.Round(toursBudget,  2),
                GoFlight           = goFlight,
                ReturnFlight       = retFlight,
                TourSessionId      = req.TourSessionId,
                CityPlans          = cityPlans,
                Itinerary          = itinerary,
                DebugData          = new PlanDebugDto
                {
                    TourApiRequestJson = tourApiRequestJsonStr,
                    TourApiResponseJson = tourApiResponseJsonStr,
                    HotelApiRequestJson = hotelApiRequestJsonStr,
                    HotelApiResponseJson = hotelApiResponseJsonStr,
                    MedianGo = Median(new List<decimal>()), 
                    MedianReturn = Median(new List<decimal>()),
                    NumGo = 0,
                    NumReturn = 0,
                    MedianHotelsSingle = debugSMed,
                    MedianHotelsDouble = debugDMed,
                    NumHotelsSingle = debugNumSingle,
                    NumHotelsDouble = debugNumDouble,
                    NumberHotels = debugTotalRooms,
                    MedianTour = Median(allTourPrices),
                    NumTours = allTourPrices.Count,
                    TourDebugPerCity = tourDebugPerCity,
                    HotelDebugPerCity = hotelDebugPerCity
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Unified Budget Divider Logic
        // ─────────────────────────────────────────────────────────────────────
        private (decimal flightAlloc, decimal hotelAlloc, decimal toursAlloc) DivideBudget(
            decimal budget,
            bool excludeFlights,
            bool excludeHotels,
            bool excludeTours,
            decimal avgFlights,
            decimal avgHotel,
            decimal avgTours)
        {
            int activeCount = (excludeFlights ? 0 : 1)
                            + (excludeHotels  ? 0 : 1)
                            + (excludeTours   ? 0 : 1);

            if (activeCount == 0)
            {
                return (0, 0, 0);
            }

            decimal marketSum = (excludeFlights ? 0 : avgFlights)
                              + (excludeHotels  ? 0 : avgHotel)
                              + (excludeTours   ? 0 : avgTours);

            if (marketSum <= 0)
            {
                decimal part = budget / activeCount;
                return (excludeFlights ? 0 : part,
                        excludeHotels  ? 0 : part,
                        excludeTours   ? 0 : part);
            }

            decimal flightAlloc = excludeFlights ? 0 : (avgFlights / marketSum) * budget;
            decimal hotelAlloc  = excludeHotels  ? 0 : (avgHotel  / marketSum) * budget;
            decimal toursAlloc  = excludeTours   ? 0 : (avgTours  / marketSum) * budget;

            return (flightAlloc, hotelAlloc, toursAlloc);
        }
    }
}
