using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.TourGuide.DTOs.TourGuide;
using TravAi.TourGuide.Services;
using TravAi.TourGuide.DTOs.WithdrawRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
namespace TravAi.TourGuide.Controllers
{
    [Route("api/tourguide/profile")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "TourGuide")]
    public class TourGuideController : ControllerBase
    {
        private readonly ITourGuideService _service;
        private readonly IBookingService _bookingService;

        public TourGuideController(ITourGuideService service, IBookingService bookingService)
        {
            _service = service;
            _bookingService = bookingService;
        }

        /// <summary>
        /// User applies to become a Tour Guide
        /// </summary>
        [Authorize(Roles = "User")]
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] TourGuideApplicationDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                 return Unauthorized("User ID not found in token.");
            }

            try
            {
                var result = await _service.ApplyAsync(userId, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get the application status of the currently logged in user
        /// </summary>
        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetMyApplicationStatus()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null)
            {
                return Ok(new { applicationStatus = "No Application" });
            }

            return Ok(new { applicationStatus = tourGuide.Status });
        }

        /// <summary>
        /// Tour Guide updates their license photos and waits for admin approval
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("update-license")]
        public async Task<IActionResult> UpdateLicense([FromBody] UpdateLicenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.UpdateLicenseAsync(tourGuide.Id, model);
            if (!result) return BadRequest("Could not update license.");

            return Ok("License updated successfully. Status changed to Pending awaiting admin approval.");
        }

        /// <summary>
        /// Tour Guide updates their profile and waits for admin approval
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.UpdateProfileAsync(tourGuide.Id, model);
            if (!result) return BadRequest("Could not update profile.");

            return Ok("Profile updated successfully. Status changed to Pending awaiting admin approval.");
        }

        /// <summary>
        /// Upload profile picture for the Tour Guide (User entity)
        /// </summary>
        [Authorize]
        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file, 
            [FromServices] IWebHostEnvironment env,
            [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var ext = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                return BadRequest("Only image files (jpg, jpeg, png, webp) are allowed.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var uploadsFolder = System.IO.Path.Combine(env.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "images");
            if (!System.IO.Directory.Exists(uploadsFolder))
                System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = System.Guid.NewGuid().ToString() + ext;
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{baseUrl}/uploads/images/{uniqueFileName}";

            // Update user profile pic
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.ProfilePic = fileUrl;
                await context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "Profile picture uploaded successfully",
                data = new { imageUrl = fileUrl }
            });
        }

        /// <summary>
        /// Upload license document (PDF or Image) for the Tour Guide
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("upload-license")]
        public async Task<IActionResult> UploadLicense(
            IFormFile file, 
            [FromServices] IWebHostEnvironment env,
            [FromServices] TravAi.Data.ApplicationDbContext context)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var ext = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                return BadRequest("Only PDF and image files (jpg, jpeg, png) are allowed.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var subFolder = ext == ".pdf" ? "documents" : "images";
            var uploadsFolder = System.IO.Path.Combine(env.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot"), "uploads", subFolder);
            
            if (!System.IO.Directory.Exists(uploadsFolder))
                System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = System.Guid.NewGuid().ToString() + ext;
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fileUrl = $"{baseUrl}/uploads/{subFolder}/{uniqueFileName}";

            // Update TourGuide license card
            var tourGuide = await context.TourGuides.FirstOrDefaultAsync(tg => tg.UserId == userId);
            if (tourGuide != null)
            {
                tourGuide.LicenseCard = fileUrl;
                await context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "License uploaded successfully",
                data = new { fileUrl = fileUrl }
            });
        }
        /// <summary>
        /// Tour Guide requests to withdraw money from their wallet/earnings
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawRequestDto model, [FromServices] IWithdrawRequestService withdrawRequestService)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            try
            {
                var result = await withdrawRequestService.CreateWithdrawRequestAsync(tourGuide.Id, model);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("not found", System.StringComparison.OrdinalIgnoreCase)) return NotFound(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tour Guide views their history of withdraw requests
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("withdraw")]
        public async Task<IActionResult> GetMyWithdrawalRequests([FromServices] IWithdrawRequestService withdrawRequestService)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await withdrawRequestService.GetMyRequestsAsync(tourGuide.Id);
            return Ok(result);
        }

        /// <summary>
        /// Get all reviews for a specific tour guide
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetTourGuideReviews(long id)
        {
            var reviews = await _service.GetTourGuideReviewsAsync(id);
            return Ok(reviews);
        }

        /// <summary>
        /// Get full profile information for a specific tour guide
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(long id)
        {
            var profile = await _service.GetProfileAsync(id);
            if (profile == null) return NotFound("Tour Guide profile not found.");
            return Ok(profile);
        }
        /// <summary>
        /// Get Dashboard Summary for the logged-in tour guide
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("/api/tourguide/dashboard")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.GetDashboardSummaryAsync(tourGuide.Id);
            return Ok(result);
        }

        /// <summary>
        /// Get Earnings Chart Data for the logged-in tour guide
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("/api/tourguide/earnings/chart")]
        public async Task<IActionResult> GetEarningsChart()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var result = await _service.GetEarningsChartAsync(tourGuide.Id);
            return Ok(result);
        }

        /// <summary>
        /// Get all bookings assigned to the logged-in tour guide
        /// </summary>
        [Authorize(Roles = "Tourguide")]
        [HttpGet("/api/tourguide/my-assigned-bookings")]
        public async Task<IActionResult> GetMyAssignedBookings([FromQuery] TravAi.TourGuide.Models.Enums.BookingStatus? status = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return Unauthorized("User ID not found in token.");

            var tourGuide = await _service.GetTourGuideByUserIdAsync(userId);
            if (tourGuide == null) return NotFound("Tour Guide profile not found.");

            var bookings = await _bookingService.GetAssignedBookingsAsync(tourGuide.Id, status);
            return Ok(bookings);
        }
    }
}

