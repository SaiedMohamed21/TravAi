using TravAi;
using TravAi.Data;
using Microsoft.EntityFrameworkCore;

using TravAi.Airline.DTOs.Passenger;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Airline.Models;

namespace TravAi.Airline.Services.PassengerService
{
    public class PassengerService : IPassengerService
    {
        private readonly ApplicationDbContext _context;

        public PassengerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PassengerResponseDto> CreateAsync(CreatePassengerDto dto, long userId)
        {
            // Verify booking exists
            var booking = await _context.Bookings.FindAsync(dto.BookingId);
            if (booking == null)
                throw new Exception("Booking not found.");
            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("You are not allowed to add passenger details for another user's booking.");

            var passenger = new Passenger
            {
                BookingId = dto.BookingId,
                PassengerType = dto.PassengerType,
                AgeType = dto.AgeType,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PassportNumber = dto.PassportNumber,
                Nationality = dto.Nationality,
                Price = dto.Price,
                DateOfBirth = dto.DateOfBirth,
                PassportExpiryDate = dto.PassportExpiryDate,
                Gender = dto.Gender
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

            await RecalculateBookingStatusAsync(passenger.BookingId);

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
                DateOfBirth = p.DateOfBirth,
                PassportExpiryDate = p.PassportExpiryDate,
                Gender = p.Gender,
                PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList(),
                EmergencyContacts = p.EmergencyContacts.Select(ec => new EmergencyContactResponseDto
                {
                    Name = ec.EmergencyName,
                    PhoneNumber = ec.PhoneNumber
                }).ToList()
            };
        }

        public async Task UpdateAsync(long id, UpdatePassengerDto dto, long userId)
        {
            var passenger = await _context.Passengers
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (passenger == null)
                throw new Exception("Passenger not found.");

            if (passenger.Booking == null)
                await _context.Entry(passenger).Reference(p => p.Booking).LoadAsync();

            if (passenger.Booking.UserId != userId)
                throw new UnauthorizedAccessException("You are not allowed to update passenger details for another user's booking.");

            if (!string.IsNullOrEmpty(dto.FirstName))
                passenger.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                passenger.LastName = dto.LastName;

            if (!string.IsNullOrEmpty(dto.PassportNumber))
                passenger.PassportNumber = dto.PassportNumber;

            if (!string.IsNullOrEmpty(dto.Nationality))
                passenger.Nationality = dto.Nationality;

            if (!string.IsNullOrEmpty(dto.PassengerType))
                passenger.PassengerType = dto.PassengerType;

            if (dto.Price.HasValue)
                passenger.Price = dto.Price.Value;

            if (dto.DateOfBirth.HasValue)
                passenger.DateOfBirth = dto.DateOfBirth.Value;

            if (dto.PassportExpiryDate.HasValue)
                passenger.PassportExpiryDate = dto.PassportExpiryDate.Value;

            if (!string.IsNullOrEmpty(dto.Gender))
                passenger.Gender = dto.Gender;

            await _context.SaveChangesAsync();
            await RecalculateBookingStatusAsync(passenger.BookingId);
        }

        public async Task DeleteAsync(long id, long userId)
        {
            var passenger = await _context.Passengers
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (passenger == null)
                throw new Exception("Passenger not found.");

            if (passenger.Booking == null)
                await _context.Entry(passenger).Reference(p => p.Booking).LoadAsync();

            if (passenger.Booking.UserId != userId)
                throw new UnauthorizedAccessException("You are not allowed to delete passenger details for another user's booking.");

            long bookingId = passenger.BookingId;
            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
            await RecalculateBookingStatusAsync(bookingId);
        }

        public async Task SavePassengerDetailsAsync(long bookingId, SaveBookingPassengersRequest request, long userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new KeyNotFoundException("Booking not found.");

            if (booking.UserId != userId) throw new UnauthorizedAccessException("You are not allowed to add/edit passenger details for another user's booking.");

            var existingPassengers = booking.Passengers.ToList();

            for (int i = 0; i < request.Passengers.Count; i++)
            {
                var input = request.Passengers[i];
                Passenger passenger;

                if (i < existingPassengers.Count)
                {
                    passenger = existingPassengers[i];
                }
                else
                {
                    if (booking.Passengers.Count >= booking.NumberOfSeats)
                        break;

                    passenger = new Passenger { BookingId = bookingId, Status = "Pending" };
                    _context.Passengers.Add(passenger);
                    booking.Passengers.Add(passenger);
                }

                passenger.FirstName = input.FirstName;
                passenger.LastName = input.LastName;
                passenger.PassportNumber = input.PassportNumber;
                passenger.Nationality = input.Nationality;
                passenger.PassengerType = input.PassengerType;
                passenger.AgeType = input.AgeType;
                passenger.Price = input.Price > 0 ? input.Price : (booking.TotalPrice / booking.NumberOfSeats);
                passenger.DateOfBirth = input.DateOfBirth;
                passenger.PassportExpiryDate = input.PassportExpiryDate;
                passenger.Gender = input.Gender;
            }

            await _context.SaveChangesAsync();
            await RecalculateBookingStatusAsync(bookingId);
        }

        private async Task RecalculateBookingStatusAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return;

            bool isComplete = booking.Passengers.Count == booking.NumberOfSeats &&
                              booking.Passengers.All(p => !string.IsNullOrWhiteSpace(p.FirstName) &&
                                                          !string.IsNullOrWhiteSpace(p.LastName) &&
                                                          !string.IsNullOrWhiteSpace(p.PassportNumber) &&
                                                          p.DateOfBirth.HasValue &&
                                                          !string.IsNullOrWhiteSpace(p.Gender) &&
                                                          p.PassportExpiryDate.HasValue &&
                                                          p.LastName != "(Account Holder)"); // placeholder check

            booking.PassengerDetailsStatus = isComplete ? "Complete" : "Incomplete";
            booking.PassengerDetailsCompletedAt = isComplete ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();
        }
    }
}



