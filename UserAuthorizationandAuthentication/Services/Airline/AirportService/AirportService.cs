using TravAi;
using TravAi.Data;
using Microsoft.EntityFrameworkCore;

using TravAi.Airline.DTOs.Airport;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Airline.Models;

namespace TravAi.Airline.Services.AirportService
{
    public class AirportService : IAirportService
    {
        private readonly ApplicationDbContext _context;

        public AirportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AirportDto>> GetAllAsync()
        {
            return await _context.Airports
                .Select(a => new AirportDto
                {
                    Code = a.Code,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country
                })
                .ToListAsync();
        }

        public async Task<List<AirportDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetAllAsync();

            query = query.ToLower();

            return await _context.Airports
                .Where(a => a.Code.ToLower().Contains(query) || 
                            a.Name.ToLower().Contains(query) || 
                            a.City.ToLower().Contains(query))
                .Select(a => new AirportDto
                {
                    Code = a.Code,
                    Name = a.Name,
                    City = a.City,
                    Country = a.Country
                })
                .ToListAsync();
        }

        public async Task<AirportDto?> GetByCodeAsync(string code)
        {
            var a = await _context.Airports.FindAsync(code);
            if (a == null) return null;

            return new AirportDto
            {
                Code = a.Code,
                Name = a.Name,
                City = a.City,
                Country = a.Country
            };
        }

        public async Task CreateAsync(CreateAirportDto dto)
        {
            if (await _context.Airports.AnyAsync(a => a.Code == dto.Code))
                throw new Exception("Airport code already exists.");

            var airport = new Airport
            {
                Code = dto.Code.ToUpper(),
                Name = dto.Name,
                City = dto.City,
                Country = dto.Country
            };

            _context.Airports.Add(airport);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string code)
        {
            var airport = await _context.Airports.FindAsync(code);
            if (airport == null)
                throw new Exception("Airport not found.");

            _context.Airports.Remove(airport);
            await _context.SaveChangesAsync();
        }
    }
}



