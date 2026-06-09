using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.DTOs.AI;
using TravAi.Models.Enums;
using static TravAi.Services.AI.AiTripPlannerHelpers;

namespace TravAi.Services.AI
{
    public class AiTripPlannerService : IAiTripPlannerService
    {
        private readonly ApplicationDbContext _db;
        public AiTripPlannerService(ApplicationDbContext db) => _db = db;

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
            var (tourMin, tourMax) = GetTourPriceRange(budgetType);
            var lang = ParseLanguage(req.TouristLanguage);
            string targetCluster = budgetType.ToLower() switch
            {
                "economy" => "economic",
                "premium" => "premium",
                "luxury"  => "business",
                _         => "economic"
            };

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

                // Search within ±7 days of departure date for best price estimate
                var goDateFrom = req.DepartureDate.Date.AddDays(-7);
                if (goDateFrom < DateTime.Today) goDateFrom = DateTime.Today;
                var goDateTo   = req.DepartureDate.Date.AddDays(7);
                var goPrices = await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date >= goDateFrom
                        && f.DepartureTime!.Value.Date <= goDateTo
                        && fromCodes.Contains(f.DepartureAirportCode!)
                        && toCodes.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

                // Search within ±7 days of return date for best price estimate
                var retDateFrom = req.ReturnDate.Date.AddDays(-7);
                if (retDateFrom < DateTime.Today) retDateFrom = DateTime.Today;
                var retDateTo   = req.ReturnDate.Date.AddDays(7);
                var retPrices = await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date >= retDateFrom
                        && f.DepartureTime!.Value.Date <= retDateTo
                        && lastCodes.Contains(f.DepartureAirportCode!)
                        && fromCodes.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

                if (goPrices.Any())
                {
                    flightMin += FlightTotalPrice(goPrices.Min(), req.Adults, req.Children);
                    flightMax += FlightTotalPrice(goPrices.Max(), req.Adults, req.Children);
                }
                if (retPrices.Any())
                {
                    flightMin += FlightTotalPrice(retPrices.Min(), req.Adults, req.Children);
                    flightMax += FlightTotalPrice(retPrices.Max(), req.Adults, req.Children);
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
                             || r.Hotel.CityArea    != null && r.Hotel.CityArea.ToLower()    == cityLower))
                        .ToListAsync();

                    var singles = rooms.Where(r => r.BedType == BedType.Single)
                                       .Select(r => r.FBPrice!.Value).ToList();
                    var doubles = rooms.Where(r => r.BedType == BedType.Double)
                                       .Select(r => r.FBPrice!.Value).ToList();

                    if (singles.Any() && req.SingleRooms > 0)
                    {
                        hotelMin += singles.Min() * req.SingleRooms * city.Days;
                        hotelMax += singles.Max() * req.SingleRooms * city.Days;
                    }
                    if (doubles.Any() && req.DoubleRooms > 0)
                    {
                        hotelMin += doubles.Min() * req.DoubleRooms * city.Days;
                        hotelMax += doubles.Max() * req.DoubleRooms * city.Days;
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
                            && t.BasePriceUsd >= tourMin
                            && (tourMax < 0 || t.BasePriceUsd <= tourMax)
                            && t.AvailableDateTime.HasValue
                            && t.AvailableDateTime >= ci && t.AvailableDateTime < co
                            && (lang == null || t.TourGuide.TourGuideLanguages
                                .Any(l => l.Language == lang.Value)))
                        .Select(t => t.BasePriceUsd!.Value).ToListAsync();

