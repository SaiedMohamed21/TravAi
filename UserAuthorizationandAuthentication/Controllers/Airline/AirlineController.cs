using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAuthorizationandAuthentication.DTOs;
using UserAuthorizationandAuthentication.Airline.Models.Airlines;
using UserAuthorizationandAuthentication.Airline.DTOs.Airline;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/airlines")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AirlineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AirlineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =============================
        // Create Airline (Admin)
        // =============================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAirlineDto dto)
        {
            if (await _context.Airlines.AnyAsync(a => a.Name == dto.Name))
                return BadRequest(new ApiResponse<string>(false, "Airline already exists"));

            var airline = new UserAuthorizationandAuthentication.Airline.Models.Airlines.Airline
            {
                Name = dto.Name,
                UserId = dto.UserId, // Admin assigns the user owner
                Country = dto.Country,
                LogoUrl = dto.LogoUrl,
                Verified = true,
                Status = "Approved",
                IsApproved = true
            };

            _context.Airlines.Add(airline);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>(
                new { airline.Id, airline.Name },
                "Airline created successfully"
            ));
        }

        // =============================
        // Get All Airlines
        // =============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var airlines = await _context.Airlines
                .Select(a => new { a.Id, a.Name, a.Country, a.LogoUrl })
                .ToListAsync();

            return Ok(new ApiResponse<object>(airlines));
        }
    }
}
