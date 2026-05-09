using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.DTOs.AdminManagement;
using TravAi.Models.Hotels;

namespace TravAi.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class AdminManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Amenities Management

        [HttpGet("amenities")]
        public async Task<IActionResult> GetAmenities()
        {
            var amenities = await _context.Amenities
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .Select(a => new AmenityManageDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Category = a.Category,
                    IsHighlighted = a.IsHighlighted
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<AmenityManageDto>>(amenities));
        }

        [HttpPost("amenities")]
        public async Task<IActionResult> CreateAmenity([FromBody] AmenityManageDto dto)
        {
            var amenity = new Amenity
            {
                Name = dto.Name,
                Category = dto.Category,
                IsHighlighted = dto.IsHighlighted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<AmenityManageDto>(dto));
        }

        [HttpPut("amenities/{id}")]
        public async Task<IActionResult> UpdateAmenity(long id, [FromBody] AmenityManageDto dto)
        {
            var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
            if (amenity == null) return NotFound();

            amenity.Name = dto.Name;
            amenity.Category = dto.Category;
            amenity.IsHighlighted = dto.IsHighlighted;
            amenity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<AmenityManageDto>(dto));
        }

        [HttpDelete("amenities/{id}")]
        public async Task<IActionResult> DeleteAmenity(long id)
        {
            var amenity = await _context.Amenities.Include(a => a.HotelAmenities).FirstOrDefaultAsync(a => a.Id == id);
            if (amenity == null) return NotFound();

            if (amenity.HotelAmenities.Any())
            {
                return BadRequest(new ApiResponse<string>(false, "Cannot delete amenity because it is used by hotels. Please deactivate instead (if supported).", null));
            }

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<string>("Amenity deleted successfully."));
        }

        #endregion

        #region Legal Documents Management

        [HttpGet("document-types")]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var docs = await _context.DocumentTypes
                .OrderBy(d => d.Name)
                .Select(d => new DocumentTypeManageDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    KeyName = d.KeyName,
                    IsRequired = d.IsRequired,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<DocumentTypeManageDto>>(docs));
        }

        [HttpPost("document-types")]
        public async Task<IActionResult> CreateDocumentType([FromBody] DocumentTypeManageDto dto)
        {
            var doc = new DocumentTypeDefinition
            {
                Name = dto.Name,
                KeyName = dto.KeyName,
                IsRequired = dto.IsRequired,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DocumentTypes.Add(doc);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<DocumentTypeManageDto>(dto));
        }

        [HttpPut("document-types/{id}")]
        public async Task<IActionResult> UpdateDocumentType(long id, [FromBody] DocumentTypeManageDto dto)
        {
            var doc = await _context.DocumentTypes.FindAsync(id);
            if (doc == null) return NotFound();

            doc.Name = dto.Name;
            doc.KeyName = dto.KeyName;
            doc.IsRequired = dto.IsRequired;
            doc.IsActive = dto.IsActive;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<DocumentTypeManageDto>(dto));
        }

        [HttpDelete("document-types/{id}")]
        public async Task<IActionResult> DeleteDocumentType(long id)
        {
            var doc = await _context.DocumentTypes.Include(d => d.HotelDocuments).FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return NotFound();

            if (doc.HotelDocuments.Any())
            {
                // Soft delete by deactivating
                doc.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string>("Document type is used by hotels. Deactivated instead of deleting."));
            }

            _context.DocumentTypes.Remove(doc);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<string>("Document type deleted successfully."));
        }

        #endregion

        #region Additional Data Management

        [HttpGet("field-definitions")]
        public async Task<IActionResult> GetFieldDefinitions()
        {
            var fields = await _context.HotelFieldDefinitions
                .OrderBy(f => f.DisplayName)
                .Select(f => new FieldDefinitionManageDto
                {
                    Id = f.Id,
                    KeyName = f.KeyName,
                    DisplayName = f.DisplayName,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    IsActive = f.IsActive
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<FieldDefinitionManageDto>>(fields));
        }

        [HttpPost("field-definitions")]
        public async Task<IActionResult> CreateFieldDefinition([FromBody] FieldDefinitionManageDto dto)
        {
            var field = new HotelFieldDefinition
            {
                KeyName = dto.KeyName,
                DisplayName = dto.DisplayName,
                FieldType = dto.FieldType,
                IsRequired = dto.IsRequired,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HotelFieldDefinitions.Add(field);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<FieldDefinitionManageDto>(dto));
        }

        [HttpPut("field-definitions/{id}")]
        public async Task<IActionResult> UpdateFieldDefinition(long id, [FromBody] FieldDefinitionManageDto dto)
        {
            var field = await _context.HotelFieldDefinitions.FindAsync(id);
            if (field == null) return NotFound();

            field.KeyName = dto.KeyName;
            field.DisplayName = dto.DisplayName;
            field.FieldType = dto.FieldType;
            field.IsRequired = dto.IsRequired;
            field.IsActive = dto.IsActive;
            field.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<FieldDefinitionManageDto>(dto));
        }

        [HttpDelete("field-definitions/{id}")]
        public async Task<IActionResult> DeleteFieldDefinition(long id)
        {
            var field = await _context.HotelFieldDefinitions.Include(f => f.Values).FirstOrDefaultAsync(f => f.Id == id);
            if (field == null) return NotFound();

            if (field.Values.Any())
            {
                // Soft delete by deactivating
                field.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string>("Field definition is used by hotels. Deactivated instead of deleting."));
            }

            _context.HotelFieldDefinitions.Remove(field);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<string>("Field definition deleted successfully."));
        }

        #endregion
    }
}



