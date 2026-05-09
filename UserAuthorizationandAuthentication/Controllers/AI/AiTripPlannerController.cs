using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.DTOs.AI;
using TravAi.DTOs.Common;
using TravAi.Services.AI;

namespace TravAi.Controllers.AI
{
    [Route("api/ai")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "AI")]
    public class AiTripPlannerController : ControllerBase
    {
        private readonly IAiTripPlannerService _aiService;
        private readonly ApplicationDbContext _db;

        public AiTripPlannerController(IAiTripPlannerService aiService, ApplicationDbContext db)
        {
            _aiService = aiService;
            _db = db;
        }

        /// <summary>
        /// Returns all unique city names available in the database
        /// (airports, hotels, tours) – no auth required for autocomplete.
        /// </summary>
        [HttpGet("cities")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCities([FromQuery] string? q = null)
        {
            // Gather cities from all three sources
            var airportCities = await _db.Airports
                .Where(a => a.City != null && a.City != "")
                .Select(a => a.City!)
                .Distinct()
                .ToListAsync();

            var hotelCities = await _db.Hotels
                .Where(h => h.Active && h.Verified)
                .SelectMany(h => new[]
                {
                    h.Governorate ?? "",
                    h.CityArea    ?? ""
                })
                .Where(c => c != "")
                .Distinct()
                .ToListAsync();

            var tourCities = await _db.Tours
                .Where(t => t.Active && t.City != null && t.City != "")
                .Select(t => t.City!)
                .Distinct()
                .ToListAsync();

            var all = airportCities
                .Concat(hotelCities)
                .Concat(tourCities)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            // Optional search filter
            if (!string.IsNullOrWhiteSpace(q))
                all = all.Where(c => c.StartsWith(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Message = $"{all.Count} cities found.",
                Data    = all,
                Errors  = new List<string>()
            });
        }

        /// <summary>
        /// Step 2: Estimate trip cost ranges (Economy / Premium / Luxury)
        /// based on real data in the database filtered by route, dates, and cities.
        /// </summary>
        [HttpPost("estimate")]
        [Authorize]
        public async Task<IActionResult> EstimateBudget([FromBody] TripEstimateRequestDto request)
        {
            try
            {
                // Validate itinerary days == total trip days
                int totalDays = (request.ReturnDate.Date - request.DepartureDate.Date).Days;
                if (request.Itinerary != null && request.Itinerary.Any())
                {
                    int itineraryDays = request.Itinerary.Sum(c => c.Days);
                    if (itineraryDays != totalDays)
                        return BadRequest(new ApiResponse<string>(false,
                            $"Itinerary total days ({itineraryDays}) must equal trip duration ({totalDays} days).", null));
                }

                if (request.ReturnDate <= request.DepartureDate)
                    return BadRequest(new ApiResponse<string>(false, "Return date must be after departure date.", null));

                if (request.Adults < 1)
                    return BadRequest(new ApiResponse<string>(false, "At least 1 adult is required.", null));

                var result = await _aiService.EstimateBudgetAsync(request);

                return Ok(new ApiResponse<BudgetEstimationResponseDto>
                {
                    Success = true,
                    Message = "Budget estimation calculated from available data.",
                    Data    = result,
                    Errors  = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Step 3: Generate a complete trip plan.
        /// Applies budget_divider algorithm then selects the best flight/hotel/tour per city.
        /// </summary>
        [HttpPost("generate-plan")]
        [Authorize]
        public async Task<IActionResult> GeneratePlan([FromBody] TripPlanRequestDto request)
        {
            try
            {
                // Validate
                int totalDays = (request.ReturnDate.Date - request.DepartureDate.Date).Days;
                if (request.Itinerary != null && request.Itinerary.Any())
                {
                    int itineraryDays = request.Itinerary.Sum(c => c.Days);
                    if (itineraryDays != totalDays)
                        return BadRequest(new ApiResponse<string>(false,
                            $"Itinerary total days ({itineraryDays}) must equal trip duration ({totalDays} days).", null));
                }

                if (request.ReturnDate <= request.DepartureDate)
                    return BadRequest(new ApiResponse<string>(false, "Return date must be after departure date.", null));

                if (request.MaxBudget <= 0)
                    return BadRequest(new ApiResponse<string>(false, "MaxBudget must be greater than 0.", null));

                var validTypes = new[] { "Economy", "Premium", "Luxury" };
                if (!validTypes.Contains(request.BudgetType))
                    return BadRequest(new ApiResponse<string>(false, "BudgetType must be Economy, Premium, or Luxury.", null));

                var result = await _aiService.GeneratePlanAsync(request);

                return Ok(new ApiResponse<TripPlanResponseDto>
                {
                    Success = true,
                    Message = "Trip plan generated successfully.",
                    Data    = result,
                    Errors  = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message, new List<string> { ex.Message }));
            }
        }
    }
}
