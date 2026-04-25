using Microsoft.AspNetCore.Mvc;
using UserAuthorizationandAuthentication.DTOs.Hotel;
using UserAuthorizationandAuthentication.Services.HotelService;

namespace UserAuthorizationandAuthentication.Controllers.Hotel
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class PublicRoomsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public PublicRoomsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        /// <summary>
        /// Search for rooms with filters (Hotel, Price, Occupancy, BedType, State)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<RoomSearchResponse>> SearchRooms([FromQuery] RoomSearchRequest request)
        {
            try
            {
                var result = await _hotelService.SearchRoomsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get details of a specific room by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDetailsDto>> GetRoomDetails(long id)
        {
            try
            {
                var room = await _hotelService.GetRoomDetailsAsync(id);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}



