using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TravAi.Data;
using TravAi.DTOs.Checkout;
using TravAi.Models;
using TravAi.Options;
using TravAi.Models.Enums;
using TourGuideBookingStatus = TravAi.TourGuide.Models.Enums.BookingStatus;
using TourGuidePaymentStatus = TravAi.TourGuide.Models.Enums.PaymentStatus;
using TravAi.Models.Hotels.Bookings;
using TravAi.TourGuide.Models;
using TravAi.Airline.Models;

namespace TravAi.Services
{
    public class CheckoutService : ICheckoutService
    {
        private const int AirlineExpirationMinutes = 10;
        private const int HotelTourExpirationMinutes = 60;

        private readonly ApplicationDbContext _context;
        private readonly StripeOptions _stripeOptions;
        private readonly ILogger<CheckoutService> _logger;

        public CheckoutService(
            ApplicationDbContext context,
            IOptions<StripeOptions> stripeOptions,
            ILogger<CheckoutService> logger)
        {
            _context = context;
            _stripeOptions = stripeOptions.Value;
            _logger = logger;

            // Set the global Stripe API key
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
        }

        public async Task<List<PendingCheckoutItemDto>> GetPendingBookingsAsync(long userId)
        {
            // Run expired unpaid cleanup first
            await ExpireAndDeleteUnpaidBookingsAsync(userId);

            var now = DateTime.UtcNow;
            var list = new List<PendingCheckoutItemDto>();

            // 1. Airline Bookings
            var airlineBookings = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.UserId == userId && 
                            b.PaymentStatus != "Paid" && 
                            b.Status != "Confirmed" &&
                            b.Status != "Cancelled" && 
                            b.Status != "Rejected")
                .ToListAsync();

            foreach (var b in airlineBookings)
            {
                var expiresAt = b.BookingDate.AddMinutes(AirlineExpirationMinutes);
                if (expiresAt > now)
                {
                    list.Add(new PendingCheckoutItemDto
                    {
                        Id = b.Id,
                        ItemType = "AirlineBooking",
                        DisplayName = b.Flight != null 
                            ? $"Flight {b.Flight.FlightNumber} ({b.Flight.DepartureAirportCode} -> {b.Flight.ArrivalAirportCode})"
                            : $"Flight Booking #{b.Id}",
                        Amount = b.TotalPrice,
                        Currency = "usd",
                        CreatedAt = b.BookingDate,
                        ExpiresAt = expiresAt,
                        ServerNowUtc = now,
                        RemainingSeconds = (expiresAt - now).TotalSeconds
                    });
                }
            }

            // 2. Hotel Bookings
            var hotelBookings = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Where(b => b.UserId == userId && 
                            b.PaymentStatus != PaymentStatus.Paid && 
                            b.Status != BookingStatus.Confirmed &&
                            b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            foreach (var b in hotelBookings)
            {
                var expiresAt = b.CreatedAt.AddMinutes(HotelTourExpirationMinutes);
                if (expiresAt > now)
                {
                    list.Add(new PendingCheckoutItemDto
                    {
                        Id = b.Id,
                        ItemType = "HotelBooking",
                        DisplayName = b.Hotel != null 
                            ? $"Hotel: {b.Hotel.HotelName}" 
                            : $"Hotel Booking #{b.Id}",
                        Amount = b.TotalPrice,
                        Currency = "usd",
                        CreatedAt = b.CreatedAt,
                        ExpiresAt = expiresAt,
                        ServerNowUtc = now,
                        RemainingSeconds = (expiresAt - now).TotalSeconds
                    });
                }
            }

            // 3. Tour Bookings
            var tourBookings = await _context.TourBookings
                .Include(b => b.Tour)
                .Where(b => b.UserId == userId && 
                            b.PaymentStatus != TourGuidePaymentStatus.Completed && 
                            b.Status != TourGuideBookingStatus.Confirmed &&
                            b.Status != TourGuideBookingStatus.Cancelled)
                .ToListAsync();

            foreach (var b in tourBookings)
            {
                var expiresAt = b.CreatedAt.AddMinutes(HotelTourExpirationMinutes);
                if (expiresAt > now)
                {
                    list.Add(new PendingCheckoutItemDto
                    {
                        Id = b.Id,
                        ItemType = "TourBooking",
                        DisplayName = b.Tour != null 
                            ? $"Tour: {b.Tour.TourTitle}" 
                            : $"Tour Booking #{b.Id}",
                        Amount = b.TotalPrice,
                        Currency = b.Currency?.ToLower() ?? "usd",
                        CreatedAt = b.CreatedAt,
                        ExpiresAt = expiresAt,
                        ServerNowUtc = now,
                        RemainingSeconds = (expiresAt - now).TotalSeconds
                    });
                }
            }

            return list.OrderBy(item => item.ExpiresAt).ToList();
        }

