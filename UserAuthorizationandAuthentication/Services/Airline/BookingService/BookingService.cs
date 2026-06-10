using TravAi.Data;
using Microsoft.EntityFrameworkCore;
using TravAi.Airline.DTOs.Booking;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Airline.Models;
using TravAi.Airline.Models.Airlines;

namespace TravAi.Airline.Services.BookingService
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingResponseDto> BookFlightAsync(long userId, BookingRequestDto dto)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .FirstOrDefaultAsync(f => f.Id == dto.FlightId);

            if (flight == null) throw new Exception("Flight not found.");
            if ((flight.AvailableSeats ?? 0) < dto.NumberOfSeats)
                throw new Exception("Not enough seats available.");

            var companions = await _context.UserCompanions
                .Where(c => dto.CompanionIds.Contains(c.Id))
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            decimal totalCost = (flight.Price ?? 0) * dto.NumberOfSeats;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = new Booking
                {
                    UserId = userId,
                    FlightId = flight.Id,
                    NumberOfSeats = dto.NumberOfSeats,
                    TotalPrice = totalCost,
                    BookingDate = DateTime.UtcNow,
                    Status = "Pending",
                    PaymentStatus = "Pending"
                };

                foreach (var companion in companions)
                {
                    booking.Passengers.Add(new Passenger
                    {
                        FirstName = companion.FirstName,
                        LastName = companion.LastName,
                        AgeType = companion.AgeType,
                        PassportNumber = companion.PassportNumber,
                        Nationality = companion.Nationality,
                        Price = flight.Price ?? 0,
                        Status = "Pending"
                    });
                }

                if (booking.Passengers.Count < dto.NumberOfSeats)
                {
                    booking.Passengers.Add(new Passenger
                    {
                        FirstName = user.Name,
                        LastName = "(Account Holder)",
                        AgeType = "Adult",
                        PassportNumber = user.PassportNumber,
                        Nationality = user.Nationality,
                        Price = flight.Price ?? 0,
                        Status = "Pending"
                    });
                }

                flight.AvailableSeats = (flight.AvailableSeats ?? 0) - dto.NumberOfSeats;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToResponse(booking);
            }
            catch (Exception) { await transaction.RollbackAsync(); throw; }
        }

        public async Task<List<BookingResponseDto>> GetUserBookingsAsync(long userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return bookings.Select(MapToResponse).ToList();
        }

        public async Task<List<BookingResponseDto>> GetUserTripsAsync(long userId, string tab)
        {
            var query = _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (tab == "upcoming")
                query = query.Where(b => b.Status != "Cancelled" && b.Flight != null && b.Flight.DepartureTime > DateTime.UtcNow);
            else if (tab == "past")
                query = query.Where(b => b.Status != "Cancelled" && b.Flight != null && b.Flight.DepartureTime <= DateTime.UtcNow);
            else if (tab == "cancelled")
                query = query.Where(b => b.Status == "Cancelled");

            var bookings = await query.ToListAsync();
            return bookings.Select(MapToResponse).ToList();
        }

        private BookingResponseDto MapToResponse(Booking b)
        {
            var departureTime = b.Flight?.DepartureTime ?? DateTime.Now;
            return new BookingResponseDto
            {
                Id = b.Id,
                UserName = b.User?.Name ?? "User",
                FlightId = b.FlightId,
                AirlineName = b.Flight?.Airline?.Name ?? "Unknown",
                FromCode = b.Flight?.DepartureAirportCode ?? "TBD",
                ToCode = b.Flight?.ArrivalAirportCode ?? "TBD",
                DepartureTime = departureTime,
                ArrivalTime = b.Flight?.ArrivalTime ?? DateTime.Now,
                NumberOfSeats = b.NumberOfSeats,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                BookingDate = b.BookingDate,
                FlightNumber = b.Flight?.FlightNumber ?? $"FL-{b.FlightId}",
                RouteTitle = $"{b.Flight?.DepartureAirportCode} to {b.Flight?.ArrivalAirportCode}",
                UiBadge = b.Status == "Cancelled" ? "Cancelled" : (departureTime > DateTime.UtcNow ? "Upcoming" : "Completed"),
                CanCancel = b.Status != "Cancelled" && departureTime > DateTime.UtcNow.AddHours(24),
                CanReview = b.Status == "Completed" || (b.Status == "Confirmed" && departureTime <= DateTime.UtcNow),
                BoardingTime = departureTime.AddMinutes(-45).ToString("HH:mm")
            };
        }

        public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Include(b => b.User)
                .ToListAsync();

            return bookings.Select(MapToResponse).ToList();
        }

        public async Task<List<BookingResponseDto>> GetFlightBookingsAsync(long flightId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Include(b => b.User)
                .Where(b => b.FlightId == flightId)
                .ToListAsync();

            return bookings.Select(MapToResponse).ToList();
        }

        public async Task<BookingResponseDto?> GetByIdAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            return booking != null ? MapToResponse(booking) : null;
        }

        public async Task CancelAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new Exception("Booking not found.");
            if (booking.Status == "Cancelled") return;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.Status = "Cancelled";
                
                // Refund seats
                if (booking.Flight != null)
                {
                    booking.Flight.AvailableSeats = (booking.Flight.AvailableSeats ?? 0) + booking.NumberOfSeats;
                }

                // Refund money to wallet
                if (booking.User != null)
                {
                    booking.User.WalletBalance += booking.TotalPrice;
                    
                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = booking.User.Id,
                        Amount = booking.TotalPrice,
                        Type = "Booking Refund",
                        Description = $"Refund for flight booking #{booking.Id}",
                        ReferenceId = booking.Id.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateBookingStatusAsync(long bookingId, string status, string? reason = null)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new Exception("Booking not found.");

            booking.Status = status;
            booking.RejectionReason = reason;
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePassengerStatusAsync(long passengerId, string status, string? reason = null)
        {
            var passenger = await _context.Passengers.FindAsync(passengerId);
            if (passenger == null) throw new Exception("Passenger not found.");

            passenger.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task<ETicketDto> GetETicketAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new Exception("Booking not found.");

            return new ETicketDto
            {
                BookingId = booking.Id,
                Pnr = $"AIR-{booking.Id:D6}", // Simulated PNR
                AirlineName = booking.Flight?.Airline?.Name ?? "N/A",
                DepartureAirport = booking.Flight?.DepartureAirportCode ?? "Unknown",
                ArrivalAirport = booking.Flight?.ArrivalAirportCode ?? "Unknown",
                DepartureTime = booking.Flight?.DepartureTime ?? DateTime.Now,
                ArrivalTime = booking.Flight?.ArrivalTime ?? DateTime.Now,
                BookingDate = booking.BookingDate,
                Passengers = booking.Passengers.Select(p => new PassengerTicketDto
                {
                    Name = $"{p.FirstName} {p.LastName}",
                    Type = p.AgeType ?? "Adult",
                    Status = p.Status ?? "Confirmed",
                    QrCodeBase64 = "" // Placeholder for UI
                }).ToList()
            };
        }
    }
}
