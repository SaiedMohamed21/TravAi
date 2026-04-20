using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.DTOs.Hotel
{
    // --- Image Upload DTOs ---
    
    public class UploadImageRequest
    {
        [Required]
        public long HotelId { get; set; }
        
        [Required]
        public IFormFile Image { get; set; } = null!;
        
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateImageRequest
    {
        public string? Caption { get; set; }
        public bool? IsPrimary { get; set; }
        public int? SortOrder { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class ImageUploadResponse
    {
        public long Id { get; set; }
        public long HotelId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
