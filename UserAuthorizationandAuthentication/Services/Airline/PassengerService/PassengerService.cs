using UserAuthorizationandAuthentication;
using Microsoft.EntityFrameworkCore;

using UserAuthorizationandAuthentication.Airline.DTOs.Passenger;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Airline.Models;

namespace UserAuthorizationandAuthentication.Airline.Services.PassengerService
{
    public class PassengerService : IPassengerService
    {
        private readonly ApplicationDbContext _context;

        public PassengerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PassengerResponseDto> CreateAsync(CreatePassengerDto dto)
        {
            // Verify booking exists
            var booking = await _context.Bookings.FindAsync(dto.BookingId);
            if (booking == null)
                throw new Exception("Booking not found.");

            var passenger = new Passenger
            {
                BookingId = dto.BookingId,
                PassengerType = dto.PassengerType,
                AgeType = dto.AgeType,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PassportNumber = dto.PassportNumber,
                Nationality = dto.Nationality,
                Price = dto.Price
            };

            // Add phones
            foreach (var phone in dto.PhoneNumbers)
            {
                passenger.Phones.Add(new PassengerPhone { PhoneNumber = phone });
            }

            // Add emergency contacts
            foreach (var contact in dto.EmergencyContacts)
            {
                passenger.EmergencyContacts.Add(new PassengerEmergencyContact 
                { 
                    EmergencyName = contact.Name, 
                    PhoneNumber = contact.PhoneNumber 
                });
            }

            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            return MapToResponseDto(passenger);
        }

        public async Task<List<PassengerResponseDto>> GetBookingPassengersAsync(long bookingId)
        {
            var passengers = await _context.Passengers
                .Include(p => p.Phones)
                .Include(p => p.EmergencyContacts)
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();

            return passengers.Select(MapToResponseDto).ToList();
        }

        public async Task<PassengerResponseDto?> GetByIdAsync(long id)
        {
            var passenger = await _context.Passengers
                .Include(p => p.Phones)
                .Include(p => p.EmergencyContacts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (passenger == null) return null;

            return MapToResponseDto(passenger);
        }

        // Helper method for mapping
        private PassengerResponseDto MapToResponseDto(Passenger p)
        {
            return new PassengerResponseDto
            {
                Id = p.Id,
                BookingId = p.BookingId,
                PassengerType = p.PassengerType,
                AgeType = p.AgeType,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PassportNumber = p.PassportNumber,
                Nationality = p.Nationality,
                Price = p.Price,
                PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList(),
                EmergencyContacts = p.EmergencyContacts.Select(ec => new EmergencyContactResponseDto
                {
                    Name = ec.EmergencyName,
                    PhoneNumber = ec.PhoneNumber
                }).ToList()
            };
        }

        public async Task UpdateAsync(long id, UpdatePassengerDto dto)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger == null)
                throw new Exception("Passenger not found.");

            // Basic updates... (skipping complex logic update for now to keep it simple, 
            // but AgeType can be added here)
            
            if (!string.IsNullOrEmpty(dto.FirstName))
                passenger.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                passenger.LastName = dto.LastName;

            // Save basic info
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger == null)
                throw new Exception("Passenger not found.");

            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
        }
    }
}