        public async Task<CheckoutResponse> CreateAirlineCheckoutAsync(CreateAirlineCheckoutRequest request, string baseUrl)
        {
            var now = DateTime.UtcNow;
            
            // Validate booking exists and belongs to user
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.Id == request.AirlineBookingId && b.UserId == request.UserId);

            if (booking == null)
                throw new KeyNotFoundException("Airline booking not found or doesn't belong to the user.");

            if (booking.PaymentStatus == "Paid")
                throw new InvalidOperationException("This booking has already been paid.");

            var expiresAt = booking.BookingDate.AddMinutes(AirlineExpirationMinutes);
            if (expiresAt < now)
            {
                await DeleteAirlineBookingInternalAsync(booking);
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("This flight reservation expired. Please book again.");
            }

            // Create Db Transaction
            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create CheckoutSession
                    var checkoutSession = new CheckoutSession
                    {
                        UserId = request.UserId,
                        CheckoutType = "Airline",
                        Status = "Pending",
                        TotalAmount = booking.TotalPrice,
                        Currency = _stripeOptions.Currency.ToLower(),
                        CreatedAt = now,
                        ExpiresAt = expiresAt
                    };

                    _context.CheckoutSessions.Add(checkoutSession);
                    await _context.SaveChangesAsync();

                    // Create CheckoutSessionItem
                    var displayName = booking.Flight != null
                        ? $"Flight {booking.Flight.FlightNumber} ({booking.Flight.DepartureAirportCode} -> {booking.Flight.ArrivalAirportCode})"
                        : $"Flight Booking #{booking.Id}";

                    var sessionItem = new CheckoutSessionItem
                    {
                        CheckoutSessionId = checkoutSession.Id,
                        ItemType = "AirlineBooking",
                        ReferenceId = booking.Id,
                        DisplayName = displayName,
                        Amount = booking.TotalPrice,
                        CreatedAt = now
                    };

                    _context.CheckoutSessionItems.Add(sessionItem);

                    // Create PaymentTransaction
                    var paymentTransaction = new PaymentTransaction
                    {
                        CheckoutSessionId = checkoutSession.Id,
                        Provider = "Stripe",
                        Amount = booking.TotalPrice,
                        Currency = _stripeOptions.Currency.ToLower(),
                        Status = "Pending",
                        CreatedAt = now
                    };

                    _context.PaymentTransactions.Add(paymentTransaction);
                    await _context.SaveChangesAsync();

                    // Create Stripe Checkout Session
                    var stripeSessionId = await CreateStripeSessionAsync(
                        checkoutSession.Id,
                        new List<CheckoutSessionItem> { sessionItem },
                        _stripeOptions.Currency.ToLower(),
                        baseUrl
                    );

                    // Save session ID and payment intent placeholder (will be set by webhook/api confirm)
                    checkoutSession.StripeCheckoutSessionId = stripeSessionId;
                    paymentTransaction.ProviderCheckoutSessionId = stripeSessionId;

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    // Retrieve redirect URL from Stripe
                    var service = new SessionService();
                    var stripeSession = await service.GetAsync(stripeSessionId);

