using TravAi;
using TravAi.Data;
using Microsoft.EntityFrameworkCore;

using TravAi.Airline.DTOs.Companion;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Airline.Models;

namespace TravAi.Airline.Services.CompanionService
{
    public class CompanionService : ICompanionService
    {
        private readonly ApplicationDbContext _context;

        public CompanionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserCompanionDto>> GetMyCompanionsAsync(long userId)
        {
            return await _context.UserCompanions
                .Where(c => c.UserId == userId)
                .Select(c => new UserCompanionDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    AgeType = c.AgeType,
                    PassportNumber = c.PassportNumber,
                    Nationality = c.Nationality,
                    ProfilePic = c.ProfilePic,
                    PassportImage = c.PassportImage,
                    DateOfBirth = c.DateOfBirth,
                    PassportExpireDate = c.PassportExpireDate,
                    Gender = c.Gender.ToString()
                })
                .ToListAsync();
        }

        public async Task<UserCompanionDto> AddCompanionAsync(long userId, CreateCompanionDto dto)
        {
            // Parse Gender enum safely
            if (!Enum.TryParse<TravAi.Models.Enums.Gender>(dto.Gender, true, out var genderEnum))
                genderEnum = TravAi.Models.Enums.Gender.Male;

            var companion = new UserCompanion
            {
                UserId = userId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                AgeType = dto.AgeType,
                PassportNumber = dto.PassportNumber,
                Nationality = dto.Nationality,
                ProfilePic = dto.ProfilePic,
                PassportImage = dto.PassportImage,
                DateOfBirth = dto.DateOfBirth,
                PassportExpireDate = dto.PassportExpireDate,
                Gender = genderEnum
            };

            _context.UserCompanions.Add(companion);
            await _context.SaveChangesAsync();

            return new UserCompanionDto
            {
                Id = companion.Id,
                FirstName = companion.FirstName,
                LastName = companion.LastName,
                AgeType = companion.AgeType,
                PassportNumber = companion.PassportNumber,
                Nationality = companion.Nationality,
                ProfilePic = companion.ProfilePic,
                PassportImage = companion.PassportImage,
                DateOfBirth = companion.DateOfBirth,
                PassportExpireDate = companion.PassportExpireDate,
                Gender = companion.Gender.ToString()
            };
        }

        public async Task DeleteCompanionAsync(long userId, long companionId)
        {
            var companion = await _context.UserCompanions
                .FirstOrDefaultAsync(c => c.Id == companionId && c.UserId == userId);

            if (companion == null) throw new Exception("Companion not found.");

            _context.UserCompanions.Remove(companion);
            await _context.SaveChangesAsync();
        }
    }
}



