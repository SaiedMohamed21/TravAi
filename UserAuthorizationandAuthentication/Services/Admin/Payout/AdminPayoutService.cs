using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.Models.Admin;
using TravAi.Models.Admin.DTOs;
using TravAi.Models.Enums;
using BookingStatus = TravAi.TourGuide.Models.Enums.BookingStatus;
using PaymentStatus = TravAi.TourGuide.Models.Enums.PaymentStatus;
using HotelPaymentStatus = TravAi.Models.Enums.PaymentStatus;

namespace TravAi.Services.Admin.Payout
{
    public class StripeDestinationException : Exception
    {
        public long PayoutId { get; set; }
        public string ProviderType { get; set; }
        public long ProviderId { get; set; }
        public string DestinationAccount { get; set; }
        
        public StripeDestinationException(string message) : base(message) { }
    }

    public class StripeDestinationVerificationException : Exception
    {
        public string ProviderAccountFromDb { get; set; }
        public string PaymentIntentDestination { get; set; }
        public string PaymentIntentId { get; set; }
        public string CheckoutSessionId { get; set; }
        public string? TransferId { get; set; }

        public StripeDestinationVerificationException(string message) : base(message) { }
    }

    public class AdminPayoutService : IAdminPayoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public AdminPayoutService(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<List<PayoutBatchDto>> GenerateWeeklyPayoutsAsync(long adminUserId, GenerateWeeklyPayoutsRequestDto request)
        {
            var generatedBatches = new List<PayoutBatch>();

            // If we have a specific week start, use it, else we process all un-payout-ed completed bookings.
            // A more robust approach is to gather all eligible bookings and group them by Week.
            var now = DateTime.UtcNow;

            var activeCommissions = await _context.PlatformCommissions
                .Where(c => c.IsActive)
                .ToListAsync();

            decimal GetCommissionPct(string serviceType)
            {
                return activeCommissions.FirstOrDefault(c => c.ServiceType == serviceType)?.Percentage ?? 0m;
            }

            // 1. HOTEL BOOKINGS
            if (request.ProviderType == null || request.ProviderType == ProviderType.Hotel)
            {
                var hotelCommission = GetCommissionPct("Hotel");
                
                var hotelBookings = await _context.HotelBookings
                    .Include(b => b.Hotel)
                    .Where(b => b.PaymentStatus == HotelPaymentStatus.Paid && b.CheckOutDate != null && b.CheckOutDate <= now)
                    .Where(b => !_context.PayoutItems.Any(pi => pi.BookingType == "Hotel" && pi.BookingId == b.Id))
                    .ToListAsync();

                var grouped = hotelBookings.GroupBy(b => new { b.HotelId, WeekStart = GetWeekStart(b.CheckOutDate.Value) })
                                           .OrderBy(g => g.Key.WeekStart);

                foreach (var group in grouped)
                {
                    var weekEnd = group.Key.WeekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    if (request.FilterWeekStartDate.HasValue && group.Key.WeekStart.Date != request.FilterWeekStartDate.Value.Date)
                        continue;

                    var batch = CreateBatch(ProviderType.Hotel, group.Key.HotelId, group.First().Hotel.HotelName, group.Key.WeekStart, weekEnd, adminUserId);
                    
                    foreach (var b in group)
                    {
                        var originalPaid = b.TotalPrice;
                        var refund = b.RefundAmount ?? 0m;
                        var net = originalPaid - refund;
                        var commAmt = net * (hotelCommission / 100m);
                        
                        batch.Items.Add(new PayoutItem
                        {
                            BookingType = "Hotel",
                            BookingId = b.Id,
                            ServiceEndDate = b.CheckOutDate.Value,
                            OriginalPaidAmount = originalPaid,
                            RefundAmount = refund,
                            NetAfterRefundAmount = net,
                            RefundReason = b.CancellationReason,
                            CommissionPercentage = hotelCommission,
                            CommissionAmount = commAmt,
                            ProviderAmount = net - commAmt
                        });
                    }
                    
                    // Process later
                    if (batch.Items.Any()) generatedBatches.Add(batch);
                }
            }

            // 2. TOUR BOOKINGS
            if (request.ProviderType == null || request.ProviderType == ProviderType.TourGuide)
            {
                var tourCommission = GetCommissionPct("Tour");

                var tourBookings = await _context.TourBookings
                    .Include(b => b.TourGuide)
                    .Where(b => b.PaymentStatus == PaymentStatus.Completed && b.TourDate != null && b.TourDate <= now)
                    .Where(b => !_context.PayoutItems.Any(pi => pi.BookingType == "Tour" && pi.BookingId == b.Id))
                    .ToListAsync();
                    
                var tourResolutions = await _context.TourBookingResolutions
                    .Where(r => tourBookings.Select(b => b.Id).Contains(r.OriginalBookingId))
                    .ToListAsync();

                var grouped = tourBookings.GroupBy(b => new { b.TourGuideId, WeekStart = GetWeekStart(b.TourDate.Value) })
                                          .OrderBy(g => g.Key.WeekStart);

                foreach (var group in grouped)
                {
                    var weekEnd = group.Key.WeekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    if (request.FilterWeekStartDate.HasValue && group.Key.WeekStart.Date != request.FilterWeekStartDate.Value.Date)
                        continue;

                    var batch = CreateBatch(ProviderType.TourGuide, group.Key.TourGuideId, group.First().TourGuide.Name, group.Key.WeekStart, weekEnd, adminUserId);
                    
                    foreach (var b in group)
                    {
                        var originalPaid = b.TotalPrice;
                        var res = tourResolutions.FirstOrDefault(r => r.OriginalBookingId == b.Id);
                        var refund = res?.RefundAmount ?? 0m;
                        var net = originalPaid - refund;
                        var commAmt = net * (tourCommission / 100m);
                        
                        batch.Items.Add(new PayoutItem
                        {
                            BookingType = "Tour",
                            BookingId = b.Id,
                            ServiceEndDate = b.TourDate.Value,
                            OriginalPaidAmount = originalPaid,
                            RefundAmount = refund,
                            NetAfterRefundAmount = net,
                            RefundReason = b.CancellationReason,
                            CommissionPercentage = tourCommission,
                            CommissionAmount = commAmt,
                            ProviderAmount = net - commAmt
                        });
                    }
                    
                    // Process later
                    if (batch.Items.Any()) generatedBatches.Add(batch);
                }
            }

            // 3. AIRLINE BOOKINGS
            if (request.ProviderType == null || request.ProviderType == ProviderType.Airline)
            {
                var airlineCommission = GetCommissionPct("Airline");

                var airlineBookings = await _context.Bookings
                    .Include(b => b.Flight)
                        .ThenInclude(f => f.Airline)
                    .Where(b => (b.PaymentStatus == "Paid" || b.PaymentStatus == "Refunded") && b.Flight.ArrivalTime != null && b.Flight.ArrivalTime <= now && b.Flight.AirlineId != null)
                    .Where(b => !_context.PayoutItems.Any(pi => pi.BookingType == "Airline" && pi.BookingId == b.Id))
                    .ToListAsync();

                var grouped = airlineBookings.GroupBy(b => new { AirlineId = b.Flight.AirlineId.Value, WeekStart = GetWeekStart(b.Flight.ArrivalTime.Value) })
                                             .OrderBy(g => g.Key.WeekStart);

                foreach (var group in grouped)
                {
                    var weekEnd = group.Key.WeekStart.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    if (request.FilterWeekStartDate.HasValue && group.Key.WeekStart.Date != request.FilterWeekStartDate.Value.Date)
                        continue;

                    var batch = CreateBatch(ProviderType.Airline, group.Key.AirlineId, group.First().Flight.Airline.Name, group.Key.WeekStart, weekEnd, adminUserId);
                    
                    foreach (var b in group)
                    {
                        var originalPaid = b.TotalPrice;
                        var refund = b.PaymentStatus == "Refunded" ? b.TotalPrice : 0m;
                        var net = originalPaid - refund;
                        var commAmt = net * (airlineCommission / 100m);
                        
                        batch.Items.Add(new PayoutItem
                        {
                            BookingType = "Airline",
                            BookingId = b.Id,
                            ServiceEndDate = b.Flight.ArrivalTime.Value,
                            OriginalPaidAmount = originalPaid,
                            RefundAmount = refund,
                            NetAfterRefundAmount = net,
                            RefundReason = b.PaymentStatus == "Refunded" ? (b.RejectionReason ?? "Refunded") : null,
                            CommissionPercentage = airlineCommission,
                            CommissionAmount = commAmt,
                            ProviderAmount = net - commAmt
                        });
                    }
                    
                    // Process later
                    if (batch.Items.Any()) generatedBatches.Add(batch);
                }
            }

            var allocatedFinesInRun = new HashSet<long>();
            foreach (var batch in generatedBatches.OrderBy(b => b.WeekStartDate))
            {
                await ProcessBatch(batch, allocatedFinesInRun);
            }

            // Remove items with 0 bookings just in case, though they aren't added if empty
            generatedBatches = generatedBatches.Where(b => b.Items.Any()).ToList();

            _context.PayoutBatches.AddRange(generatedBatches);
            await _context.SaveChangesAsync();

            return generatedBatches.Select(MapToDto).ToList();
        }

        private async Task ProcessBatch(PayoutBatch batch, HashSet<long> allocatedFinesInRun)
        {
            // Avoid duplicate batches
            var exists = await _context.PayoutBatches.AnyAsync(pb => 
                pb.ProviderType == batch.ProviderType && 
                pb.ProviderId == batch.ProviderId && 
                pb.WeekStartDate == batch.WeekStartDate);

            if (exists) 
            {
                batch.Items.Clear(); // Clear items so it's not saved
                return;
            }

            batch.GrossAmount = batch.Items.Sum(i => i.OriginalPaidAmount);
            batch.TotalRefundAmount = batch.Items.Sum(i => i.RefundAmount);
            batch.NetAfterRefundAmount = batch.Items.Sum(i => i.NetAfterRefundAmount);
            batch.TotalCommissionAmount = batch.Items.Sum(i => i.CommissionAmount);

            var providerAmountBeforeFines = batch.Items.Sum(i => i.ProviderAmount);

            // Fetch eligible fines
            var eligibleFines = await _context.ProviderFines
                .Where(f => f.ProviderType == batch.ProviderType && 
                            f.ProviderId == batch.ProviderId && 
                            f.Status == ProviderFineStatus.Active && 
                            f.CreatedAt.AddMonths(1) <= batch.WeekEndDate)
                .OrderBy(f => f.CreatedAt).ThenBy(f => f.Id)
                .ToListAsync();

            // Make sure fines aren't already pending in another pending batch or paid batch
            var alreadyDeductedFineIds = await _context.PayoutFineDeductions
                .Where(d => d.PayoutBatch.Status != PayoutBatchStatus.Failed)
                .Select(d => d.ProviderFineId)
                .Distinct()
                .ToListAsync();

            foreach (var fine in eligibleFines)
            {
                if (alreadyDeductedFineIds.Contains(fine.Id) || allocatedFinesInRun.Contains(fine.Id)) continue;

                batch.Deductions.Add(new PayoutFineDeduction
                {
                    ProviderFineId = fine.Id,
                    Amount = fine.Amount,
                    ReasonSnapshot = fine.Reason,
                    FineCreatedAt = fine.CreatedAt
                });
                
                allocatedFinesInRun.Add(fine.Id);
            }

            batch.TotalFineDeductionAmount = batch.Deductions.Sum(d => d.Amount);
            batch.FinalPayoutAmount = providerAmountBeforeFines - batch.TotalFineDeductionAmount;
        }

        private PayoutBatch CreateBatch(ProviderType type, long providerId, string? providerName, DateTime start, DateTime end, long adminId)
        {
            return new PayoutBatch
            {
                ProviderType = type,
                ProviderId = providerId,
                ProviderNameSnapshot = providerName,
                WeekStartDate = start,
                WeekEndDate = end,
                GeneratedByAdminUserId = adminId
            };
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        public async Task<List<PayoutBatchDto>> GetPayoutBatchesAsync(ProviderType? providerType, string? status, DateTime? weekStart, DateTime? weekEnd, int? month, int? year)
        {
            var query = _context.PayoutBatches
                .Include(b => b.GeneratedByAdminUser)
                .Include(b => b.ConfirmedByAdminUser)
                .AsQueryable();

            if (providerType.HasValue) query = query.Where(b => b.ProviderType == providerType.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayoutBatchStatus>(status, out var s)) 
                query = query.Where(b => b.Status == s);
            if (weekStart.HasValue) query = query.Where(b => b.WeekStartDate >= weekStart.Value);
            if (weekEnd.HasValue) query = query.Where(b => b.WeekEndDate <= weekEnd.Value);
            if (month.HasValue) query = query.Where(b => b.WeekStartDate.Month == month.Value || b.WeekEndDate.Month == month.Value);
            if (year.HasValue) query = query.Where(b => b.WeekStartDate.Year == year.Value || b.WeekEndDate.Year == year.Value);

            var batches = await query.OrderByDescending(b => b.WeekStartDate).ToListAsync();
            return batches.Select(MapToDto).ToList();
        }

        public async Task<PayoutBatchDetailDto?> GetPayoutBatchDetailsAsync(long id)
        {
            var batch = await _context.PayoutBatches
                .Include(b => b.GeneratedByAdminUser)
                .Include(b => b.ConfirmedByAdminUser)
                .Include(b => b.Items)
                .Include(b => b.Deductions)
                    .ThenInclude(d => d.ProviderFine)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null) return null;

            var dto = new PayoutBatchDetailDto
            {
                Id = batch.Id,
                ProviderType = batch.ProviderType.ToString(),
                ProviderId = batch.ProviderId,
                ProviderNameSnapshot = batch.ProviderNameSnapshot,
                WeekStartDate = batch.WeekStartDate,
                WeekEndDate = batch.WeekEndDate,
                Status = batch.Status.ToString(),
                GrossAmount = batch.GrossAmount,
                TotalRefundAmount = batch.TotalRefundAmount,
                NetAfterRefundAmount = batch.NetAfterRefundAmount,
                TotalCommissionAmount = batch.TotalCommissionAmount,
                TotalFineDeductionAmount = batch.TotalFineDeductionAmount,
                FinalPayoutAmount = batch.FinalPayoutAmount,
                Currency = batch.Currency,
                GeneratedAt = batch.GeneratedAt,
                GeneratedByAdminUserEmail = batch.GeneratedByAdminUser?.Email,
                ConfirmedAt = batch.ConfirmedAt,
                ConfirmedByAdminUserEmail = batch.ConfirmedByAdminUser?.Email,
                FailedAt = batch.FailedAt,
                FailureReason = batch.FailureReason,
                Notes = batch.Notes,
                Items = batch.Items.Select(i => {
                    var dtoItem = new PayoutItemDto
                    {
                        Id = i.Id,
                        BookingType = i.BookingType,
                        BookingId = i.BookingId,
                        ServiceEndDate = i.ServiceEndDate,
                        OriginalPaidAmount = i.OriginalPaidAmount,
                        RefundAmount = i.RefundAmount,
                        NetAfterRefundAmount = i.NetAfterRefundAmount,
                        RefundReason = i.RefundReason,
                        CommissionPercentage = i.CommissionPercentage,
                        CommissionAmount = i.CommissionAmount,
                        ProviderAmount = i.ProviderAmount,
                        Currency = i.Currency,
                        CreatedAt = i.CreatedAt
                    };

                    if (i.BookingType == "Hotel")
                    {
                        var hb = _context.HotelBookings.Include(b => b.User).FirstOrDefault(b => b.Id == i.BookingId);
                        if (hb != null)
                        {
                            dtoItem.GuestName = hb.User?.Name ?? hb.User?.Email;
                            dtoItem.CheckInDate = hb.CheckInDate;
                            dtoItem.CheckOutDate = hb.CheckOutDate;
                        }
                    }

                    return dtoItem;
                }).ToList(),
                Deductions = batch.Deductions.Select(d => new PayoutFineDeductionDto
                {
                    Id = d.Id,
                    ProviderFineId = d.ProviderFineId,
                    Amount = d.Amount,
                    ReasonSnapshot = d.ReasonSnapshot,
                    FineCreatedAt = d.FineCreatedAt,
                    AppliedAt = d.AppliedAt,
                    SourceComplaintId = d.ProviderFine?.ComplaintId
                }).ToList()
            };

            return dto;
        }

        public async Task<PayoutBatchDto> ConfirmPayoutAsync(long id, long adminUserId, ConfirmPayoutRequestDto request)
        {
            var batch = await _context.PayoutBatches
                .Include(b => b.Deductions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null) throw new Exception("Batch not found");
            if (batch.Status != PayoutBatchStatus.Pending) throw new Exception("Only pending batches can be confirmed");

            if (batch.FinalPayoutAmount > 0)
            {
                throw new Exception("Use create-payment-session to confirm this positive payout through Stripe.");
            }

            batch.Status = PayoutBatchStatus.Paid;
            batch.ConfirmedAt = DateTime.UtcNow;
            batch.ConfirmedByAdminUserId = adminUserId;
            if (!string.IsNullOrEmpty(request.Notes)) batch.Notes = request.Notes;

            // Mark fines as deducted
            foreach (var deduction in batch.Deductions)
            {
                var fine = await _context.ProviderFines.FindAsync(deduction.ProviderFineId);
                if (fine != null)
                {
                    fine.Status = ProviderFineStatus.Deducted;
                }
            }

            await _context.SaveChangesAsync();

            // Load users to return proper DTO
            await _context.Entry(batch).Reference(b => b.GeneratedByAdminUser).LoadAsync();
            await _context.Entry(batch).Reference(b => b.ConfirmedByAdminUser).LoadAsync();

            return MapToDto(batch);
        }

        public async Task<PayoutBatchDto> MarkPayoutFailedAsync(long id, long adminUserId, MarkPayoutFailedRequestDto request)
        {
            var batch = await _context.PayoutBatches.FirstOrDefaultAsync(b => b.Id == id);
            if (batch == null) throw new Exception("Batch not found");
            if (batch.Status != PayoutBatchStatus.Pending) throw new Exception("Only pending batches can be failed");

            batch.Status = PayoutBatchStatus.Failed;
            batch.FailedAt = DateTime.UtcNow;
            batch.FailureReason = request.FailureReason;

            await _context.SaveChangesAsync();
            
            await _context.Entry(batch).Reference(b => b.GeneratedByAdminUser).LoadAsync();
            
            return MapToDto(batch);
        }

        public async Task<PayoutSummaryDto> GetPayoutSummaryAsync()
        {
            var batches = await _context.PayoutBatches.ToListAsync();
            
            return new PayoutSummaryDto
            {
                TotalPendingBatches = batches.Count(b => b.Status == PayoutBatchStatus.Pending),
                TotalPendingAmount = batches.Where(b => b.Status == PayoutBatchStatus.Pending).Sum(b => b.FinalPayoutAmount),
                TotalPaidBatches = batches.Count(b => b.Status == PayoutBatchStatus.Paid),
                TotalPaidAmount = batches.Where(b => b.Status == PayoutBatchStatus.Paid).Sum(b => b.FinalPayoutAmount),
                TotalFailedBatches = batches.Count(b => b.Status == PayoutBatchStatus.Failed)
            };
        }

        private PayoutBatchDto MapToDto(PayoutBatch b)
        {
            return new PayoutBatchDto
            {
                Id = b.Id,
                ProviderType = b.ProviderType.ToString(),
                ProviderId = b.ProviderId,
                ProviderNameSnapshot = b.ProviderNameSnapshot,
                WeekStartDate = b.WeekStartDate,
                WeekEndDate = b.WeekEndDate,
                Status = b.Status.ToString(),
                GrossAmount = b.GrossAmount,
                TotalRefundAmount = b.TotalRefundAmount,
                NetAfterRefundAmount = b.NetAfterRefundAmount,
                TotalCommissionAmount = b.TotalCommissionAmount,
                TotalFineDeductionAmount = b.TotalFineDeductionAmount,
                FinalPayoutAmount = b.FinalPayoutAmount,
                Currency = b.Currency,
                GeneratedAt = b.GeneratedAt,
                GeneratedByAdminUserEmail = b.GeneratedByAdminUser?.Email,
                ConfirmedAt = b.ConfirmedAt,
                ConfirmedByAdminUserEmail = b.ConfirmedByAdminUser?.Email,
                FailedAt = b.FailedAt,
                FailureReason = b.FailureReason,
                Notes = b.Notes
            };
        }

        public async Task<(bool skipped, string? checkoutUrl, string message)> CreatePayoutStripeSessionAsync(long id, long adminUserId, string successUrl, string cancelUrl)
        {
            var batch = await _context.PayoutBatches
                .Include(b => b.Deductions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null) throw new Exception("Payout batch not found");
            if (batch.Status != PayoutBatchStatus.Pending) throw new Exception("Payout status is not Pending");

            if (batch.FinalPayoutAmount <= 0)
            {
                // Process without Stripe
                batch.Status = PayoutBatchStatus.Paid;
                batch.ConfirmedAt = DateTime.UtcNow;
                batch.ConfirmedByAdminUserId = adminUserId;

                foreach (var deduction in batch.Deductions)
                {
                    var fine = await _context.ProviderFines.FindAsync(deduction.ProviderFineId);
                    if (fine != null) fine.Status = ProviderFineStatus.Deducted;
                }

                var skippedPayment = new PayoutStripePayment
                {
                    PayoutBatchId = batch.Id,
                    ProviderType = batch.ProviderType,
                    ProviderId = batch.ProviderId,
                    Amount = batch.FinalPayoutAmount,
                    Currency = batch.Currency,
                    Status = StripePaymentStatus.Skipped,
                    CreatedByAdminUserId = adminUserId,
                    PaidAt = DateTime.UtcNow
                };
                
                // Fetch the provider account if it exists, otherwise just leave the FK unassigned (it is required so we need to assign it)
                var providerAccount = await _context.ProviderStripePayoutAccounts
                    .FirstOrDefaultAsync(a => a.ProviderType == batch.ProviderType && a.ProviderId == batch.ProviderId);
                
                if (providerAccount != null)
                {
                    skippedPayment.ProviderStripePayoutAccountId = providerAccount.Id;
                    skippedPayment.StripeConnectedAccountId = providerAccount.StripeConnectedAccountId;
                    _context.PayoutStripePayments.Add(skippedPayment);
                }

                await _context.SaveChangesAsync();
                
                string msg = batch.FinalPayoutAmount == 0 ? "No card payment required because final payout amount is zero." : "No card payment required because final payout amount is negative after deductions.";
                return (true, null, msg);
            }

            var account = await _context.ProviderStripePayoutAccounts
                .FirstOrDefaultAsync(a => a.ProviderType == batch.ProviderType && a.ProviderId == batch.ProviderId);

            if (account == null || !account.IsActive)
            {
                throw new Exception("Active provider Stripe payout account not found.");
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            Stripe.StripeConfiguration.ApiKey = secretKey;

            try
            {
                var accountService = new Stripe.AccountService();
                var destinationAccount = await accountService.GetAsync(account.StripeConnectedAccountId);
                Console.WriteLine($"Stripe Account Check (Non-blocking): {destinationAccount.Id}, ChargesEnabled={destinationAccount.ChargesEnabled}, PayoutsEnabled={destinationAccount.PayoutsEnabled}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe Account Diagnostics Check (Non-blocking Error): {ex.Message}");
            }

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(batch.FinalPayoutAmount * 100),
                            Currency = account.Currency.ToLower(),
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Provider payout #{batch.Id}"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
                {
                    TransferData = new Stripe.Checkout.SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = account.StripeConnectedAccountId
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["PayoutBatchId"] = batch.Id.ToString(),
                        ["ProviderType"] = batch.ProviderType.ToString(),
                        ["ProviderId"] = batch.ProviderId.ToString(),
                        ["StripeDestinationAccount"] = account.StripeConnectedAccountId
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["PayoutBatchId"] = batch.Id.ToString(),
                    ["ProviderType"] = batch.ProviderType.ToString(),
                    ["ProviderId"] = batch.ProviderId.ToString(),
                    ["StripeDestinationAccount"] = account.StripeConnectedAccountId
                }
            };

            Stripe.Checkout.Session session;
            try
            {
                var service = new Stripe.Checkout.SessionService();
                session = await service.CreateAsync(options);
            }
            catch (Stripe.StripeException ex)
            {
                throw new StripeDestinationException(ex.Message)
                {
                    PayoutId = batch.Id,
                    ProviderType = batch.ProviderType.ToString(),
                    ProviderId = batch.ProviderId,
                    DestinationAccount = account.StripeConnectedAccountId
                };
            }
            
            Console.WriteLine($"CheckoutSessionId: {session.Id}");
            Console.WriteLine($"PaymentIntentData.TransferData.Destination requested: {account.StripeConnectedAccountId}");
            Console.WriteLine($"ProviderStripeAccountFromDb: {account.StripeConnectedAccountId}");

            var payment = new PayoutStripePayment
            {
                PayoutBatchId = batch.Id,
                ProviderStripePayoutAccountId = account.Id,
                ProviderType = batch.ProviderType,
                ProviderId = batch.ProviderId,
                StripeConnectedAccountId = account.StripeConnectedAccountId,
                StripeCheckoutSessionId = session.Id,
                Amount = batch.FinalPayoutAmount,
                Currency = account.Currency,
                Status = StripePaymentStatus.Created,
                CreatedByAdminUserId = adminUserId
            };

            _context.PayoutStripePayments.Add(payment);
            await _context.SaveChangesAsync();

            return (false, session.Url, "Checkout session created.");
        }

        public async Task<PayoutBatchDto> VerifyPayoutStripePaymentAsync(long id, string sessionId, long adminUserId)
        {
            var batch = await _context.PayoutBatches
                .Include(b => b.Deductions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null) throw new Exception("Batch not found");
            
            var payment = await _context.PayoutStripePayments
                .FirstOrDefaultAsync(p => p.StripeCheckoutSessionId == sessionId && p.PayoutBatchId == id);

            if (payment == null) throw new Exception("Payment record not found");

            if (payment.Status == StripePaymentStatus.Paid)
            {
                // Already paid, return idempotently
                var dto = MapToDto(batch);
                dto.CheckoutSessionId = sessionId;
                dto.PaymentIntentId = payment.StripePaymentIntentId;
                dto.PaymentIntentDestination = payment.StripeConnectedAccountId;
                dto.ProviderAccountFromDb = payment.StripeConnectedAccountId;
                try
                {
                    if (!string.IsNullOrEmpty(payment.StripePaymentIntentId))
                    {
                        var piServiceIdempotent = new Stripe.PaymentIntentService();
                        var pi = await piServiceIdempotent.GetAsync(payment.StripePaymentIntentId);
                        dto.PaymentIntentDestination = pi.TransferData?.DestinationId;
                        if (pi.LatestCharge != null)
                        {
                            dto.TransferId = pi.LatestCharge.TransferId;
                        }
                        else if (!string.IsNullOrEmpty(pi.LatestChargeId))
                        {
                            var chargeService = new Stripe.ChargeService();
                            var charge = await chargeService.GetAsync(pi.LatestChargeId);
                            dto.TransferId = charge?.TransferId;
                        }
                    }
                }
                catch {}
                return dto;
            }

            var secretKey = _configuration["Stripe:SecretKey"];
            Stripe.StripeConfiguration.ApiKey = secretKey;

            var service = new Stripe.Checkout.SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.PaymentStatus != "paid")
            {
                throw new Exception("Stripe session is not paid.");
            }

            if (!session.Metadata.TryGetValue("PayoutBatchId", out var metaId) || metaId != id.ToString())
            {
                throw new Exception("Session metadata mismatch.");
            }

            var piService = new Stripe.PaymentIntentService();
            var paymentIntent = await piService.GetAsync(session.PaymentIntentId);

            string? transferId = null;
            if (paymentIntent.LatestCharge != null)
            {
                transferId = paymentIntent.LatestCharge.TransferId;
            }
            else if (!string.IsNullOrEmpty(paymentIntent.LatestChargeId))
            {
                try
                {
                    var chargeService = new Stripe.ChargeService();
                    var charge = await chargeService.GetAsync(paymentIntent.LatestChargeId);
                    transferId = charge?.TransferId;
                }
                catch {}
            }

            var expectedDestination = payment.StripeConnectedAccountId;

            if (paymentIntent.TransferData == null || string.IsNullOrEmpty(paymentIntent.TransferData.DestinationId))
            {
                throw new StripeDestinationVerificationException("Stripe payment succeeded but no provider destination transfer was found.")
                {
                    ProviderAccountFromDb = expectedDestination,
                    PaymentIntentDestination = "",
                    PaymentIntentId = session.PaymentIntentId,
                    CheckoutSessionId = sessionId,
                    TransferId = transferId
                };
            }

            var actualDestination = paymentIntent.TransferData.DestinationId;

            if (actualDestination != expectedDestination)
            {
                throw new StripeDestinationVerificationException("Stripe payment destination does not match provider payout account.")
                {
                    ProviderAccountFromDb = expectedDestination,
                    PaymentIntentDestination = actualDestination,
                    PaymentIntentId = session.PaymentIntentId,
                    CheckoutSessionId = sessionId,
                    TransferId = transferId
                };
            }

            payment.StripePaymentIntentId = session.PaymentIntentId;
            payment.Status = StripePaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;

            batch.Status = PayoutBatchStatus.Paid;
            batch.ConfirmedAt = DateTime.UtcNow;
            batch.ConfirmedByAdminUserId = adminUserId;
            batch.Notes = $"Paid via Stripe (Intent: {session.PaymentIntentId})";

            foreach (var deduction in batch.Deductions)
            {
                var fine = await _context.ProviderFines.FindAsync(deduction.ProviderFineId);
                if (fine != null) fine.Status = ProviderFineStatus.Deducted;
            }

            await _context.SaveChangesAsync();
            await _context.Entry(batch).Reference(b => b.GeneratedByAdminUser).LoadAsync();
            await _context.Entry(batch).Reference(b => b.ConfirmedByAdminUser).LoadAsync();

            var successDto = MapToDto(batch);
            successDto.CheckoutSessionId = sessionId;
            successDto.PaymentIntentId = session.PaymentIntentId;
            successDto.PaymentIntentDestination = actualDestination;
            successDto.TransferId = transferId;
            successDto.ProviderAccountFromDb = expectedDestination;

            return successDto;
        }

