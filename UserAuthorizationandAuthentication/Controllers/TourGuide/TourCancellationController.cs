using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using TravAi.Data;
using TravAi.Models;
using TravAi.TourGuide.Models;
using TravAi.TourGuide.Models.Enums;
using TravAi.Services.Common;

namespace TravAi.Controllers.TourGuide
{
    public class UserCancellationDecisionDto
    {
        public long BookingId { get; set; }
        public string Decision { get; set; } = null!; // "Refund" or "Alternative"
        public long? AlternativeTourId { get; set; } 
    }

    public class ChooseAlternativeDto
    {
        public long AlternativeTourId { get; set; }
        public string? PaymentMethod { get; set; } // "Wallet" or "Stripe" for difference payment
    }

    public class FinalizeAlternativeStripeDto
    {
        public string SessionId { get; set; } = null!;
    }

    public class TourCancellationRefundDto
    {
        public string? RefundMethod { get; set; } // "Wallet" or "OriginalPaymentMethod"
    }

    public class TourCancellationRefundPreviewDto
    {
        public long BookingId { get; set; }
        public string TourName { get; set; } = null!;
        public decimal RefundAmount { get; set; }
        public List<string> AvailableRefundMethods { get; set; } = new();
        public bool OriginalPaymentMethodAvailable { get; set; }
        public string? OriginalPaymentUnavailableReason { get; set; }
    }

    [Route("api/tourguide/cancellation")]
    [ApiController]
    [Authorize]
    public class TourCancellationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly TravAi.Options.StripeOptions _stripeOptions;

        public TourCancellationController(
            ApplicationDbContext context, 
            IWalletService walletService,
            Microsoft.Extensions.Options.IOptions<TravAi.Options.StripeOptions> stripeOptions)
        {
            _context = context;
            _walletService = walletService;
            _stripeOptions = stripeOptions.Value;
        }

        [HttpPost("decision")]
        public async Task<IActionResult> SubmitDecision([FromBody] UserCancellationDecisionDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.UserId == userId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Status != BookingStatus.PendingUserDecision)
                return BadRequest("Booking is not pending a decision.");

            var resolution = new TourBookingResolution
            {
                OriginalBookingId = booking.Id,
                UserId = userId,
                ResolutionType = dto.Decision,
                ResolvedAt = DateTime.UtcNow
            };

            if (dto.Decision == "Refund")
            {
                // Refund Idempotency Check
                var existingResolution = await _context.TourBookingResolutions
                    .FirstOrDefaultAsync(r => r.OriginalBookingId == booking.Id && r.ResolutionType == "Refund");

                if (existingResolution != null)
                {
                    return Ok(new { Message = "Refund has already been processed for this booking." });
                }

                // Give 100% refund
                decimal refundAmount = booking.TotalPrice;
                if (booking.PaymentStatus == PaymentStatus.Completed)
                {
                    await _walletService.RefundToWalletAsync(userId, refundAmount, $"Refund-Tour-{booking.Id}", $"Refund for cancelled tour booking #{booking.Id}");
                    
                    var transactionItem = await _context.PaymentTransactionItems
                        .FirstOrDefaultAsync(pti => pti.BookingType == "Tour" && pti.BookingId == booking.Id);
                    if (transactionItem != null)
                    {
                        transactionItem.Status = "Refunded";
                    }
                    booking.PaymentStatus = PaymentStatus.Refunded;
                }
                
                resolution.RefundAmount = refundAmount;

                // Issue 5% compensation coupon
                var couponAmount = refundAmount * 0.05m;
                if (couponAmount > 0)
                {
                    var coupon = new UserTourCompensationCoupon
                    {
                        UserId = userId,
                        TriggeringBookingId = booking.Id,
                        CouponCode = $"COMP-TOUR-{booking.Id}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                        DiscountPercentage = 5.0m,
                        IsUsed = false,
                        IssuedAt = DateTime.UtcNow
                    };
                    _context.UserTourCompensationCoupons.Add(coupon);
                }

                booking.Status = BookingStatus.Cancelled;
            }
            else if (dto.Decision == "Alternative")
            {
                if (!dto.AlternativeTourId.HasValue) return BadRequest("Alternative tour ID required.");

                var altTour = await _context.Tours.FindAsync(dto.AlternativeTourId.Value);
                if (altTour == null || !altTour.Active) return BadRequest("Invalid alternative tour.");

                // Rebook to alternative
                booking.TourId = altTour.Id;
                booking.TourGuideId = altTour.TourGuideId;
                booking.Status = BookingStatus.Confirmed; // Restore status
                booking.UpdatedAt = DateTime.UtcNow;

                resolution.NewBookingId = null; // Do not store identical values pretending to be old/new
                resolution.SelectedAlternativeTourId = altTour.Id; // Record chosen alternative tour ID
            }
            else
            {
                return BadRequest("Invalid decision.");
            }

