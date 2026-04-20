using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.DTOs;

namespace UserAuthorizationandAuthentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Hotel")]
    public class MetadataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MetadataController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("amenities")]
        public async Task<IActionResult> GetAmenities()
        {
            var amenities = await _context.Amenities
                .Select(a => new AmenityDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Category = a.Category,
                    IsHighlighted = a.IsHighlighted
                })
                .ToListAsync();
            return Ok(new ApiResponse<List<AmenityDto>>(amenities));
        }

        [HttpGet("document-types")]
        public async Task<IActionResult> GetDocumentTypes()
        {
            var types = await _context.DocumentTypes
                .Select(d => new DocumentTypeDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    KeyName = d.KeyName,
                    IsRequired = d.IsRequired
                })
                .ToListAsync();
            return Ok(new ApiResponse<List<DocumentTypeDto>>(types));
        }

        [HttpGet("hotel-field-definitions")]
        public async Task<IActionResult> GetFieldDefinitions()
        {
            var fieldsList = await _context.HotelFieldDefinitions
                .Where(f => f.IsActive)
                .OrderBy(f => f.Id)
                .ToListAsync();
            var fields = fieldsList
                .Select(f => new HotelFieldDefinitionDto
                {
                    Id = f.Id,
                    KeyName = f.KeyName,
                    DisplayName = f.DisplayName,
                    FieldType = f.FieldType.ToString(),
                    IsRequired = f.IsRequired
                })
                .ToList();
            return Ok(new ApiResponse<List<HotelFieldDefinitionDto>>(fields));
        }
    }
}



