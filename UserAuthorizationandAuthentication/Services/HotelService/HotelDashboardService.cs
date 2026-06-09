using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.DTOs.Hotel;
using TravAi.Models.Enums;
using TravAi.Models.Hotels;
using TravAi.Models.Hotels.Bookings;
using TravAi.Services.FileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TravAi.Services.HotelService
{
    public class HotelDashboardService : IHotelDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public HotelDashboardService(ApplicationDbContext context, IFileService fileService, IServiceProvider serviceProvider)
        {
            _context = context;
            _fileService = fileService;
            _serviceProvider = serviceProvider;
        }

        private async Task<Hotel?> ResolveHotelAsync(long userId, long? hotelId)
        {
            if (hotelId.HasValue)
            {
                return await _context.Hotels
                    .Include(h => h.User)
                    .Include(h => h.Rooms)
                    .Include(h => h.Bookings)
                    .FirstOrDefaultAsync(h => h.Id == hotelId.Value);
            }
            return await _context.Hotels
                .Include(h => h.User)
                .Include(h => h.Rooms)
                .Include(h => h.Bookings)
                .FirstOrDefaultAsync(h => h.UserId == userId);
        }

        public async Task<HotelFinancialsDto> GetFinancialsAsync(long userId, int year, long? hotelId = null)
        {
            var hotel = await ResolveHotelAsync(userId, hotelId);
            if (hotel == null) return new HotelFinancialsDto();

            var now = DateTime.UtcNow;
            
            // 1. Base query for successful payments
            var successfulPayments = await _context.HotelPayments
                .Where(p => p.HotelId == hotel.Id && p.Status == HotelPaymentStatus.Paid && p.PaidAt.HasValue)
                .ToListAsync();

            // 2. Highest Earning Month
            var monthlyRevenueAllTime = successfulPayments
                .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            var highestMonthLabel = monthlyRevenueAllTime != null 
                ? new DateTime(monthlyRevenueAllTime.Year, monthlyRevenueAllTime.Month, 1).ToString("MMMM yyyy") 
                : "No Data";
            var highestMonthRev = monthlyRevenueAllTime?.Total ?? 0;

            // 3. Total Collected Revenue
            var totalRev = successfulPayments.Sum(p => p.Amount);

            // 4. YTD Growth
            var currentYTD = successfulPayments
                .Where(p => p.PaidAt!.Value.Year == now.Year && p.PaidAt.Value <= now)
                .Sum(p => p.Amount);

            var lastYearSameTime = now.AddYears(-1);
            var previousYTD = successfulPayments
                .Where(p => p.PaidAt!.Value.Year == (now.Year - 1) && p.PaidAt.Value <= lastYearSameTime)
                .Sum(p => p.Amount);

            decimal growthPct = 0;
            if (previousYTD > 0) growthPct = ((currentYTD - previousYTD) / previousYTD) * 100;
            else if (currentYTD > 0) growthPct = 100;

            // 5. Yearly Chart (selected year)
            var yearPayments = successfulPayments.Where(p => p.PaidAt!.Value.Year == year).ToList();
            var chartData = new List<ChartDataPoint>();
            for (int m = 1; m <= 12; m++)
            {
                var monthName = new DateTime(year, m, 1).ToString("MMM");
                var revenue = yearPayments.Where(p => p.PaidAt!.Value.Month == m).Sum(p => p.Amount);
                chartData.Add(new ChartDataPoint { Month = monthName, Revenue = revenue });
            }

            // 6. Monthly Breakdown (Selected Year)
            var totalInventory = await _context.HotelRooms.Where(r => r.HotelId == hotel.Id && r.State == RoomState.Available).SumAsync(r => r.Quantity);
            
            var validStatuses = new[] { BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.CheckedOut, BookingStatus.Completed };
            var activeBookings = await _context.HotelBookings
                .Where(b => b.HotelId == hotel.Id && validStatuses.Contains(b.Status) && b.CheckInDate.HasValue && b.CheckOutDate.HasValue)
                .Where(b => b.CheckInDate!.Value.Year == year || b.CheckOutDate!.Value.Year == year)
                .ToListAsync();

            var breakdown = new List<MonthlyBreakdownRow>();
            for (int m = 1; m <= 12; m++)
            {
                var monthStart = new DateTime(year, m, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var daysInMonth = DateTime.DaysInMonth(year, m);

                var monthRev = yearPayments.Where(p => p.PaidAt!.Value.Month == m).Sum(p => p.Amount);
                
                double occupancyPct = 0;
                if (totalInventory > 0)
                {
                    long totalBookedRoomNights = 0;
                    foreach (var b in activeBookings)
                    {
                        var stayStart = b.CheckInDate!.Value > monthStart ? b.CheckInDate.Value : monthStart;
                        var stayEnd = b.CheckOutDate!.Value < monthEnd ? b.CheckOutDate.Value : monthEnd;

                        if (stayStart <= stayEnd)
                        {
                            var overlapDays = (stayEnd - stayStart).Days + 1;
                            if (overlapDays > 0)
                            {
                                totalBookedRoomNights += (overlapDays * b.TotalRooms);
                            }
                        }
                    }
                    long totalCapacityRoomNights = totalInventory * daysInMonth;
                    occupancyPct = (double)totalBookedRoomNights / totalCapacityRoomNights * 100;
                    if (occupancyPct > 100) occupancyPct = 100;
                }

                breakdown.Add(new MonthlyBreakdownRow
                {
                    Month = monthStart.ToString("MMMM"),
                    GrossRevenue = monthRev,
                    OccupancyPercentage = Math.Round(occupancyPct, 1)
                });
            }

            return new HotelFinancialsDto
            {
                HighestEarningMonth = highestMonthLabel,
                HighestEarningMonthRevenue = highestMonthRev,
                TotalCollectedRevenue = totalRev,
                YtdGrowthPercentage = Math.Round(growthPct, 2),
                YearlyRevenueOverview = chartData,
                MonthlyBreakdown = breakdown
            };
        }

        public async Task<HotelInboxSummaryDto> GetInboxDashboardAsync(long userId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return new HotelInboxSummaryDto();

            // Get latest 5 items for each category
            var financial = await GetInboxCategoryItemsAsync(hotel.Id, InboxCategory.FinancialTransaction, 1, 5);
            var alerts = await GetInboxCategoryItemsAsync(hotel.Id, InboxCategory.WarningAlert, 1, 5);
            var actions = await GetInboxCategoryItemsAsync(hotel.Id, InboxCategory.UpcomingActionInstruction, 1, 5);

            return new HotelInboxSummaryDto
            {
                FinancialTransactions = financial.Items,
                WarningsAlerts = alerts.Items,
                UpcomingActions = actions.Items
            };
        }

        public async Task<HotelInboxPagedDto<HotelInboxItemDto>> GetInboxCategoryPagedAsync(long userId, InboxCategory category, int page, int pageSize)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return new HotelInboxPagedDto<HotelInboxItemDto> { Items = new() };

            return await GetInboxCategoryItemsAsync(hotel.Id, category, page, pageSize);
        }

        private async Task<HotelInboxPagedDto<HotelInboxItemDto>> GetInboxCategoryItemsAsync(long hotelId, InboxCategory category, int page, int pageSize)
        {
            var query = _context.HotelAdminInboxMessages
                .Where(m => m.HotelId == hotelId && m.Category == category)
                .OrderByDescending(m => m.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.Replies)
                .ToListAsync();

            return new HotelInboxPagedDto<HotelInboxItemDto>
            {
                TotalCount = total,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = items.Select(MapToInboxItemDto).ToList()
            };
        }

        public async Task<HotelInboxItemDto> GetInboxMessageDetailsAsync(long userId, long messageId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return null;

            var msg = await _context.HotelAdminInboxMessages
                .Include(m => m.Replies)
                .ThenInclude(r => r.FromUser)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.HotelId == hotel.Id);

            return msg != null ? MapToInboxItemDto(msg) : null;
        }

        public async Task ReplyToInboxMessageAsync(long userId, long messageId, string messageContent)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found");

            var msg = await _context.HotelAdminInboxMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.HotelId == hotel.Id);
            if (msg == null) throw new Exception("Message not found");

            var reply = new HotelAdminInboxReply
            {
                InboxMessageId = messageId,
                HotelId = hotel.Id,
                FromUserId = userId,
                ToAdminUserId = msg.AdminUserId,
                ReplyMessage = messageContent,
                CreatedAt = DateTime.UtcNow
            };

            // Update original message status
            msg.Status = InboxStatus.Read;
            msg.IsRead = true;
            msg.UpdatedAt = DateTime.UtcNow;

            _context.HotelAdminInboxReplies.Add(reply);
            await _context.SaveChangesAsync();
        }

        public async Task SendMessageToAdminAsync(long userId, string subject, string message, HotelToAdminCategory category)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found");

            var msg = new HotelToAdminMessage
            {
                HotelId = hotel.Id,
                FromUserId = userId,
                Category = category,
                Subject = subject,
                Message = message,
                Status = InboxStatus.Unread,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HotelToAdminMessages.Add(msg);
            await _context.SaveChangesAsync();
        }

        public async Task MarkInboxMessageAsReadAsync(long userId, long messageId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return;

            var msg = await _context.HotelAdminInboxMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.HotelId == hotel.Id);
            if (msg != null && !msg.IsRead)
            {
                msg.IsRead = true;
                msg.Status = msg.Status == InboxStatus.Unread ? InboxStatus.Read : msg.Status;
                msg.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResolveInboxMessageAsync(long userId, long messageId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return;

            var msg = await _context.HotelAdminInboxMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.HotelId == hotel.Id);
            if (msg != null)
            {
                msg.IsResolved = true;
                msg.Status = InboxStatus.Completed;
                msg.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private HotelInboxItemDto MapToInboxItemDto(HotelAdminInboxMessage m)
        {
            return new HotelInboxItemDto
            {
                Id = m.Id,
                Category = m.Category,
                Subject = m.Subject,
                Message = m.Message,
                Severity = m.Severity,
                SeverityLabel = m.Severity?.ToString(),
                SeverityColor = m.Severity switch
                {
                    InboxSeverity.High => "var(--red)",
                    InboxSeverity.Medium => "var(--amber)",
                    _ => "var(--green)"
                },
                RefType = m.RefType,
                RefId = m.RefId,
                Amount = m.Amount,
                ResolutionDate = m.ResolutionDate,
                ActionLabel = m.ActionLabel,
                Priority = m.Priority,
                PriorityLabel = m.Priority?.ToString(),
                Status = m.Status,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt,
                Replies = m.Replies.Select(r => new HotelInboxReplyDto
                {
                    Id = r.Id,
                    Message = r.ReplyMessage,
                    FromName = r.FromUser?.Name ?? "Hotel Staff",
                    IsFromAdmin = r.ToAdminUserId == null && r.FromUserId != m.Hotel.UserId, // Simplified check
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
        }

        public async Task<HotelDashboardOverviewDto> GetOverviewAsync(long userId, long? hotelId = null)
        {
            var hotel = await ResolveHotelAsync(userId, hotelId);
            if (hotel == null) throw new Exception("Hotel not found.");

            string accessMode = hotelId.HasValue ? "admin_readonly" : "owner";

            var now = DateTime.UtcNow;
            var today = now.Date;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            // Revenue calculation
            var bookings = await _context.HotelBookings
                .Where(b => b.HotelId == hotel.Id && b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            decimal totalRevenue = bookings.Sum(b => b.TotalPrice);
            
            decimal thisMonthRev = bookings
                .Where(b => b.CreatedAt >= firstDayThisMonth)
                .Sum(b => b.TotalPrice);

            decimal lastMonthRev = bookings
                .Where(b => b.CreatedAt >= firstDayLastMonth && b.CreatedAt < firstDayThisMonth)
                .Sum(b => b.TotalPrice);

            double changePercent = 0;
            if (lastMonthRev > 0)
            {
                changePercent = (double)((thisMonthRev - lastMonthRev) / lastMonthRev * 100);
            }
            else if (thisMonthRev > 0)
            {
                changePercent = 100;
            }

            // EXACT LOGIC FROM HotelService.SearchHotelsAsync
            var startDate = today;
            var endDate = today.AddDays(1);

            var bookedCounts = await _context.HotelBookingRooms
                .Where(br => br.Room.HotelId == hotel.Id &&
                             br.Booking.Status != BookingStatus.Cancelled &&
                             br.Booking.Status != BookingStatus.Completed &&
                             br.Booking.CheckInDate < endDate &&
                             startDate < br.Booking.CheckOutDate)
                .GroupBy(br => br.RoomId!.Value)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoomId, x => x.Count);

            int totalUnits = hotel.Rooms.Sum(r => r.Quantity);
            int calculatedAvailableToday = hotel.Rooms.Sum(r =>
            {
                var booked = bookedCounts.GetValueOrDefault(r.Id, 0);
                return Math.Max(0, r.Quantity - booked);
            });

            int calculatedOccupiedToday = totalUnits - calculatedAvailableToday;

            // Room Types Summary for Widgets
            var roomTypesSummary = hotel.Rooms.Select(r =>
            {
                var booked = bookedCounts.GetValueOrDefault(r.Id, 0);
                return new RoomTypeSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name.ToString(),
                    BedType = r.BedType.ToString() ?? "Standard",
                    Price = r.ROPrice ?? 0,
                    Quantity = r.Quantity,
                    Occupied = booked,
                    Available = Math.Max(0, r.Quantity - booked),
                    Occupancy = r.Occupancy ?? 2,
                    State = r.State.ToString(),
                    ROPrice = r.ROPrice,
                    BBPrice = r.BBPrice,
                    HBPrice = r.HBPrice,
                    FBPrice = r.FBPrice,
                    AIPrice = r.AIPrice
                };
            }).ToList();

            double occupancyRate = totalUnits > 0 ? (double)calculatedOccupiedToday / totalUnits * 100 : 0;

            // Guests calculation: Use active bookings for guest count
            var activeBookings = await _context.HotelBookings
                .Include(b => b.BookingRooms)
                .Where(b => b.HotelId == hotel.Id &&
                            b.Status != BookingStatus.Cancelled &&
                            b.Status != BookingStatus.Completed &&
                            b.CheckInDate < endDate &&
                            startDate < b.CheckOutDate)
                .ToListAsync();

            int totalGuests = activeBookings.Sum(b => b.BookingRooms.Count);

            // Recent Bookings: Order by Last Updated (or Created if never updated)
            var recentBookingsData = await _context.HotelBookings
                .Include(b => b.User)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Where(b => b.HotelId == hotel.Id)
                .OrderByDescending(b => b.UpdatedAt)
                .ThenByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentBookingsDto = recentBookingsData.Select(MapBookingToDto).ToList();

            // Latest Reviews
            var latestReviews = await _context.HotelReviews
                .Include(r => r.User)
                .Where(r => r.HotelId == hotel.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new HotelReviewDto
                {
                    Id = r.Id,
                    UserName = r.User.Name ?? r.User.UserName ?? "Guest",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new HotelDashboardOverviewDto
            {
                HotelName = hotel.HotelName,
                ManagerName = hotel.User?.Name ?? hotel.User?.UserName ?? "Manager",
                TotalRevenue = totalRevenue,
                RevenueChangePercent = Math.Round(changePercent, 1),
                OccupiedToday = calculatedOccupiedToday,
                OccupancyRateToday = Math.Round(occupancyRate, 1),
                AvailableToday = calculatedAvailableToday,
                TotalUnits = totalUnits,
                RoomTypesCount = hotel.Rooms.Count(),
                TotalGuests = totalGuests,
                RecentBookings = recentBookingsDto,
                RoomTypesSummary = roomTypesSummary,
                LatestReviews = latestReviews,
                AccessMode = accessMode
            };
        }

        private static BookingDto MapBookingToDto(HotelBooking booking)
        {
            var displayStatus = booking.Status;
            if (booking.PaymentStatus == PaymentStatus.Paid && displayStatus == BookingStatus.Pending)
            {
                displayStatus = BookingStatus.Confirmed;
            }

            var dto = new BookingDto
            {
                Id = booking.Id,
                HotelId = booking.HotelId,
                HotelName = booking.Hotel?.HotelName ?? "Unknown Hotel",
                PropertyType = "Hotel",
                CheckInDate = booking.CheckInDate ?? DateTime.MinValue,
                CheckOutDate = booking.CheckOutDate ?? DateTime.MinValue,
                Nights = booking.Nights ?? 0,
                TotalRooms = booking.TotalRooms,
                TotalPrice = booking.TotalPrice,
                PaymentStatus = booking.PaymentStatus.ToString(),
                Status = displayStatus.ToString(),
                CreatedAt = booking.CreatedAt,
                Rooms = booking.BookingRooms?.Select(br => new BookingRoomDto
                {
                    RoomId = br.RoomId ?? 0,
                    RoomName = br.RoomName ?? "Unknown Room",
                    BedType = br.Room?.BedType.ToString() ?? "Standard",
                    MealPlan = br.MealPlan,
                    PricePerNight = br.PricePerNight ?? 0,
                    Nights = br.Nights,
                    Subtotal = br.Subtotal
                }).ToList() ?? new List<BookingRoomDto>()
            };
            if (booking.User != null)
            {
                dto.User = new UserSummaryDto
                {
                    Id = booking.User.Id,
                    Name = booking.User.Name ?? booking.User.UserName ?? "Guest",
                    Email = booking.User.Email ?? "",
                    PhoneNumber = null
                };
            }
            return dto;
        }

        public async Task<List<DashboardBookingItemDto>> GetDashboardBookingsAsync(long userId, string? status, DateTime? from, DateTime? to, string? bedType, string? guestName, string? bookingId, DateTime? checkIn = null, DateTime? checkOut = null, long? hotelId = null)
        {
            var hotel = await ResolveHotelAsync(userId, hotelId);

            if (hotel == null) return new List<DashboardBookingItemDto>();

            var query = _context.HotelBookings
                .Include(b => b.User)
                .Include(b => b.BookingRooms)
                    .ThenInclude(br => br.Room)
                .Include(b => b.Payments)
                .Where(b => b.HotelId == hotel.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                if (status.ToLower() == "complete" || status.ToLower() == "completed")
                {
                    query = query.Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed);
                }
                else if (status.ToLower() == "pending")
                {
                    query = query.Where(b => b.Status == BookingStatus.Pending);
                }
                else if (Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(b => b.Status == parsedStatus);
                }
            }

            if (from.HasValue)
            {
                query = query.Where(b => b.CheckInDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(b => b.CheckInDate <= to.Value.Date);
            }

            if (checkIn.HasValue)
            {
                query = query.Where(b => b.CheckInDate.HasValue && b.CheckInDate.Value.Date == checkIn.Value.Date);
            }

            if (checkOut.HasValue)
            {
                query = query.Where(b => b.CheckOutDate.HasValue && b.CheckOutDate.Value.Date == checkOut.Value.Date);
            }

            if (!string.IsNullOrEmpty(bedType) && bedType.ToLower() != "all")
            {
                var searchPattern = bedType.ToLower();
                
                if (Enum.TryParse<BedType>(bedType, true, out var parsedBedType))
                {
                    query = query.Where(b => b.BookingRooms.Any(br => 
                        (br.Room != null && br.Room.BedType == parsedBedType) ||
                        (br.RoomName != null && br.RoomName.ToLower().Contains(searchPattern))
                    ));
                }
                else 
                {
                    query = query.Where(b => b.BookingRooms.Any(br => 
                        br.RoomName != null && br.RoomName.ToLower().Contains(searchPattern)
                    ));
                }
            }
            
            if (!string.IsNullOrEmpty(guestName))
            {
                var pattern = guestName.ToLower().Trim();
                query = query.Where(b => 
                    (b.User != null && (
                        (b.User.Name != null && b.User.Name.ToLower().Contains(pattern)) || 
                        (b.User.UserName != null && b.User.UserName.ToLower().Contains(pattern)) ||
                        (b.User.Email != null && b.User.Email.ToLower().Contains(pattern))
                    ))
                );
            }

            if (!string.IsNullOrEmpty(bookingId))
            {
                var idStr = bookingId.ToUpper().Replace("B", "").Trim();
                if (long.TryParse(idStr, out long idValue))
                {
                    query = query.Where(b => b.Id == idValue);
                }
            }

            var bookingsData = await query
                .OrderByDescending(b => b.UpdatedAt)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            var colors = new[] { "#5B8DEF", "#3ECFA0", "#D4A853", "#7C6EF0", "#F5A623", "#F06565", "#0891B2", "#A16207" };

            var result = bookingsData.Select((b, index) => 
            {
                var successfulPayment = b.Payments.FirstOrDefault(p => p.Status == HotelPaymentStatus.Paid);
                bool isPaid = successfulPayment != null;
                
                return new DashboardBookingItemDto
                {
                    Id = $"B{b.Id:D3}",
                    GuestName = b.User?.Name ?? b.User?.UserName ?? "Guest",
                    GuestEmail = b.User?.Email ?? "info@email.com",
                    TotalRooms = b.TotalRooms,
                    RoomTypes = string.Join(", ", b.BookingRooms.Select(br => br.RoomName)),
                    RoomDetails = string.Join(", ", b.BookingRooms
                        .GroupBy(br => br.Room?.BedType.ToString() ?? "Standard")
                        .Select(g => $"{g.Count()} {g.Key}")),
                    RoomNames = b.BookingRooms.Select(br => br.RoomName ?? "").ToList(),
                    BookingDate = b.CreatedAt.ToString("MMM dd"),
                    CheckIn = b.CheckInDate?.ToString("MMM dd") ?? "--",
                    CheckOut = b.CheckOutDate?.ToString("MMM dd") ?? "--",
                    TotalPaid = isPaid ? successfulPayment.Amount : 0,
                    Method = isPaid ? successfulPayment.PaymentMethod.ToString() : "--",
                    TransactionId = isPaid ? successfulPayment.TransactionId : "--",
                    PaymentDate = isPaid ? successfulPayment.PaidAt?.ToString("yyyy-MM-dd") ?? "--" : "--",
                    Status = (b.PaymentStatus == PaymentStatus.Paid && b.Status == BookingStatus.Pending) 
                             ? "confirmed" 
                             : b.Status.ToString().ToLower(),
                    ColorCode = colors[index % colors.Length],
                    
                    CancellationFee = b.CancellationFee,
                    RefundAmount = b.RefundAmount,
                    CancellationDate = b.CancelledAt?.ToString("MMM dd, HH:mm") ?? "--",
                    CancellationReason = b.CancellationReason ?? ""
                };
            }).ToList();

            return result;
        }

        public async Task UpdateRoomConfigAsync(long userId, List<RoomTypeSummaryDto> rooms, List<long> deletedIds)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (hotel == null) throw new Exception("Hotel not found.");

            // 1. Delete rooms
            if (deletedIds != null && deletedIds.Any())
            {
                var roomsToDelete = hotel.Rooms.Where(r => deletedIds.Contains(r.Id)).ToList();
                _context.HotelRooms.RemoveRange(roomsToDelete);
            }

            // 2. Update existing or add new
            foreach (var rDto in rooms)
            {
                if (rDto.Id > 0)
                {
                    var existing = hotel.Rooms.FirstOrDefault(r => r.Id == rDto.Id);
                    if (existing != null)
                    {
                        existing.Name = Enum.TryParse<HotelRoomName>(rDto.Name.Replace(" ", ""), true, out var rn) ? rn : HotelRoomName.StandardRoom;
                        existing.BedType = Enum.TryParse<BedType>(rDto.BedType.Split(' ')[0], true, out var bt) ? bt : BedType.Single;
                        existing.Quantity = rDto.Quantity;
                        existing.Occupancy = rDto.Occupancy;
                        existing.ROPrice = rDto.ROPrice;
                        existing.BBPrice = rDto.BBPrice;
                        existing.HBPrice = rDto.HBPrice;
                        existing.FBPrice = rDto.FBPrice;
                        existing.AIPrice = rDto.AIPrice;
                        existing.State = Enum.TryParse<RoomState>(rDto.State, true, out var st) ? st : RoomState.Available;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    hotel.Rooms.Add(new HotelRoom
                    {
                        HotelId = hotel.Id,
                        Name = Enum.TryParse<HotelRoomName>(rDto.Name.Replace(" ", ""), true, out var rn) ? rn : HotelRoomName.StandardRoom,
                        BedType = Enum.TryParse<BedType>(rDto.BedType.Split(' ')[0], true, out var bt) ? bt : BedType.Single,
                        Quantity = rDto.Quantity,
                        Occupancy = rDto.Occupancy,
                        ROPrice = rDto.ROPrice,
                        BBPrice = rDto.BBPrice,
                        HBPrice = rDto.HBPrice,
                        FBPrice = rDto.FBPrice,
                        AIPrice = rDto.AIPrice,
                        State = Enum.TryParse<RoomState>(rDto.State, true, out var st) ? st : RoomState.Available,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<HotelReviewsResponse> GetHotelReviewsAsync(long userId, string? datePreset, int? starRating, int page, DateTime? startDate = null, long? hotelId = null)
        {
            var hotel = await ResolveHotelAsync(userId, hotelId);
            if (hotel == null) return new HotelReviewsResponse();

            var query = _context.HotelReviews
                .Include(r => r.User)
                .Where(r => r.HotelId == hotel.Id)
                .AsQueryable();

            // 1. Calculate Overall Summary (before paging & filtering)
            var allReviews = await _context.HotelReviews.Where(r => r.HotelId == hotel.Id).ToListAsync();
            var avgRating = allReviews.Any() ? (decimal)allReviews.Average(r => r.Rating) : 0;
            var totalCount = allReviews.Count;

            // 2. Apply Filters
            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt.Date >= startDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(datePreset) && datePreset.ToLower() != "all")
            {
                var now = DateTime.UtcNow;
                query = datePreset.ToLower() switch
                {
                    "today" => query.Where(r => r.CreatedAt.Date == now.Date),
                    "lastweek" => query.Where(r => r.CreatedAt >= now.AddDays(-7)),
                    "lastmonth" => query.Where(r => r.CreatedAt >= now.AddMonths(-1)),
                    "last3months" => query.Where(r => r.CreatedAt >= now.AddMonths(-3)),
                    "last6months" => query.Where(r => r.CreatedAt >= now.AddMonths(-6)),
                    "last9months" => query.Where(r => r.CreatedAt >= now.AddMonths(-9)),
                    "lastyear" => query.Where(r => r.CreatedAt >= now.AddYears(-1)),
                    _ => query
                };
            }

            if (starRating.HasValue && starRating > 0)
            {
                query = query.Where(r => r.Rating == starRating.Value);
            }

            // 3. Paging
            int pageSize = 20;
            int filteredTotal = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)filteredTotal / pageSize);
            
            var pagedReviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new HotelReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name ?? r.User.UserName ?? "Guest",
                    UserProfilePicture = r.User.ProfilePic,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    TimeAgo = GetTimeAgo(r.CreatedAt)
                })
                .ToListAsync();

            return new HotelReviewsResponse
            {
                Reviews = pagedReviews,
                AvgRating = Math.Round(avgRating, 1),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, totalPages)
            };
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}mo ago";
            return $"{(int)(span.TotalDays / 365)}y ago";
        }

        public async Task SubmitProfileUpdateAsync(long userId, HotelProfileUpdateRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found");

            if (request.Images != null)
            {
                foreach (var img in request.Images)
                {
                    if (img.ImageFile != null)
                        img.ImageUrl = await _fileService.SaveHotelImageAsync(img.ImageFile);
                }
            }

            var pending = await _context.HotelPendingProfiles.FirstOrDefaultAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending);
            if (pending == null) {
                pending = new HotelPendingProfile { HotelId = hotel.Id };
                _context.HotelPendingProfiles.Add(pending);
            }
            
            pending.RequestedByUserId = userId;
            pending.HotelName = request.HotelName;
            pending.Description = request.Description;
            pending.Country = request.Country;
            pending.Governorate = request.Governorate;
            pending.CityArea = request.CityArea;
            pending.AddressDetails = request.AddressDetails;
            pending.StarRating = request.StarRating;
            pending.PropertyType = request.PropertyType;
            pending.AccommodationType = request.AccommodationType;
            
            pending.AmenitiesJson = JsonSerializer.Serialize(request.AmenityIds, _jsonOptions);
            pending.DynamicFieldsJson = JsonSerializer.Serialize(request.DynamicFields, _jsonOptions);
            pending.ImagesJson = JsonSerializer.Serialize(request.Images, _jsonOptions);
            pending.RoomsJson = JsonSerializer.Serialize(request.Rooms, _jsonOptions);
            pending.ContactsJson = JsonSerializer.Serialize(request.Contacts, _jsonOptions);
            pending.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task SubmitPolicyUpdateAsync(long userId, HotelPolicyUpdateRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found");

            var pending = await _context.HotelPendingPolicies.Include(p => p.CancellationRules).FirstOrDefaultAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending);
            if (pending == null) {
                pending = new HotelPendingPolicy { HotelId = hotel.Id };
                _context.HotelPendingPolicies.Add(pending);
            } else {
                _context.HotelPendingCancellationRules.RemoveRange(pending.CancellationRules);
            }
            
            pending.RequestedByUserId = userId;
            pending.ServiceChargePct = request.ServiceChargePct;
            pending.IncludeServiceCharge = request.IncludeServiceCharge;
            pending.IncludeVat = request.IncludeVat;
            pending.IncludeCityTax = request.IncludeCityTax;
            pending.CancellationStrategy = request.CancellationStrategy;
            pending.CreatedAt = DateTime.UtcNow;

            if (request.CancellationRules != null) {
                foreach(var r in request.CancellationRules) {
                    pending.CancellationRules.Add(new HotelPendingCancellationRule {
                        FromHoursBeforeCheckIn = r.FromHoursBeforeCheckIn,
                        ToHoursBeforeCheckIn = r.ToHoursBeforeCheckIn,
                        PenaltyPct = r.PenaltyPct
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task SubmitLegalUpdateAsync(long userId, HotelLegalUpdateRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found");

            if (request.Documents != null)
            {
                foreach (var doc in request.Documents)
                {
                    if (doc.File != null)
                        doc.FileUrl = await _fileService.SaveHotelDocumentAsync(doc.File);

                    var pendingDoc = new HotelPendingLegalDocument {
                        HotelId = hotel.Id,
                        DocumentTypeId = doc.DocumentTypeId,
                        FileUrl = doc.FileUrl ?? "",
                        Notes = doc.Notes,
                        RequestedByUserId = userId,
                        Status = PendingRequestStatus.Pending,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.HotelPendingLegalDocuments.Add(pendingDoc);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetPendingConfigSectionsAsync(long userId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return new List<string>();

            var sections = new List<string>();

            if (await _context.HotelPendingProfiles.AnyAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending))
                sections.Add("Profile");

            if (await _context.HotelPendingPolicies.AnyAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending))
                sections.Add("Policy");

            if (await _context.HotelPendingLegalDocuments.AnyAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending))
                sections.Add("Legal");

            return sections;
        }

        public async Task<HotelDetailsDto?> GetPendingApplicationAsync(long userId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return null;

            // Get current active details
            using var scope = _serviceProvider.CreateScope();
            var hotelService = scope.ServiceProvider.GetRequiredService<IHotelService>();
            var dto = await hotelService.GetMyApplicationStatusAsync(userId);
            if (dto == null) return null;

            // Overlay Pending Profile if exists
            var pendingProfile = await _context.HotelPendingProfiles.FirstOrDefaultAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending);
            if (pendingProfile != null)
            {
                dto.HotelName = pendingProfile.HotelName ?? dto.HotelName;
                dto.StarRating = pendingProfile.StarRating ?? dto.StarRating;
                if (pendingProfile.PropertyType.HasValue) dto.PropertyType = pendingProfile.PropertyType.Value.ToString();
                if (pendingProfile.AccommodationType.HasValue) dto.AccommodationType = pendingProfile.AccommodationType.Value.ToString();
                dto.Description = pendingProfile.Description ?? dto.Description;
                dto.Governorate = pendingProfile.Governorate ?? dto.Governorate;
                dto.CityArea = pendingProfile.CityArea ?? dto.CityArea;
                dto.AddressDetails = pendingProfile.AddressDetails ?? dto.AddressDetails;
                
                if (!string.IsNullOrEmpty(pendingProfile.AmenitiesJson))
                {
                    var amenityIds = System.Text.Json.JsonSerializer.Deserialize<List<long>>(pendingProfile.AmenitiesJson);
                    if (amenityIds != null)
                    {
                        var requestedAmenities = await _context.Amenities.Where(a => amenityIds.Contains(a.Id)).ToListAsync();
                        dto.AmenityNames = requestedAmenities.Select(a => a.Name).ToList();
                    }
                }

                if (!string.IsNullOrEmpty(pendingProfile.RoomsJson))
                {
                    var rooms = System.Text.Json.JsonSerializer.Deserialize<List<TravAi.DTOs.Hotel.CreateRoomRequest>>(pendingProfile.RoomsJson, _jsonOptions);
                    if (rooms != null)
                        dto.Rooms = rooms.Select(r => new RoomDto { Name = r.Name, Occupancy = r.Occupancy ?? 0, Quantity = r.Quantity ?? 0, BedType = r.BedType }).ToList();
                }

                if (!string.IsNullOrEmpty(pendingProfile.DynamicFieldsJson))
                {
                    var dFields = System.Text.Json.JsonSerializer.Deserialize<List<TravAi.DTOs.Hotel.HotelFieldValueInputDto>>(pendingProfile.DynamicFieldsJson, _jsonOptions);
                    if (dFields != null)
                        dto.DynamicFields = dFields.Select(f => new HotelDynamicFieldValueDto { FieldDefinitionId = f.FieldDefinitionId, Value = f.Value }).ToList();
                }

                if (!string.IsNullOrEmpty(pendingProfile.ContactsJson))
                {
                    var contacts = System.Text.Json.JsonSerializer.Deserialize<List<TravAi.DTOs.Hotel.HotelContactInputDto>>(pendingProfile.ContactsJson, _jsonOptions);
                    if (contacts != null)
                        dto.Contacts = contacts.Select(c => new HotelContactDto { ContactType = c.ContactType.ToString(), ContactValue = c.ContactValue }).ToList();
                }
            }

            // Overlay Pending Policy if exists
            var pendingPolicy = await _context.HotelPendingPolicies.Include(p => p.CancellationRules).FirstOrDefaultAsync(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending);
            if (pendingPolicy != null)
            {
                dto.Policy = new HotelPolicyDto
                {
                    ServiceChargePct = pendingPolicy.ServiceChargePct ?? (dto.Policy?.ServiceChargePct ?? 0),
                    IncludeVat = pendingPolicy.IncludeVat ?? (dto.Policy?.IncludeVat ?? false),
                    IncludeCityTax = pendingPolicy.IncludeCityTax ?? (dto.Policy?.IncludeCityTax ?? false),
                    IncludeServiceCharge = pendingPolicy.IncludeServiceCharge ?? (dto.Policy?.IncludeServiceCharge ?? false),
                    CancellationStrategy = pendingPolicy.CancellationStrategy.HasValue ? pendingPolicy.CancellationStrategy.Value.ToString() : (dto.Policy?.CancellationStrategy ?? ""),
                    CancellationRules = pendingPolicy.CancellationRules.Select(r => new HotelCancellationRuleDto { 
                        FromHoursBeforeCheckIn = r.FromHoursBeforeCheckIn, 
                        ToHoursBeforeCheckIn = r.ToHoursBeforeCheckIn, 
                        PenaltyPct = r.PenaltyPct 
                    }).ToList()
                };
            }

            var pendingLegal = await _context.HotelPendingLegalDocuments.Where(p => p.HotelId == hotel.Id && p.Status == PendingRequestStatus.Pending).ToListAsync();
            if (pendingLegal.Any())
            {
                var legalDtos = new List<HotelDocumentDto>();
                foreach (var pd in pendingLegal)
                {
                    legalDtos.Add(new HotelDocumentDto {
                        DocumentTypeId = pd.DocumentTypeId,
                        FileUrl = pd.FileUrl
                    });
                }
                dto.Documents = legalDtos;
            }

            return dto;
        }
    }
}