                    if (prices.Any())
                    {
                        toursMin += prices.Min() * totalPeople;
                        toursMax += prices.Max() * totalPeople;
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
            var (tourPMin, tourPMax) = GetTourPriceRange(req.BudgetType);
            var lang        = ParseLanguage(req.TouristLanguage);
            string targetCluster = req.BudgetType.ToLower() switch
            {
                "economy" => "economic",
                "premium" => "premium",
                "luxury"  => "business",
                _         => "economic"
            };
            int totalPeople = req.Adults + req.Children;

            // ── Fetch filtered flight prices for budget_divider ───────────────
            // Pre-fetch airport codes by city name
            var fromCodesP  = await _db.Airports.Where(a => a.City.ToLower() == req.FromCity.ToLower()).Select(a => a.Code).ToListAsync();
            var toCodesP    = await _db.Airports.Where(a => a.City.ToLower() == firstCity.ToLower()).Select(a => a.Code).ToListAsync();
            var lastCodesP  = await _db.Airports.Where(a => a.City.ToLower() == lastCity.ToLower()).Select(a => a.Code).ToListAsync();

            // ±7 days window — same as EstimateBudget so market averages are consistent
            var goDateFrom = req.DepartureDate.Date.AddDays(-7);
                if (goDateFrom < DateTime.Today) goDateFrom = DateTime.Today;
            var goDateTo   = req.DepartureDate.Date.AddDays(7);
            var goPriceList = req.ExcludeFlights ? new List<decimal>() :
                await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date >= goDateFrom
                        && f.DepartureTime!.Value.Date <= goDateTo
                        && fromCodesP.Contains(f.DepartureAirportCode!)
                        && toCodesP.Contains(f.ArrivalAirportCode!))
                    .Select(f => f.Price!.Value).ToListAsync();

            var retDateFrom = req.ReturnDate.Date.AddDays(-7);
                if (retDateFrom < DateTime.Today) retDateFrom = DateTime.Today;
            var retDateTo   = req.ReturnDate.Date.AddDays(7);
            var retPriceList = req.ExcludeFlights ? new List<decimal>() :
                await _db.Flights
                    .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                        && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                        && f.DepartureTime!.Value.Date >= retDateFrom
                        && f.DepartureTime!.Value.Date <= retDateTo
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