            _context.TourBookingResolutions.Add(resolution);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Decision processed successfully." });
        }

        [HttpGet("/api/users/tour-cancellations")]
        public async Task<IActionResult> GetAffectedBookings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var bookings = await _context.TourBookings
                .Include(b => b.Tour)
                .Where(b => b.UserId == userId && b.Status == BookingStatus.PendingUserDecision)
                .ToListAsync();

            var result = new System.Collections.Generic.List<object>();
            foreach (var b in bookings)
            {
                var cancellationReason = await _context.UrgentRequests
                    .Where(r => r.TourId == b.TourId && r.Status == UrgentRequestStatus.Pending)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.Reason)
                    .FirstOrDefaultAsync() ?? "Tour guide had an emergency cancellation.";

                result.Add(new
                {
                    BookingId = b.Id,
                    TourTitle = b.Tour?.TourTitle ?? "Unknown Tour",
                    City = b.Tour?.City ?? "Unknown Destination",
                    TourDate = b.TourDate ?? b.Tour?.AvailableDateTime ?? b.BookingDate,
                    TourTime = b.TourTime,
                    TotalPrice = b.TotalPrice,
                    CancellationReason = cancellationReason,
                    CompensationMessage = "5% compensation coupon will be issued automatically upon refund"
                });
            }

            return Ok(result);
        }

        [HttpGet("/api/users/tour-cancellations/{bookingId}/alternatives")]
        public async Task<IActionResult> GetAlternativeTours(long bookingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Status != BookingStatus.PendingUserDecision)
                return BadRequest("Booking is not pending a decision.");

            var targetCity = booking.Tour?.City;
            if (string.IsNullOrEmpty(targetCity))
            {
                return Ok(new System.Collections.Generic.List<object>());
            }

            DateTime? originalDate = null;
            if (booking.Tour != null)
            {
                originalDate = booking.Tour.AvailableDateTime;
            }
            if (!originalDate.HasValue && booking.TourDate.HasValue)
            {
                if (booking.TourTime.HasValue)
                {
                    originalDate = booking.TourDate.Value.Date.Add(booking.TourTime.Value);
                }
                else
                {
                    originalDate = booking.TourDate.Value;
                }
            }
            if (!originalDate.HasValue)
            {
                originalDate = booking.BookingDate;
            }

            if (!originalDate.HasValue)
            {
                return Ok(new System.Collections.Generic.List<object>());
            }

            var originalDateVal = originalDate.Value;
            var originalUnitPrice = booking.Tour?.BasePriceUsd ?? (booking.TotalPrice / Math.Max(1, booking.ParticipantsCount));

            // Query active tours in the same city with exact date and time, excluding original tour, future dates
            var candidates = await _context.Tours
                .Where(t => t.City == targetCity && t.Active && t.Id != booking.TourId && t.AvailableDateTime == originalDateVal && t.AvailableDateTime >= DateTime.UtcNow)
                .ToListAsync();

            var result = new System.Collections.Generic.List<object>();
            foreach (var t in candidates)
            {
                int availableSeats = t.GroupSizeMax ?? 10;
                if (t.GroupSizeMax.HasValue)
                {
                    var confirmedParticipants = await _context.TourBookings
                        .Where(b => b.TourId == t.Id && b.Status == BookingStatus.Confirmed)
                        .SumAsync(b => b.ParticipantsCount);
                    availableSeats = Math.Max(0, t.GroupSizeMax.Value - confirmedParticipants);
                }

                // Enforce available seats > 0 (must fit booking's participant count)
                if (availableSeats >= booking.ParticipantsCount)
                {
                    var diffSeconds = Math.Abs((t.AvailableDateTime.Value - originalDateVal).TotalSeconds);
                    var participantsCount = Math.Max(1, booking.ParticipantsCount);
                    var originalTotalPrice = booking.TotalPrice;

                    var originalAltPricePerPerson = t.BasePriceUsd ?? 0;
                    var originalAltPriceTotal = originalAltPricePerPerson * participantsCount;
                    var discountAmountTotal = originalAltPriceTotal * 0.05m;
                    var finalPriceTotal = originalAltPriceTotal - discountAmountTotal;
                    
                    var difference = finalPriceTotal - originalTotalPrice; // Positive means user pays, negative means wallet refund

                    result.Add(new
                    {
                        Id = t.Id,
                        TourTitle = t.TourTitle,
                        City = t.City,
                        AvailableDateTime = t.AvailableDateTime,
                        OriginalPrice = originalAltPriceTotal,
                        DiscountAmount = discountAmountTotal,
                        FinalPrice = finalPriceTotal,
                        Difference = difference,
                        RefundWalletAmount = difference < 0 ? Math.Abs(difference) : 0,
                        PayExtraAmount = difference > 0 ? difference : 0,
                        AvailableSeats = availableSeats,
                        DiffSeconds = diffSeconds
                    });
                }
            }

            // Order by nearest date preference (absolute date difference in seconds)
            var orderedResult = result
                .OrderBy(x => ((dynamic)x).DiffSeconds)
                .Select(x => new
                {
                    Id = ((dynamic)x).Id,
                    TourTitle = ((dynamic)x).TourTitle,
                    City = ((dynamic)x).City,
                    AvailableDateTime = ((dynamic)x).AvailableDateTime,
                    OriginalPrice = ((dynamic)x).OriginalPrice,
                    DiscountAmount = ((dynamic)x).DiscountAmount,
                    FinalPrice = ((dynamic)x).FinalPrice,
                    Difference = ((dynamic)x).Difference,
                    RefundWalletAmount = ((dynamic)x).RefundWalletAmount,
                    PayExtraAmount = ((dynamic)x).PayExtraAmount,
                    AvailableSeats = ((dynamic)x).AvailableSeats
                })
                .ToList();

            return Ok(orderedResult);
        }

        [HttpPost("/api/users/tour-cancellations/{bookingId}/choose-alternative")]
        public async Task<IActionResult> ChooseAlternative(long bookingId, [FromBody] ChooseAlternativeDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Status != BookingStatus.PendingUserDecision)
                return BadRequest("Booking is not pending a decision.");

            var altTour = await _context.Tours.FindAsync(dto.AlternativeTourId);
            if (altTour == null || !altTour.Active) return BadRequest("Invalid alternative tour.");

            var participantsCount = Math.Max(1, booking.ParticipantsCount);
            var originalTotalPrice = booking.TotalPrice;

            var originalAltPricePerPerson = altTour.BasePriceUsd ?? 0;
            var originalAltPriceTotal = originalAltPricePerPerson * participantsCount;
            var discountAmountTotal = originalAltPriceTotal * 0.05m;
            var finalPriceTotal = originalAltPriceTotal - discountAmountTotal;
            
            var difference = finalPriceTotal - originalTotalPrice;

            if (difference > 0)
            {
                if (string.IsNullOrEmpty(dto.PaymentMethod))
                    return BadRequest("Payment method is required to pay the price difference.");

                if (dto.PaymentMethod == "Wallet")
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null || user.WalletBalance < difference)
                        return BadRequest("Insufficient wallet balance.");

                    user.WalletBalance -= difference;
                    _context.WalletTransactions.Add(new TravAi.Airline.Models.WalletTransaction
                    {
                        UserId = userId,
                        Amount = difference,
                        Type = "Withdrawal",
                        ReferenceId = $"ALT-DIFF-{booking.Id}",
                        Description = $"Paid difference for alternative tour {altTour.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (dto.PaymentMethod == "Stripe")
                {
                    Stripe.StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
                    
                    var options = new Stripe.Checkout.SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                        {
                            new Stripe.Checkout.SessionLineItemOptions
                            {
                                PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = (long)(difference * 100),
                                    Currency = "usd",
                                    ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = $"Price difference for {altTour.TourTitle}",
                                    },
                                },
                                Quantity = 1,
                            },
                        },
                        Mode = "payment",
                        // The frontend will be responsible for handling the success redirect
                        SuccessUrl = $"{Request.Scheme}://{Request.Host}/user/tour-cancellations.html?alternativePayment=success&bookingId={booking.Id}&session_id={{CHECKOUT_SESSION_ID}}",
                        CancelUrl = $"{Request.Scheme}://{Request.Host}/user/tour-cancellations.html",
                        Metadata = new Dictionary<string, string>
                        {
                            { "BookingId", booking.Id.ToString() },
                            { "AlternativeTourId", altTour.Id.ToString() }
                        }
                    };

                    var service = new Stripe.Checkout.SessionService();
                    var session = await service.CreateAsync(options);
                    
                    // Do not finalize booking yet, just return Stripe Session URL
                    return Ok(new { RequiresStripePayment = true, CheckoutUrl = session.Url });
                }
                else
                {
                    return BadRequest("Invalid payment method.");
                }
            }
            else if (difference < 0)
            {
                var refundAmount = Math.Abs(difference);
                await _walletService.RefundToWalletAsync(userId, refundAmount, $"Refund-AltDiff-{booking.Id}", $"Refund difference for choosing cheaper alternative tour {altTour.Id}");
            }

            // Consume coupon if applicable
            var coupon = new UserTourCompensationCoupon
            {
                UserId = userId,
                TriggeringBookingId = booking.Id,
                CouponCode = $"COMP-TOUR-{booking.Id}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                DiscountPercentage = 5.0m,
                IsUsed = true, // instantly marked used since it's applied
                IssuedAt = DateTime.UtcNow
            };
            _context.UserTourCompensationCoupons.Add(coupon);

            // Rebook to alternative and update all relevant trip details to correctly reflect the new tour
            booking.TourId = altTour.Id;
            booking.TourGuideId = altTour.TourGuideId;
            booking.Status = BookingStatus.Confirmed;
            
            // Update denormalized fields so My Trips and future refunds reflect the new actual booking values
            booking.TotalPrice = finalPriceTotal;
            if (!string.IsNullOrEmpty(altTour.Currency)) booking.Currency = altTour.Currency;
            if (altTour.AvailableDateTime.HasValue)
            {
                booking.TourDate = altTour.AvailableDateTime.Value.Date;
                booking.TourTime = altTour.AvailableDateTime.Value.TimeOfDay;
            }
            booking.UpdatedAt = DateTime.UtcNow;

            var resolution = new TourBookingResolution
            {
                OriginalBookingId = booking.Id,
                UserId = userId,
                ResolutionType = "Alternative",
                NewBookingId = null,
                SelectedAlternativeTourId = altTour.Id,
                ResolvedAt = DateTime.UtcNow
            };

            _context.TourBookingResolutions.Add(resolution);
            await _context.SaveChangesAsync();

            return Ok(new { RequiresStripePayment = false, Message = "Successfully moved to alternative tour." });
        }

        [HttpPost("/api/users/tour-cancellations/{bookingId}/finalize-alternative-stripe")]
        public async Task<IActionResult> FinalizeAlternativeStripe(long bookingId, [FromBody] FinalizeAlternativeStripeDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();
            
            Stripe.StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session;
            
            try
            {
                session = await service.GetAsync(dto.SessionId);
            }
            catch
            {
                return BadRequest("Invalid session.");
            }

            if (session.PaymentStatus != "paid")
            {
                return BadRequest("Payment not completed.");
            }

            if (!session.Metadata.TryGetValue("BookingId", out var bIdStr) || !long.TryParse(bIdStr, out long sessionBookingId) || sessionBookingId != bookingId)
            {
                return BadRequest("Booking mismatch.");
            }

            if (!session.Metadata.TryGetValue("AlternativeTourId", out var aIdStr) || !long.TryParse(aIdStr, out long altTourId))
            {
                return BadRequest("Invalid tour in session metadata.");
            }

            var booking = await _context.TourBookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
            if (booking == null || booking.Status != BookingStatus.PendingUserDecision)
            {
                // Already processed or invalid
                return Ok(new { Message = "Booking already processed." });
            }

            var altTour = await _context.Tours.FindAsync(altTourId);
            if (altTour == null) return BadRequest("Alternative tour not found.");

            // Consume coupon
            var coupon = new UserTourCompensationCoupon
            {
                UserId = booking.UserId,
                TriggeringBookingId = booking.Id,
                CouponCode = $"COMP-TOUR-{booking.Id}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                DiscountPercentage = 5.0m,
                IsUsed = true,
                IssuedAt = DateTime.UtcNow
            };
            _context.UserTourCompensationCoupons.Add(coupon);

            booking.TourId = altTour.Id;
            booking.TourGuideId = altTour.TourGuideId;
            booking.Status = BookingStatus.Confirmed;
            
            var participantsCount = Math.Max(1, booking.ParticipantsCount);
            var originalAltPricePerPerson = altTour.BasePriceUsd ?? 0;
            var originalAltPriceTotal = originalAltPricePerPerson * participantsCount;
            var discountAmountTotal = originalAltPriceTotal * 0.05m;
            var finalPriceTotal = originalAltPriceTotal - discountAmountTotal;

            booking.TotalPrice = finalPriceTotal;
            if (!string.IsNullOrEmpty(altTour.Currency)) booking.Currency = altTour.Currency;
            if (altTour.AvailableDateTime.HasValue)
            {
                booking.TourDate = altTour.AvailableDateTime.Value.Date;
                booking.TourTime = altTour.AvailableDateTime.Value.TimeOfDay;
            }
            booking.UpdatedAt = DateTime.UtcNow;

            var resolution = new TourBookingResolution
            {
                OriginalBookingId = booking.Id,
                UserId = booking.UserId,
                ResolutionType = "Alternative",
                NewBookingId = null,
                SelectedAlternativeTourId = altTour.Id,
                ResolvedAt = DateTime.UtcNow
            };

            _context.TourBookingResolutions.Add(resolution);
            
            // Log Payment Transaction for Stripe
            var checkoutSessionModel = new CheckoutSession
            {
                UserId = booking.UserId,
                CheckoutType = "HotelTour",
                Status = "Paid",
                TotalAmount = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : 0m,
                StripeCheckoutSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                PaidAt = DateTime.UtcNow
            };
            _context.CheckoutSessions.Add(checkoutSessionModel);
            await _context.SaveChangesAsync(); // save to get ID
            
            var paymentTx = new PaymentTransaction
            {
                CheckoutSessionId = checkoutSessionModel.Id,
                UserId = booking.UserId,
                Amount = checkoutSessionModel.TotalAmount,
                Status = "Completed",
                Provider = "Stripe",
                ProviderTransactionId = session.PaymentIntentId,
                PaymentMethod = "Card",
                CreatedAt = DateTime.UtcNow
            };
            _context.PaymentTransactions.Add(paymentTx);
            await _context.SaveChangesAsync();
            
            _context.PaymentTransactionItems.Add(new PaymentTransactionItem
            {
                PaymentTransactionId = paymentTx.Id,
                BookingType = "Tour",
                BookingId = booking.Id,
                Amount = paymentTx.Amount,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Successfully moved to alternative tour with Stripe payment." });
        }

        [HttpPost("/api/users/tour-cancellations/{bookingId}/refund")]
        public async Task<IActionResult> RequestRefund(long bookingId, [FromBody] TourCancellationRefundDto? dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Status != BookingStatus.PendingUserDecision)
                return BadRequest("Booking is not pending a decision.");

            // Refund Idempotency Check
            var existingResolution = await _context.TourBookingResolutions
                .FirstOrDefaultAsync(r => r.OriginalBookingId == bookingId && r.ResolutionType == "Refund");

            if (existingResolution != null)
            {
                return Ok(new { Message = "Refund has already been processed for this booking." });
            }

            var resolution = new TourBookingResolution
            {
                OriginalBookingId = booking.Id,
                UserId = userId,
                ResolutionType = "Refund",
                ResolvedAt = DateTime.UtcNow
            };

            decimal refundAmount = booking.TotalPrice;
            string refundMethod = dto?.RefundMethod ?? "Wallet";

            if (booking.PaymentStatus == PaymentStatus.Completed)
            {
                var transactionItem = await _context.PaymentTransactionItems
                    .Include(pti => pti.PaymentTransaction)
                    .FirstOrDefaultAsync(pti => pti.BookingType == "Tour" && pti.BookingId == booking.Id);

                if (refundMethod == "OriginalPaymentMethod")
                {
                    if (transactionItem != null && transactionItem.PaymentTransaction.Provider == "Wallet")
                    {
                        return BadRequest("Original payment method refund is unavailable because this booking was paid using wallet.");
                    }

                    if (transactionItem == null || 
                        transactionItem.PaymentTransaction.Provider != "Stripe" || 
                        string.IsNullOrEmpty(transactionItem.PaymentTransaction.ProviderTransactionId))
                    {
                        return BadRequest("Original payment method refund is unavailable because the original Stripe payment reference was not found.");
                    }

                    try
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
                    catch (Exception ex)
                    {
                        return BadRequest($"Stripe refund failed: {ex.Message}");
                    }
                }
                else if (refundMethod == "Wallet")
                {
                    await _walletService.RefundToWalletAsync(userId, refundAmount, $"Refund-Tour-{booking.Id}", $"Refund for cancelled tour booking #{booking.Id}");
                }
                else
                {
                    return BadRequest("Invalid refund method specified.");
                }
                
                if (transactionItem != null)
                {
                    transactionItem.Status = "Refunded";
                }
                booking.PaymentStatus = PaymentStatus.Refunded;
            }
            
            resolution.RefundAmount = refundAmount;

            var couponAmount = refundAmount * 0.05m;
            if (couponAmount > 0)
            {
                var coupon = new UserTourCompensationCoupon
                {
                    UserId = userId,
                    TriggeringBookingId = booking.Id,
                    CouponCode = $"COMP-TOUR-{booking.Id}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    DiscountPercentage = 5.0m,
                    IsUsed = false,
                    IssuedAt = DateTime.UtcNow
                };
                _context.UserTourCompensationCoupons.Add(coupon);
            }

            booking.Status = BookingStatus.Cancelled;

            _context.TourBookingResolutions.Add(resolution);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Refund processed successfully." });
        }

        [HttpGet("/api/users/tour-cancellations/{bookingId}/refund-preview")]
        public async Task<IActionResult> GetRefundPreview(long bookingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out long userId)) return Unauthorized();

            var booking = await _context.TourBookings
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return NotFound("Booking not found.");
            if (booking.Status != BookingStatus.PendingUserDecision)
                return BadRequest("Booking is not pending a decision.");

            var transactionItem = await _context.PaymentTransactionItems
                .Include(pti => pti.PaymentTransaction)
                .FirstOrDefaultAsync(pti => pti.BookingType == "Tour" && pti.BookingId == booking.Id);

            bool originalPaymentAvailable = false;
            string? unavailableReason = null;

            if (transactionItem != null)
            {
                if (transactionItem.PaymentTransaction.Provider == "Stripe")
                {
                    if (!string.IsNullOrEmpty(transactionItem.PaymentTransaction.ProviderTransactionId))
                    {
                        originalPaymentAvailable = true;
                    }
                    else
                    {
                        unavailableReason = "Original payment method refund is unavailable because the original Stripe payment reference was not found.";
                    }
                }
                else if (transactionItem.PaymentTransaction.Provider == "Wallet")
                {
                    unavailableReason = "Original payment method refund is unavailable because this booking was paid using wallet.";
                }
                else
                {
                    unavailableReason = "Original payment method refund is unavailable because the original Stripe payment reference was not found.";
                }
            }
            else
            {
                unavailableReason = "Original payment method refund is unavailable because the original Stripe payment reference was not found.";
            }

            var methods = new List<string> { "Wallet" };
            if (originalPaymentAvailable)
            {
                methods.Add("OriginalPaymentMethod");
            }

            var preview = new TourCancellationRefundPreviewDto
            {
                BookingId = booking.Id,
                TourName = booking.Tour?.TourTitle ?? "Tour",
                RefundAmount = booking.TotalPrice,
                AvailableRefundMethods = methods,
                OriginalPaymentMethodAvailable = originalPaymentAvailable,
                OriginalPaymentUnavailableReason = unavailableReason
            };

            return Ok(preview);
        }
    }
}
