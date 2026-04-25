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
    [Authorize(Roles = "Hotel")] // Only verified hotels can upload images
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class HotelImagesController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelImagesController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // POST: api/HotelImages
        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var image = await _hotelService.UploadImageAsync(userId, request);
                
                return Ok(new ApiResponse<ImageUploadResponse>
                {
                    Success = true,
                    Message = "Image uploaded successfully.",
                    Data = image,
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

        // GET: api/HotelImages/hotel/{hotelId}
        [HttpGet("hotel/{hotelId}")]
        [AllowAnonymous] // Public can view images
        public async Task<IActionResult> GetHotelImages(long hotelId)
        {
            var images = await _hotelService.GetHotelImagesAsync(hotelId);
            
            return Ok(new ApiResponse<List<ImageUploadResponse>>
            {
                Success = true,
                Message = "Images retrieved.",
                Data = images,
                Errors = new List<string>()
            });
        }

        // PUT: api/HotelImages/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateImage(long id, [FromBody] UpdateImageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var image = await _hotelService.UpdateImageAsync(userId, id, request);
                
                return Ok(new ApiResponse<ImageUploadResponse>
                {
                    Success = true,
                    Message = "Image updated successfully.",
                    Data = image,
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

        // DELETE: api/HotelImages/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(long id)
        {
            var userId = GetUserId();
            var result = await _hotelService.DeleteImageAsync(userId, id);
            
            if (!result)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Image not found or you don't have permission.",
                    Data = null!,
                    Errors = new List<string> { "Not found" }
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Image deleted successfully.",
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



