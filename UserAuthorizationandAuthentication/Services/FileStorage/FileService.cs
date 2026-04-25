using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UserAuthorizationandAuthentication.Services.FileStorage
{
    public interface IFileService
    {
        Task<string?> SaveHotelImageAsync(IFormFile? file);
        Task<string?> SaveHotelDocumentAsync(IFormFile? file);
        Task<string?> SaveProfileImageAsync(IFormFile? file);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedDocExtensions = { ".pdf", ".doc", ".docx" };

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveHotelImageAsync(IFormFile? file)
        {
            return await SaveFileInternalAsync(file, "hotels/images", "hotelimg", AllowedImageExtensions);
        }

        public async Task<string?> SaveHotelDocumentAsync(IFormFile? file)
        {
            return await SaveFileInternalAsync(file, "hotels/documents", "hoteldoc", AllowedDocExtensions);
        }

        public async Task<string?> SaveProfileImageAsync(IFormFile? file)
        {
            return await SaveFileInternalAsync(file, "profiles", "profile", AllowedImageExtensions);
        }

        private async Task<string?> SaveFileInternalAsync(IFormFile? file, string subFolder, string prefix, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                throw new Exception($"File type {ext} not allowed for {subFolder}.");

            // Define absolute path
            var baseFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

            // Generate unique name: prefix_yyyyMMdd_guid.ext
            var uniqueName = $"{prefix}_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(baseFolder, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL for DB
            return $"/uploads/{subFolder}/{uniqueName}";
        }
    }
}
