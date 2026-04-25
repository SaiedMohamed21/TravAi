using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;
using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Services.HotelService;

namespace UserAuthorizationandAuthentication.Controllers.Hotel
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Hotel")] // Only verified hotels can manage rooms
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class HotelRoomsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelRoomsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // POST: api/HotelRooms
        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            try
            {
                var userId = GetUserId();
                var room = await _hotelService.CreateRoomAsync(userId, request);
                
                return Ok(new ApiResponse<RoomDto>
                {
                    Success = true,
                    Message = "Room created successfully.",
                    Data = room,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HotelRooms/hotel/{hotelId}
        [HttpGet("hotel/{hotelId}")]
        [AllowAnonymous] // Public can view rooms
        public async Task<IActionResult> GetRoomsByHotel(long hotelId)
        {
            var rooms = await _hotelService.GetRoomsByHotelAsync(hotelId);
            
            return Ok(new ApiResponse<List<RoomDto>>
            {
                Success = true,
                Message = "Rooms retrieved.",
                Data = rooms,
                Errors = new List<string>()
            });
        }

        // PUT: api/HotelRooms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(long id, [FromBody] UpdateRoomRequest request)
        {
            try
            {
                var userId = GetUserId();
                var room = await _hotelService.UpdateRoomAsync(userId, id, request);
                
                return Ok(new ApiResponse<RoomDto>
                {
                    Success = true,
                    Message = "Room updated successfully.",
                    Data = room,
                    Errors = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null!,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/HotelRooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(long id)
        {
            var userId = GetUserId();
            var result = await _hotelService.DeleteRoomAsync(userId, id);
            
            if (!result)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Room not found or you don't have permission.",
                    Data = null!,
                    Errors = new List<string> { "Not found" }
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Room deleted successfully.",
                Data = null!,
                Errors = new List<string>()
            });
        }

        private long GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user.");
            }
            return userId;
        }
    }
}