                    return new CheckoutResponse
                    {
                        CheckoutUrl = stripeSession.Url,
                        CheckoutSessionId = stripeSession.Id
                    };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating airline checkout session");
                    throw;
                }
            }
        }

        public async Task<CheckoutResponse> CreateHotelTourCheckoutAsync(CreateHotelTourCheckoutRequest request, string baseUrl)
        {
            var now = DateTime.UtcNow;

            if ((request.HotelBookingIds == null || !request.HotelBookingIds.Any()) && 
                (request.TourBookingIds == null || !request.TourBookingIds.Any()))
            {
                throw new ArgumentException("Must select at least one hotel or tour booking.");
            }

            // Load all bookings
            var hotelBookings = new List<HotelBooking>();
            if (request.HotelBookingIds != null && request.HotelBookingIds.Any())
            {
                hotelBookings = await _context.HotelBookings
                    .Include(b => b.Hotel)
                    .Where(b => request.HotelBookingIds.Contains(b.Id) && b.UserId == request.UserId)
                    .ToListAsync();

                if (hotelBookings.Count != request.HotelBookingIds.Count)
                    throw new KeyNotFoundException("One or more hotel bookings were not found or do not belong to the user.");
            }

            var tourBookings = new List<TourBooking>();
            if (request.TourBookingIds != null && request.TourBookingIds.Any())
            {
                tourBookings = await _context.TourBookings
                    .Include(b => b.Tour)
                    .Where(b => request.TourBookingIds.Contains(b.Id) && b.UserId == request.UserId)
                    .ToListAsync();

                if (tourBookings.Count != request.TourBookingIds.Count)
                    throw new KeyNotFoundException("One or more tour bookings were not found or do not belong to the user.");
            }

            // Validations
            bool anyExpired = false;
            foreach (var h in hotelBookings)
            {
                if (h.PaymentStatus == PaymentStatus.Paid)
                    throw new InvalidOperationException($"Hotel booking #{h.Id} is already paid.");
                if (h.CreatedAt.AddMinutes(HotelTourExpirationMinutes) < now)
                {
                    anyExpired = true;
                    await DeleteHotelBookingInternalAsync(h);
                }
            }

            foreach (var t in tourBookings)
            {
                if (t.PaymentStatus == TourGuidePaymentStatus.Completed)
                    throw new InvalidOperationException($"Tour booking #{t.Id} is already paid.");
                if (t.CreatedAt.AddMinutes(HotelTourExpirationMinutes) < now)
                {
                    anyExpired = true;
                    await DeleteTourBookingInternalAsync(t);
                }
            }

            if (anyExpired)
            {
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("One or more selected reservations expired. Please book again.");
            }

            // Sum total amount
            decimal totalAmount = hotelBookings.Sum(h => h.TotalPrice) + tourBookings.Sum(t => t.TotalPrice);

            // Determine earliest expiry time
            DateTime expiresAt = DateTime.MaxValue;
            foreach (var h in hotelBookings)
            {
                var hExpiry = h.CreatedAt.AddMinutes(HotelTourExpirationMinutes);
                if (hExpiry < expiresAt) expiresAt = hExpiry;
            }
            foreach (var t in tourBookings)
            {
                var tExpiry = t.CreatedAt.AddMinutes(HotelTourExpirationMinutes);
                if (tExpiry < expiresAt) expiresAt = tExpiry;
            }

            // Default currency
            string currency = _stripeOptions.Currency.ToLower();
            if (tourBookings.Any())
            {
                currency = tourBookings.First().Currency?.ToLower() ?? currency;
            }

            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create CheckoutSession
                    var checkoutSession = new CheckoutSession
                    {
                        UserId = request.UserId,
                        CheckoutType = "HotelTour",
                        Status = "Pending",
                        TotalAmount = totalAmount,
                        Currency = currency,
                        CreatedAt = now,
                        ExpiresAt = expiresAt
                    };

                    _context.CheckoutSessions.Add(checkoutSession);
                    await _context.SaveChangesAsync();

                    var sessionItems = new List<CheckoutSessionItem>();

                    // Create items for Hotels
                    foreach (var h in hotelBookings)
                    {
                        var displayName = h.Hotel != null ? $"Hotel: {h.Hotel.HotelName}" : $"Hotel Booking #{h.Id}";
                        var item = new CheckoutSessionItem
                        {
                            CheckoutSessionId = checkoutSession.Id,
                            ItemType = "HotelBooking",
                            ReferenceId = h.Id,
                            DisplayName = displayName,
                            Amount = h.TotalPrice,
                            CreatedAt = now
                        };
                        _context.CheckoutSessionItems.Add(item);
                        sessionItems.Add(item);
                    }

                    // Create items for Tours
                    foreach (var t in tourBookings)
                    {
                        var displayName = t.Tour != null ? $"Tour: {t.Tour.TourTitle}" : $"Tour Booking #{t.Id}";
                        var item = new CheckoutSessionItem
                        {
                            CheckoutSessionId = checkoutSession.Id,
                            ItemType = "TourBooking",
                            ReferenceId = t.Id,
                            DisplayName = displayName,
                            Amount = t.TotalPrice,
                            CreatedAt = now
                        };
                        _context.CheckoutSessionItems.Add(item);
                        sessionItems.Add(item);
                    }

                    // Create PaymentTransaction
                    var paymentTransaction = new PaymentTransaction
                    {
                        CheckoutSessionId = checkoutSession.Id,
                        Provider = "Stripe",
                        Amount = totalAmount,
                        Currency = currency,
                        Status = "Pending",
                        CreatedAt = now
                    };

                    _context.PaymentTransactions.Add(paymentTransaction);
                    await _context.SaveChangesAsync();

                    // Create Stripe session
                    var stripeSessionId = await CreateStripeSessionAsync(
                        checkoutSession.Id,
                        sessionItems,
                        currency,
                        baseUrl
                    );

                    checkoutSession.StripeCheckoutSessionId = stripeSessionId;
                    paymentTransaction.ProviderCheckoutSessionId = stripeSessionId;

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    var service = new SessionService();
                    var stripeSession = await service.GetAsync(stripeSessionId);

                    return new CheckoutResponse
                    {
                        CheckoutUrl = stripeSession.Url,
                        CheckoutSessionId = stripeSession.Id
                    };
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating hotel/tour checkout session");
                    throw;
                }
            }
        }

        public async Task<object> GetCheckoutSessionDetailsAsync(long id)
        {
            var session = await _context.CheckoutSessions
                .Include(s => s.Items)
                .Include(s => s.Transactions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                // Try searching by Stripe Checkout Session ID instead if long parse failed or not found
                return null;
            }

            var transaction = session.Transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

            return new
            {
                checkoutSessionId = session.Id,
                status = session.Status,
                totalAmount = session.TotalAmount,
                currency = session.Currency,
                stripeCheckoutSessionId = session.StripeCheckoutSessionId,
                items = session.Items.Select(i => new {
                    i.ItemType,
                    i.ReferenceId,
                    i.DisplayName,
                    i.Amount
                }).ToList(),
                paymentTransactionId = transaction?.Id,
                providerTransactionId = transaction?.ProviderTransactionId,
                paidAt = session.PaidAt
            };
        }

        public async Task<object> ConfirmPaymentAsync(string stripeSessionId)
        {
            var service = new SessionService();
            var stripeSession = await service.GetAsync(stripeSessionId);

            if (stripeSession == null)
                throw new KeyNotFoundException("Stripe Checkout Session not found.");

            if (stripeSession.PaymentStatus?.ToLower() == "paid")
            {
                var paymentIntentId = stripeSession.PaymentIntentId;
                var rawJson = JsonSerializer.Serialize(stripeSession);
                
                var (session, tx) = await CompleteCheckoutSessionAsync(stripeSessionId, paymentIntentId, rawJson);

                return new
                {
                    success = true,
                    checkoutSessionId = session.Id,
                    paymentTransactionId = tx.Id,
                    amount = session.TotalAmount,
                    status = session.Status,
                    paidAt = session.PaidAt
                };
            }

            throw new InvalidOperationException($"Stripe session is not paid. Status: {stripeSession.PaymentStatus}");
        }

        public async Task<bool> HandleStripeWebhookAsync(string jsonPayload, string stripeSignature)
        {
            Event stripeEvent;
            try
            {
                // Attempt to verify signature if webhook secret is configured and signature exists
                if (!string.IsNullOrEmpty(_stripeOptions.WebhookSecret) && !string.IsNullOrEmpty(stripeSignature))
                {
                    stripeEvent = EventUtility.ConstructEvent(jsonPayload, stripeSignature, _stripeOptions.WebhookSecret);
                }
                else
                {
                    stripeEvent = EventUtility.ParseEvent(jsonPayload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed. Processing raw payload directly.");
                stripeEvent = EventUtility.ParseEvent(jsonPayload);
            }

            _logger.LogInformation("Processing Stripe Webhook Event: {EventType}", stripeEvent.Type);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    var rawJson = JsonSerializer.Serialize(stripeEvent);
                    await CompleteCheckoutSessionAsync(session.Id, session.PaymentIntentId, rawJson);
                    return true;
                }
            }
            else if (stripeEvent.Type == "checkout.session.expired")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    var rawJson = JsonSerializer.Serialize(stripeEvent);
                    await ExpireCheckoutSessionAsync(session.Id, rawJson);
                    return true;
                }
            }

            return false;
        }

        // --- Helper Methods ---

        private async Task<string> CreateStripeSessionAsync(
            long checkoutSessionId,
            List<CheckoutSessionItem> items,
            string currency,
            string baseUrl)
        {
            var lineItems = new List<SessionLineItemOptions>();

            foreach (var item in items)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Amount * 100), // Cents
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.DisplayName ?? $"{item.ItemType} Booking"
                        }
                    },
                    Quantity = 1
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                ClientReferenceId = checkoutSessionId.ToString(),
                SuccessUrl = $"{baseUrl}/checkout-success.html?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{baseUrl}/checkout-cancel.html"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session.Id;
        }

        private async Task<(CheckoutSession, PaymentTransaction)> CompleteCheckoutSessionAsync(
            string stripeSessionId,
            string paymentIntentId,
            string rawResponse)
        {
            var session = await _context.CheckoutSessions
                .Include(s => s.Items)
                .Include(s => s.Transactions)
                .FirstOrDefaultAsync(s => s.StripeCheckoutSessionId == stripeSessionId);

            if (session == null)
            {
                // If not found by Stripe Session ID directly, check if we stored it in metadata or try checking by ClientReferenceId
                var service = new SessionService();
                var stripeSession = await service.GetAsync(stripeSessionId);
                if (stripeSession != null && long.TryParse(stripeSession.ClientReferenceId, out long dbId))
                {
                    session = await _context.CheckoutSessions
                        .Include(s => s.Items)
                        .Include(s => s.Transactions)
                        .FirstOrDefaultAsync(s => s.Id == dbId);
                }
            }

            if (session == null)
                throw new KeyNotFoundException($"CheckoutSession with Stripe Session ID {stripeSessionId} not found in database.");

            var transaction = session.Transactions.FirstOrDefault(t => t.ProviderCheckoutSessionId == stripeSessionId)
                ?? session.Transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

            if (transaction == null)
            {
                transaction = new PaymentTransaction
                {
                    CheckoutSessionId = session.Id,
                    Provider = "Stripe",
                    Amount = session.TotalAmount,
                    Currency = session.Currency,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.PaymentTransactions.Add(transaction);
            }

            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Update Checkout Session status
                    if (session.Status != "Paid")
                    {
                        session.Status = "Paid";
                        session.PaidAt = DateTime.UtcNow;
                        session.StripePaymentIntentId = paymentIntentId;
                    }

                    // 2. Update Payment Transaction status
                    if (transaction.Status != "Paid")
                    {
                        transaction.Status = "Paid";
                        transaction.PaidAt = DateTime.UtcNow;
                        transaction.ProviderTransactionId = paymentIntentId;
                        transaction.ProviderCheckoutSessionId = stripeSessionId;
                        transaction.RawProviderResponse = rawResponse;
                    }

                    // 3. Update Bookings statuses
                    foreach (var item in session.Items)
                    {
                        if (item.ItemType == "AirlineBooking")
                        {
                            var b = await _context.Bookings.FindAsync(item.ReferenceId);
                            if (b != null)
                            {
                                b.PaymentStatus = "Paid";
                                b.Status = "Confirmed";
                            }
                        }
                        else if (item.ItemType == "HotelBooking")
                        {
                            var h = await _context.HotelBookings.FindAsync(item.ReferenceId);
                            if (h != null)
                            {
                                h.PaymentStatus = PaymentStatus.Paid;
                                h.Status = BookingStatus.Confirmed;
                                h.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else if (item.ItemType == "TourBooking")
                        {
                            var t = await _context.TourBookings.FindAsync(item.ReferenceId);
                            if (t != null)
                            {
                                t.PaymentStatus = TourGuidePaymentStatus.Completed;
                                t.Status = TourGuideBookingStatus.Confirmed;
                                t.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }

                    // 4. Save Webhook Event log to DB
                    var webhookEvent = new StripeWebhookEvent
                    {
                        StripeEventId = Guid.NewGuid().ToString(), // Event ID generated/saved
                        EventType = "checkout.session.completed",
                        CheckoutSessionId = session.Id,
                        PaymentTransactionId = transaction.Id,
                        RawJson = rawResponse,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StripeWebhookEvents.Add(webhookEvent);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error completing checkout session in DB transaction");
                    throw;
                }
            }

            return (session, transaction);
        }

        private async Task ExpireCheckoutSessionAsync(string stripeSessionId, string rawResponse)
        {
            var session = await _context.CheckoutSessions
                .Include(s => s.Items)
                .Include(s => s.Transactions)
                .FirstOrDefaultAsync(s => s.StripeCheckoutSessionId == stripeSessionId);

            if (session == null)
            {
                var service = new SessionService();
                var stripeSession = await service.GetAsync(stripeSessionId);
                if (stripeSession != null && long.TryParse(stripeSession.ClientReferenceId, out long dbId))
                {
                    session = await _context.CheckoutSessions
                        .Include(s => s.Items)
                        .Include(s => s.Transactions)
                        .FirstOrDefaultAsync(s => s.Id == dbId);
                }
            }

            if (session == null)
                return; // Nothing to expire in DB

            var transaction = session.Transactions.FirstOrDefault(t => t.ProviderCheckoutSessionId == stripeSessionId)
                ?? session.Transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

            using (var dbTransaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (session.Status == "Pending")
                    {
                        session.Status = "Expired";
                    }

                    if (transaction != null && transaction.Status == "Pending")
                    {
                        transaction.Status = "Expired";
                        transaction.RawProviderResponse = rawResponse;
                    }

                    // For each item still unpaid, update status to Expired or Cancelled
                    foreach (var item in session.Items)
                    {
                        if (item.ItemType == "AirlineBooking")
                        {
                            var b = await _context.Bookings.FindAsync(item.ReferenceId);
                            if (b != null && b.PaymentStatus != "Paid")
                            {
                                b.Status = "Expired";
                            }
                        }
                        else if (item.ItemType == "HotelBooking")
                        {
                            var h = await _context.HotelBookings.FindAsync(item.ReferenceId);
                            if (h != null && h.PaymentStatus != PaymentStatus.Paid)
                            {
                                h.Status = BookingStatus.Cancelled; // Closest existing enum/status for cancelled/expired
                                h.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else if (item.ItemType == "TourBooking")
                        {
                            var t = await _context.TourBookings.FindAsync(item.ReferenceId);
                            if (t != null && t.PaymentStatus != TourGuidePaymentStatus.Completed)
                            {
                                t.Status = TourGuideBookingStatus.Cancelled; // Closest existing enum/status for cancelled/expired
                                t.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }

                    var webhookEvent = new StripeWebhookEvent
                    {
                        StripeEventId = Guid.NewGuid().ToString(),
                        EventType = "checkout.session.expired",
                        CheckoutSessionId = session.Id,
                        PaymentTransactionId = transaction?.Id,
                        RawJson = rawResponse,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StripeWebhookEvents.Add(webhookEvent);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error expiring checkout session in DB transaction");
                    throw;
                }
            }
        }

        public async Task ExpireAndDeleteUnpaidBookingsAsync(long userId)
        {
            var now = DateTime.UtcNow;

            // 1. Airline Bookings
            var expiredAirlines = await _context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.UserId == userId &&
                            b.PaymentStatus != "Paid" &&
                            b.Status != "Confirmed" &&
                            b.BookingDate.AddMinutes(AirlineExpirationMinutes) < now)
                .ToListAsync();

            foreach (var b in expiredAirlines)
            {
                await DeleteAirlineBookingInternalAsync(b);
            }

            // 2. Hotel Bookings
            var expiredHotels = await _context.HotelBookings
                .Where(b => b.UserId == userId &&
                            b.PaymentStatus != PaymentStatus.Paid &&
                            b.Status != BookingStatus.Confirmed &&
                            b.CreatedAt.AddMinutes(HotelTourExpirationMinutes) < now)
                .ToListAsync();

            foreach (var h in expiredHotels)
            {
                await DeleteHotelBookingInternalAsync(h);
            }

            // 3. Tour Bookings
            var expiredTours = await _context.TourBookings
                .Where(b => b.UserId == userId &&
                            b.PaymentStatus != TourGuidePaymentStatus.Completed &&
                            b.Status != TourGuideBookingStatus.Confirmed &&
                            b.CreatedAt.AddMinutes(HotelTourExpirationMinutes) < now)
                .ToListAsync();

            foreach (var t in expiredTours)
            {
                await DeleteTourBookingInternalAsync(t);
            }

            await _context.SaveChangesAsync();
        }

        private async Task DeleteAirlineBookingInternalAsync(Booking booking)
        {
            if (booking.Flight != null)
            {
                booking.Flight.AvailableSeats = (booking.Flight.AvailableSeats ?? 0) + booking.NumberOfSeats;
            }

            var passengers = await _context.Passengers
                .Include(p => p.Phones)
                .Include(p => p.EmergencyContacts)
                .Where(p => p.BookingId == booking.Id)
                .ToListAsync();

            foreach (var p in passengers)
            {
                _context.PassengerPhones.RemoveRange(p.Phones);
                _context.PassengerEmergencyContacts.RemoveRange(p.EmergencyContacts);
            }

            _context.Passengers.RemoveRange(passengers);
            _context.Bookings.Remove(booking);
        }

        private async Task DeleteHotelBookingInternalAsync(HotelBooking hotelBooking)
        {
            var rooms = await _context.HotelBookingRooms
                .Where(br => br.BookingId == hotelBooking.Id)
                .ToListAsync();

            var payments = await _context.HotelPayments
                .Where(p => p.BookingId == hotelBooking.Id)
                .ToListAsync();

            _context.HotelBookingRooms.RemoveRange(rooms);
            _context.HotelPayments.RemoveRange(payments);
            _context.HotelBookings.Remove(hotelBooking);
        }

        private async Task DeleteTourBookingInternalAsync(TourBooking tourBooking)
        {
            var participants = await _context.TourBookingParticipants
                .Include(p => p.Phones)
                .Include(p => p.EmergencyNumbers)
                .Where(p => p.BookingId == tourBooking.Id)
                .ToListAsync();

            var payments = await _context.TourBookingPayments
                .Where(p => p.BookingId == tourBooking.Id)
                .ToListAsync();

            foreach (var p in participants)
            {
                _context.TourParticipantPhones.RemoveRange(p.Phones);
                _context.TourParticipantEmergencyNumbers.RemoveRange(p.EmergencyNumbers);
            }

            _context.TourBookingParticipants.RemoveRange(participants);
            _context.TourBookingPayments.RemoveRange(payments);
            _context.TourBookings.Remove(tourBooking);
        }
    }
}