            // Per-city hotel & tour market averages
            var hotelSingleMedians = new List<decimal>();
            var hotelDoubleMedians = new List<decimal>();
            var tourMeans          = new List<(string city, decimal mean, int days)>();

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
                             || r.Hotel.CityArea    != null && r.Hotel.CityArea.ToLower()    == cityLowerP))
                        .ToListAsync();

                    var sm = rooms.Where(r => r.BedType == BedType.Single)
                                  .Select(r => r.FBPrice!.Value).ToList();
                    var dm = rooms.Where(r => r.BedType == BedType.Double)
                                  .Select(r => r.FBPrice!.Value).ToList();

                    decimal sMed = Median(sm);
                    decimal dMed = Median(dm);

                    LogDebug($"   - Hotels Selected    : Total Rooms={rooms.Count}");
                    LogDebug($"   - Single Rooms Count : {sm.Count} (Median={sMed})");
                    LogDebug($"   - Double Rooms Count : {dm.Count} (Median={dMed})");

                    if (sm.Any())
                    {
                        decimal sPart = sMed * city.Days * req.SingleRooms;
                        hotelSingleMedians.Add(sPart);
                        LogDebug($"   - Single Room Part   : {sMed} * {city.Days} days * {req.SingleRooms} rooms = {sPart}");
                    }
                    if (dm.Any())
                    {
                        decimal dPart = dMed * city.Days * req.DoubleRooms;
                        hotelDoubleMedians.Add(dPart);
                        LogDebug($"   - Double Room Part   : {dMed} * {city.Days} days * {req.DoubleRooms} rooms = {dPart}");
                    }
                }

                if (!req.ExcludeTours)
                {
                    var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                    var co = ci.AddDays(city.Days);
                    var cityLower = city.City.ToLower();
                    var tp = await _db.Tours
                        .Include(t => t.TourGuide).ThenInclude(tg => tg.TourGuideLanguages)
                        .Where(t => t.Active && t.BasePriceUsd.HasValue
                            && t.City!.ToLower() == cityLower
                            && t.BasePriceUsd >= tourPMin
                            && (tourPMax < 0 || t.BasePriceUsd <= tourPMax)
                            && t.AvailableDateTime >= ci && t.AvailableDateTime < co
                            && (lang == null || t.TourGuide.TourGuideLanguages.Any(l => l.Language == lang.Value)))
                        .Select(t => t.BasePriceUsd!.Value).ToListAsync();

                    if (tp.Any())
                    {
                        decimal mean = tp.Average();
                        tourMeans.Add((city.City, mean, city.Days));
                        LogDebug($"   - Tours Found in City: {tp.Count} (Mean/Average Price={mean})");
                    }
                }
                LogDebug(new string('-', 60));
            }

            decimal avgHotel = hotelSingleMedians.Sum() + hotelDoubleMedians.Sum();
            decimal avgTours = tourMeans.Sum(t => t.mean * t.days * totalPeople);
            decimal marketSum = avgGoFlight + avgRetFlight + avgHotel + avgTours;

            LogDebug($" [GLOBAL MARKET SUMMARY]");
            LogDebug($"   - avgHotel (Sum)     : {avgHotel}");
            LogDebug($"   - avgTours (Sum)     : {avgTours}");
            LogDebug($"   - marketSum Eq       : {avgGoFlight} + {avgRetFlight} + {avgHotel} + {avgTours} = {marketSum}");
            LogDebug(new string('-', 60));

            // Allocate budgets proportionally (budget_divider logic)
            decimal budget = req.MaxBudget;
            decimal flightBudget, hotelBudget, toursBudget;

            if (marketSum <= 0)
            {
                // Fallback: equal split among active categories
                int activeCount = (req.ExcludeFlights ? 0 : 1)
                                + (req.ExcludeHotels  ? 0 : 1)
                                + (req.ExcludeTours   ? 0 : 1);
                decimal part = activeCount > 0 ? budget / activeCount : budget;
                flightBudget = req.ExcludeFlights ? 0 : part;
                hotelBudget  = req.ExcludeHotels  ? 0 : part;
                toursBudget  = req.ExcludeTours   ? 0 : part;
                LogDebug(" [PROPORTIONAL ALLOCATION - FALLBACK RUN]");
            }
            else
            {
                flightBudget = ((avgGoFlight + avgRetFlight) / marketSum) * budget;
                hotelBudget  = (avgHotel / marketSum) * budget;
                toursBudget  = (avgTours  / marketSum) * budget;

                LogDebug($" [PROPORTIONAL ALLOCATION (budget_divider)]");
                LogDebug($"   - flightBudget Eq    : (({avgGoFlight} + {avgRetFlight}) / {marketSum}) * {budget} = {flightBudget}");
                LogDebug($"   - hotelBudget Eq     : ({avgHotel} / {marketSum}) * {budget} = {hotelBudget}");
                LogDebug($"   - toursBudget Eq     : ({avgTours} / {marketSum}) * {budget} = {toursBudget}");
            }
            LogDebug(" [DEBUG: BUDGET DIVIDER END]");
            LogDebug(new string('=', 60) + "\n");

            decimal goBudget  = (avgGoFlight  + avgRetFlight) > 0
                ? flightBudget * (avgGoFlight  / (avgGoFlight + avgRetFlight)) : flightBudget / 2;
            decimal retBudget = flightBudget - goBudget;

            // Per-city budgets for hotels/tours (proportional to days)
            int totalDays = itinerary.Sum(c => c.Days);
            var cityPlans = new List<CityPlanDto>();

            foreach (var city in itinerary)
            {
                double dayRatio = totalDays > 0 ? (double)city.Days / totalDays : 0;
                decimal cityHotelBudget = (decimal)dayRatio * hotelBudget;
                decimal cityToursBudget = (decimal)dayRatio * toursBudget;

                var ci = GetCityCheckIn(req.DepartureDate, itinerary, city.City);
                var co = ci.AddDays(city.Days);

                var cityLowerP = city.City.ToLower();
                cityPlans.Add(new CityPlanDto
                {
                    City            = city.City,
                    Days            = city.Days,
                    CheckIn         = ci,
                    CheckOut        = co,
                    CityHotelBudget = Math.Round(cityHotelBudget, 2),
                    CityToursBudget = Math.Round(cityToursBudget, 2),
                    Hotel           = req.ExcludeHotels ? null : await SelectBestHotel(city.City, starMin, starMax, cityHotelBudget, city.Days, req.SingleRooms, req.DoubleRooms),
                    Tour            = req.ExcludeTours ? null : await SelectBestTour(city.City, tourPMin, tourPMax, lang, ci, co, cityToursBudget, totalPeople)
                });
            }

            var goFlight = req.ExcludeFlights ? null : await SelectBestFlight(fromCodesP, toCodesP, req.DepartureDate, flightClass, goBudget, req.Adults, req.Children, "Outbound");
            var retFlight = req.ExcludeFlights ? null : await SelectBestFlight(lastCodesP, fromCodesP, req.ReturnDate, flightClass, retBudget, req.Adults, req.Children, "Return");

            return new TripPlanResponseDto
            {
                BudgetType         = req.BudgetType,
                MaxBudget          = req.MaxBudget,
                EstimatedTotalCost = Math.Round(req.MaxBudget, 2),
                TotalDays          = totalDays,
                Adults             = req.Adults,
                Children           = req.Children,
                FlightBudget       = Math.Round(flightBudget, 2),
                HotelBudget        = Math.Round(hotelBudget,  2),
                ToursBudget        = Math.Round(toursBudget,  2),
                GoFlight           = goFlight,
                ReturnFlight       = retFlight,
                CityPlans          = cityPlans,
                Itinerary          = itinerary
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Selection: Best Flight within budget
        // ─────────────────────────────────────────────────────────────────────
        private async Task<PlannedFlightDto?> SelectBestFlight(
            List<string> fromCodes, List<string> toCodes, DateTime date, string flightClass,
            decimal budget, int adults, int children, string direction)
        {
            // Search ±7 days; pick closest to requested date within budget
            var dateFrom = date.Date.AddDays(-7);
            if (dateFrom < DateTime.Today) dateFrom = DateTime.Today;
            var dateTo   = date.Date.AddDays(7);

            var flights = await _db.Flights
                .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                .Include(f => f.Airline)
                .Where(f => f.Status == "Active" && f.Price.HasValue && f.DepartureTime.HasValue
                    && f.FlightClass != null && f.FlightClass.ToLower() == flightClass.ToLower()
                    && f.DepartureTime!.Value.Date >= dateFrom
                    && f.DepartureTime!.Value.Date <= dateTo
                    && fromCodes.Contains(f.DepartureAirportCode!)
                    && toCodes.Contains(f.ArrivalAirportCode!)
                    && f.Price <= budget)
                .ToListAsync();

            if (!flights.Any()) return null;

            // Pick cheapest flight that's closest to the target date
            var best = flights
                .Select(f => new
                {
                    Flight   = f,
                    Total    = FlightTotalPrice(f.Price!.Value, adults, children),
                    DaysDiff = Math.Abs((f.DepartureTime!.Value.Date - date.Date).Days)
                })
                .Where(x => x.Total <= budget)
                .OrderBy(x => x.DaysDiff)   // closest date first
                .ThenBy(x => x.Total)        // then cheapest
                .FirstOrDefault();

            if (best == null) return null;
            var f2 = best.Flight;

            return new PlannedFlightDto
            {
                Id                   = f2.Id,
                FlightNumber         = f2.FlightNumber ?? "",
                AirlineName          = f2.Airline?.Name ?? "",
                DepartureAirportCode = f2.DepartureAirportCode ?? "",
                DepartureCity        = f2.DepartureAirport?.City ?? "",
                ArrivalAirportCode   = f2.ArrivalAirportCode ?? "",
                ArrivalCity          = f2.ArrivalAirport?.City ?? "",
                DepartureTime        = f2.DepartureTime,
                ArrivalTime          = f2.ArrivalTime,
                FlightClass          = f2.FlightClass,
                Duration             = f2.Duration,
                NumberOfStops        = f2.NumberOfStops,
                DestinationImageUrl  = f2.DestinationImageUrl,
                PricePerAdult        = f2.Price!.Value,
                PricePerChild        = f2.Price!.Value * 0.75m,
                TotalPrice           = Math.Round(best.Total, 2),
                Direction            = direction
            };
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
    }
}
