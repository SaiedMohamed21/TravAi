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
        private readonly TravAi.Options.StripeOptions _stripeOptions;

        public BookingService(ApplicationDbContext context, Microsoft.Extensions.Options.IOptions<TravAi.Options.StripeOptions> stripeOptions)
        {
            _context = context;
            _stripeOptions = stripeOptions.Value;
        }

        public async Task<BookingResponseDto> BookFlightAsync(long userId, BookingRequestDto dto)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .FirstOrDefaultAsync(f => f.Id == dto.FlightId);

            if (flight == null || flight.Airline == null || 
                (flight.Airline.Status != "Approved" && flight.Airline.Status != "Active" && !flight.Airline.IsApproved) ||
                flight.Airline.Status == "Inactive" || flight.Airline.Status == "Disabled" || flight.Airline.Status == "Rejected" ||
                flight.Status == "Inactive")
                throw new Exception("Flight not found.");
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
                        DateOfBirth = companion.DateOfBirth,
                        PassportExpiryDate = companion.PassportExpireDate,
                        Gender = companion.Gender.ToString(),
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

                // Add empty placeholders for any remaining seats to ensure we always have NumberOfSeats passenger records
                while (booking.Passengers.Count < dto.NumberOfSeats)
                {
                    booking.Passengers.Add(new Passenger
                    {
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        AgeType = "Adult",
                        PassengerType = "Adult",
                        PassportNumber = null,
                        Nationality = null,
                        Price = flight.Price ?? 0,
                        Status = "Pending"
                    });
                }

                // Determine initial passenger details status
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
                .Where(b => b.UserId == userId && b.PaymentStatus == "Paid")
                .AsQueryable();

            if (tab == "upcoming")
                query = query.Where(b => b.Status != "Cancelled" && b.Flight != null && b.Flight.DepartureTime > DateTime.UtcNow);
            else if (tab == "past")
                query = query.Where(b => b.Status != "Cancelled" && b.Flight != null && b.Flight.DepartureTime <= DateTime.UtcNow);
            else if (tab == "cancelled")
                query = query.Where(b => b.Status == "Cancelled");

            var bookings = await query.ToListAsync();
            var userAirlineReviews = await _context.AirlineReviews
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return bookings.Select(b => MapToResponse(b, userAirlineReviews)).ToList();
        }

        private BookingResponseDto MapToResponse(Booking b)
        {
            return MapToResponse(b, null);
        }

        private BookingResponseDto MapToResponse(Booking b, List<TravAi.Airline.Models.Review>? userReviews)
        {
            var departureTime = b.Flight?.DepartureTime ?? b.BookingDate;
            var existingReview = userReviews?.FirstOrDefault(r => r.FlightId == b.FlightId);
            bool hasReviewed = existingReview != null;
            bool canReview = !string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) && departureTime <= DateTime.UtcNow && !hasReviewed;

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
                Route = b.Flight != null ? $"{b.Flight.DepartureAirportCode} → {b.Flight.ArrivalAirportCode}" : "TBD → TBD",
                UiBadge = b.Status == "Cancelled" ? "Cancelled" : (departureTime > DateTime.UtcNow ? "Upcoming" : "Completed"),
                CanCancel = b.Status != "Cancelled" && departureTime > DateTime.UtcNow.AddHours(24),
                CanReview = canReview,
                HasReviewed = hasReviewed,
                ReviewId = existingReview?.Id,
                ReviewRating = existingReview?.Rating,
                ReviewComment = existingReview?.Comment,
                BoardingTime = departureTime.AddMinutes(-45).ToString("HH:mm"),
                FlightClass = b.Flight?.FlightClass
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

        public async Task CancelAsync(long bookingId, string? refundMethod = null)
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
                    decimal refundPercentage = 0.0m; // Economy and Premium are non-refundable

                    if (booking.Flight?.FlightClass == "Business")
                    {
                        refundPercentage = 0.90m; // 90% refund, 10% fee
                    }

                    decimal refundAmount = booking.TotalPrice * refundPercentage;

                    var transactionItem = await _context.PaymentTransactionItems
                        .Include(pti => pti.PaymentTransaction)
                        .FirstOrDefaultAsync(pti => pti.BookingType == "Airline" && pti.BookingId == booking.Id && pti.Status == "Paid");

                    if (refundAmount > 0)
                    {
                        if (refundMethod == "OriginalPaymentMethod" && transactionItem != null && transactionItem.PaymentTransaction.Provider == "Stripe")
                        {
                            Stripe.StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
                            var refundService = new Stripe.RefundService();
                            await refundService.CreateAsync(new Stripe.RefundCreateOptions
                            {
                                PaymentIntent = transactionItem.PaymentTransaction.ProviderTransactionId,
                                Amount = (long)(refundAmount * 100),
                                Reason = "requested_by_customer"
                            });
                        }
                        else
                        {
                            booking.User.WalletBalance += refundAmount;
                            
                            _context.WalletTransactions.Add(new WalletTransaction
                            {
                                UserId = booking.User.Id,
                                Amount = refundAmount,
                                Type = "Refund",
                                Description = $"Refund for flight booking #{booking.Id}",
                                ReferenceId = $"Refund-Airline-{booking.Id}",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    if (transactionItem != null)
                    {
                        transactionItem.Status = "Refunded";
                    }
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
        public async Task<bool> IsAirlineBookingPassengerDetailsCompleteAsync(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return false;

            return booking.Passengers.Count == booking.NumberOfSeats &&
                   booking.Passengers.All(p => !string.IsNullOrWhiteSpace(p.FirstName) &&
                                               !string.IsNullOrWhiteSpace(p.LastName) &&
                                               !string.IsNullOrWhiteSpace(p.PassportNumber) &&
                                               p.DateOfBirth.HasValue &&
                                               !string.IsNullOrWhiteSpace(p.Gender) &&
                                               p.PassportExpiryDate.HasValue &&
                                               p.LastName != "(Account Holder)"); // placeholder check
        }

        public async Task<AirlineCancelPreviewDto> PreviewCancelAsync(long userId, long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight).ThenInclude(f => f.Airline)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new KeyNotFoundException("Flight booking not found.");
            
            if (booking.UserId != userId) 
                throw new UnauthorizedAccessException("You are not authorized to preview cancellation for this booking.");

            decimal refundPercentage = 0.0m;
            string flightClass = booking.Flight?.FlightClass ?? "Economy";
            
            if (flightClass == "Business")
            {
                refundPercentage = 0.90m;
            }

            decimal refundAmount = booking.TotalPrice * refundPercentage;
            decimal serviceFee = booking.TotalPrice - refundAmount;

            string policyDesc = flightClass == "Business" 
                ? "Business class bookings receive a 90% refund. A 10% service fee is deducted." 
                : $"{flightClass} class bookings are strictly non-refundable (0% refund).";

            var isStripePayment = await _context.PaymentTransactionItems
                .Include(pti => pti.PaymentTransaction)
                .AnyAsync(pti => pti.BookingType == "Airline" && pti.BookingId == booking.Id && pti.Status == "Paid" && pti.PaymentTransaction.Provider == "Stripe");

            return new AirlineCancelPreviewDto
            {
                BookingId = booking.Id,
                BookingType = "Airline",
                FlightName = $"{booking.Flight?.Airline?.Name ?? "Airline"} - {booking.Flight?.FlightNumber ?? "FL"}",
                FlightClass = flightClass,
                PaidAmount = booking.TotalPrice,
                ServiceFee = serviceFee,
                RefundAmount = refundAmount,
                RefundDestination = "Wallet",
                PolicyDescription = policyDesc,
                OriginalPaymentMethodAvailable = isStripePayment
            };
        }
    }
}