        public async Task<PayoutStripePaymentReceiptDto?> GetPayoutStripePaymentAsync(long id)
        {
            var payment = await _context.PayoutStripePayments
                .Include(p => p.ProviderStripePayoutAccount)
                .Where(p => p.PayoutBatchId == id && (p.Status == StripePaymentStatus.Paid || p.Status == StripePaymentStatus.Skipped))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null || payment.ProviderStripePayoutAccount == null) return null;

            return new PayoutStripePaymentReceiptDto
            {
                PayoutBatchId = payment.PayoutBatchId,
                ProviderType = payment.ProviderType.ToString(),
                ProviderId = payment.ProviderId,
                ProviderPayoutAccountNumber = payment.ProviderStripePayoutAccount.ProviderPayoutAccountNumber,
                StripeConnectedAccountId = payment.StripeConnectedAccountId,
                StripeDestinationAccount = payment.StripeConnectedAccountId,
                StripeCheckoutSessionId = payment.StripeCheckoutSessionId,
                StripePaymentIntentId = payment.StripePaymentIntentId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                BankName = payment.ProviderStripePayoutAccount.BankName,
                BankLast4 = payment.ProviderStripePayoutAccount.BankLast4,
                PaidAt = payment.PaidAt
            };
        }

        public async Task<(bool CanSee, List<string> AvailableAccounts, string? ErrorMessage)> TestStripeConnectVisibilityAsync(string destinationAccount)
        {
            try
            {
                var secretKey = _configuration["Stripe:SecretKey"];
                Stripe.StripeConfiguration.ApiKey = secretKey;

                var service = new Stripe.AccountService();
                var options = new Stripe.AccountListOptions { Limit = 100 };
                var accounts = await service.ListAsync(options);
                
                var availableAccounts = accounts.Select(a => a.Id).ToList();
                var canSee = availableAccounts.Contains(destinationAccount);
                
                return (canSee, availableAccounts, null);
            }
            catch (Exception ex)
            {
                return (false, new List<string>(), ex.Message);
            }
        }
    }
}
