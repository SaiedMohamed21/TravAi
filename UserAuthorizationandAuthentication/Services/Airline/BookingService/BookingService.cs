using UserAuthorizationandAuthentication;
using Microsoft.EntityFrameworkCore;

using UserAuthorizationandAuthentication.Airline.DTOs.Booking;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Airline.Models;

namespace UserAuthorizationandAuthentication.Airline.Services.BookingService
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
            if (flight.AvailableSeats < dto.NumberOfSeats)
                throw new Exception("Not enough seats available.");

            var companions = await _context.UserCompanions
                .Where(c => dto.CompanionIds.Contains(c.Id))
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            decimal totalCost = flight.Price * dto.NumberOfSeats;
            if (user.WalletBalance < totalCost)
            {
                throw new Exception("Insufficient funds in your wallet to complete this booking.");
            }

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
                    Status = "Confirmed",
                    PaymentStatus = "Paid"
                };

            // 3. Add Passengers from Companions
            foreach (var companion in companions)
            {
                booking.Passengers.Add(new Passenger
                {
                    FirstName = companion.FirstName,
                    LastName = companion.LastName,
                    AgeType = companion.AgeType,
                    PassportNumber = companion.PassportNumber,
                    Nationality = companion.Nationality,
                    ProfilePic = companion.ProfilePic,
                    PassportImage = companion.PassportImage,
                    Price = flight.Price,
                    Status = "Confirmed"
                });
            }

            // 4. Handle Case: User booking for themselves (No companions or fewer than seats)
            if (booking.Passengers.Count < dto.NumberOfSeats)
            {
                if (user != null)
                {
                    var mainPhone = await _context.UserPhones.FirstOrDefaultAsync(p => p.UserId == userId);
                    var mainPassenger = new Passenger
                    {
                        FirstName = user.Name,
                        LastName = "(Account Holder)",
                        AgeType = "Adult",
                        PassportNumber = user.PassportNumber,
                        Nationality = user.Nationality,
                        ProfilePic = user.ProfilePic,
                        PassportImage = user.PassportImage,
                        Price = flight.Price,
                        Status = "Confirmed"
                    };

                    if (mainPhone != null)
                    {
                        mainPassenger.Phones.Add(new PassengerPhone { PhoneNumber = mainPhone.PhoneNumber });
                    }

                    booking.Passengers.Add(mainPassenger);
                }
            }

                // Deduct seats
                flight.AvailableSeats -= dto.NumberOfSeats;

                // Deduct from wallet
                user.WalletBalance -= totalCost;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Add wallet transaction
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = user.Id,
                    Amount = -totalCost,
                    Type = "Booking Deduction",
                    Description = $"Payment for Booking Flight {flight.DepartureAirportCode} to {flight.ArrivalAirportCode}",
                    ReferenceId = booking.Id.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

            return new BookingResponseDto
            {
                Id = booking.Id,
                UserName = user?.Name,
                FlightId = flight.Id,
                AirlineName = flight.Airline.Name,
                FromCode = flight.DepartureAirportCode,
                ToCode = flight.ArrivalAirportCode,
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                NumberOfSeats = booking.NumberOfSeats,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                BookingDate = booking.BookingDate,
                Passengers = booking.Passengers.Select(p => new UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto
                {
                    Id = p.Id,
                    BookingId = p.BookingId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PassengerType = p.PassengerType,
                    AgeType = p.AgeType,
                    PassportNumber = p.PassportNumber,
                    Nationality = p.Nationality,
                    Price = p.Price,
                    Status = p.Status,
                    RejectionReason = p.RejectionReason,
                    ProfilePic = p.ProfilePic,
                    PassportImage = p.PassportImage,
                    PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList()
                }).ToList()
            };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<BookingResponseDto>> GetUserBookingsAsync(long userId)
        {
            return await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                    .ThenInclude(p => p.Phones)
                .Where(b => b.UserId == userId)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    UserName = b.User.Name,
                    FlightId = b.Flight.Id,
                    AirlineName = b.Flight.Airline.Name,
                    FromCode = b.Flight.DepartureAirportCode,
                    ToCode = b.Flight.ArrivalAirportCode,
                    DepartureTime = b.Flight.DepartureTime,
                    ArrivalTime = b.Flight.ArrivalTime,
                    NumberOfSeats = b.NumberOfSeats,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason,
                    PaymentStatus = b.PaymentStatus,
                    BookingDate = b.BookingDate,
                    Passengers = b.Passengers.Select(p => new UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto
                    {
                        Id = p.Id,
                        BookingId = p.BookingId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        PassengerType = p.PassengerType,
                        AgeType = p.AgeType,
                        PassportNumber = p.PassportNumber,
                        Nationality = p.Nationality,
                        Price = p.Price,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        ProfilePic = p.ProfilePic,
                        PassportImage = p.PassportImage,
                        PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                    .ThenInclude(p => p.Phones)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    UserName = b.User.Name,
                    FlightId = b.Flight.Id,
                    AirlineName = b.Flight.Airline.Name,
                    FromCode = b.Flight.DepartureAirportCode,
                    ToCode = b.Flight.ArrivalAirportCode,
                    DepartureTime = b.Flight.DepartureTime,
                    ArrivalTime = b.Flight.ArrivalTime,
                    NumberOfSeats = b.NumberOfSeats,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason,
                    PaymentStatus = b.PaymentStatus,
                    BookingDate = b.BookingDate,
                    Passengers = b.Passengers.Select(p => new UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto
                    {
                        Id = p.Id,
                        BookingId = p.BookingId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        PassengerType = p.PassengerType,
                        AgeType = p.AgeType,
                        PassportNumber = p.PassportNumber,
                        Nationality = p.Nationality,
                        Price = p.Price,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<List<BookingResponseDto>> GetFlightBookingsAsync(long flightId)
        {
             return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                    .ThenInclude(p => p.Phones)
                .Where(b => b.FlightId == flightId)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    UserName = b.User.Name,
                    FlightId = b.Flight.Id,
                    AirlineName = b.Flight.Airline.Name,
                    FromCode = b.Flight.DepartureAirportCode,
                    ToCode = b.Flight.ArrivalAirportCode,
                    DepartureTime = b.Flight.DepartureTime,
                    ArrivalTime = b.Flight.ArrivalTime,
                    NumberOfSeats = b.NumberOfSeats,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason,
                    PaymentStatus = b.PaymentStatus,
                    BookingDate = b.BookingDate,
                    Passengers = b.Passengers.Select(p => new UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto
                    {
                        Id = p.Id,
                        BookingId = p.BookingId,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        PassengerType = p.PassengerType,
                        AgeType = p.AgeType,
                        PassportNumber = p.PassportNumber,
                        Nationality = p.Nationality,
                        Price = p.Price,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<BookingResponseDto?> GetByIdAsync(long bookingId)
        {
            var b = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                    .ThenInclude(p => p.Phones)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (b == null) return null;

            return new BookingResponseDto
            {
                Id = b.Id,
                UserName = b.User?.Name,
                FlightId = b.Flight.Id,
                AirlineName = b.Flight.Airline.Name,
                FromCode = b.Flight.DepartureAirportCode,
                ToCode = b.Flight.ArrivalAirportCode,
                DepartureTime = b.Flight.DepartureTime,
                ArrivalTime = b.Flight.ArrivalTime,
                NumberOfSeats = b.NumberOfSeats,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                RejectionReason = b.RejectionReason,
                PaymentStatus = b.PaymentStatus,
                BookingDate = b.BookingDate,
                Passengers = b.Passengers.Select(p => new UserAuthorizationandAuthentication.Airline.DTOs.Passenger.PassengerResponseDto
                {
                    Id = p.Id,
                    BookingId = p.BookingId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    PassengerType = p.PassengerType,
                    AgeType = p.AgeType,
                    PassportNumber = p.PassportNumber,
                    Nationality = p.Nationality,
                    Price = p.Price,
                    Status = p.Status,
                    RejectionReason = p.RejectionReason,
                    PhoneNumbers = p.Phones.Select(ph => ph.PhoneNumber).ToList()
                }).ToList()
            };
        }

        public async Task CancelAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                throw new Exception("Booking not found.");

            if (booking.Status == "Cancelled")
                throw new Exception("Booking is already cancelled.");

            booking.Status = "Cancelled";
            
            // Return seats to flight
            booking.Flight.AvailableSeats += booking.NumberOfSeats;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateBookingStatusAsync(long bookingId, string status, string? reason = null)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                    .ThenInclude(p => p.Phones)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new Exception("Booking not found.");

            booking.Status = status; // Approved, Rejected, Cancelled
            booking.RejectionReason = reason;

            // Cascade approval to all passengers
            foreach (var passenger in booking.Passengers)
            {
                passenger.Status = status;
                passenger.RejectionReason = reason;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdatePassengerStatusAsync(long passengerId, string status, string? reason = null)
        {
            var passenger = await _context.Passengers.FindAsync(passengerId);
            if (passenger == null) throw new Exception("Passenger not found.");

            passenger.Status = status; // Approved, Rejected
            passenger.RejectionReason = reason;

            await _context.SaveChangesAsync();
        }

        public async Task<ETicketDto> GetETicketAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Status == "Confirmed");

            if (booking == null) throw new Exception("Confirmed booking not found.");

            var eticket = new ETicketDto
            {
                BookingId = booking.Id,
                Pnr = $"PNR-{booking.Id.ToString().PadLeft(6, '0')}",
                AirlineName = booking.Flight.Airline.Name,
                DepartureAirport = booking.Flight.DepartureAirportCode,
                ArrivalAirport = booking.Flight.ArrivalAirportCode,
                DepartureTime = booking.Flight.DepartureTime,
                ArrivalTime = booking.Flight.ArrivalTime,
                BookingDate = booking.BookingDate
            };

            // using var qrGenerator = new QRCoder.QRCodeGenerator();

            foreach (var passenger in booking.Passengers)
            {
                /* 
                string payload = $"PNR: {eticket.Pnr}\nName: {passenger.FirstName} {passenger.LastName}\nFlight: {eticket.DepartureAirport} -> {eticket.ArrivalAirport}\nDeparts: {eticket.DepartureTime}";
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                byte[] qrCodeImage = qrCode.GetGraphic(5);
                string base64Qr = $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
                */

                eticket.Passengers.Add(new PassengerTicketDto
                {
                    Name = $"{passenger.FirstName} {passenger.LastName}",
                    Type = passenger.AgeType,
                    QrCodeBase64 = "disabled-qrcoder-temporarily",
                    Status = passenger.Status
                });
            }

            return eticket;
        }
    }
}




