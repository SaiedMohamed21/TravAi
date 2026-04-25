using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;

namespace UserAuthorizationandAuthentication.Airline.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/airline/admin")]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class AdminController : ControllerBase
    {
        // =============================
        // Approve Airline
        // =============================
        [HttpPost("approve-airline")]
        public IActionResult ApproveAirline()
        {
            return Ok(new ApiResponse<string>(
                data: "Airline approved",
                message: "Success"
            ));
        }

        // =============================
        // Approve Hotel
        // =============================
        [HttpPost("approve-hotel")]
        public IActionResult ApproveHotel()
        {
            return Ok(new ApiResponse<string>(
                data: "Hotel approved",
                message: "Success"
            ));
        }

        // =============================
        // Approve Tour Guide
        // =============================
        [HttpPost("approve-tourguide")]
        public IActionResult ApproveTourGuide()
        {
            return Ok(new ApiResponse<string>(
                data: "Tour guide approved",
                message: "Success"
            ));
        }
    }
}
