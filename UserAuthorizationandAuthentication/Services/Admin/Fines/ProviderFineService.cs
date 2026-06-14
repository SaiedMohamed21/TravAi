using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;
using TravAi.DTOs.Admin.Fines;
using TravAi.Models.Admin;
using TravAi.Models.Enums;
using TravAi.TourGuide.Models.Enums;

namespace TravAi.Services.Admin.Fines
{
    public class ProviderFineService : IProviderFineService
    {
        private readonly ApplicationDbContext _context;

        public ProviderFineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProviderFineListItemDto>> GetFinesAsync(ProviderFineFilterDto filter)
        {
            var query = _context.ProviderFines
                .Include(f => f.CreatedByAdminUser)
                .AsQueryable();

            if (filter.FineId.HasValue)
                query = query.Where(f => f.Id == filter.FineId.Value);

            if (filter.CreatedAt.HasValue)
            {
                var targetDate = filter.CreatedAt.Value.Date;
                query = query.Where(f => f.CreatedAt.Date == targetDate);
            }

            if (filter.ProviderType.HasValue)
                query = query.Where(f => f.ProviderType == filter.ProviderType.Value);

            if (filter.ProviderId.HasValue)
                query = query.Where(f => f.ProviderId == filter.ProviderId.Value);

            if (filter.Status.HasValue)
                query = query.Where(f => f.Status == filter.Status.Value);

            if (filter.SourceType.HasValue)
                query = query.Where(f => f.SourceType == filter.SourceType.Value);

            if (filter.ComplaintId.HasValue)
                query = query.Where(f => f.ComplaintId == filter.ComplaintId.Value);

            if (filter.BookingId.HasValue)
                query = query.Where(f => f.HotelBookingId == filter.BookingId.Value || 
                                         f.TourBookingId == filter.BookingId.Value || 
                                         f.AirlineBookingId == filter.BookingId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(f => f.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(f => f.CreatedAt <= filter.ToDate.Value);

            if (filter.AmountFrom.HasValue)
                query = query.Where(f => f.Amount >= filter.AmountFrom.Value);

            if (filter.AmountTo.HasValue)
                query = query.Where(f => f.Amount <= filter.AmountTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.Currency))
            {
                var cur = filter.Currency.ToLower();
                query = query.Where(f => f.Currency.ToLower() == cur);
            }

            if (!string.IsNullOrWhiteSpace(filter.Reason))
            {
                var reason = filter.Reason.ToLower();
                query = query.Where(f => f.Reason.ToLower().Contains(reason));
            }

            if (!string.IsNullOrWhiteSpace(filter.CreatedByAdminName))
            {
                var creator = filter.CreatedByAdminName.ToLower();
                query = query.Where(f => f.CreatedByAdminUser.UserName.ToLower().Contains(creator) || (f.CreatedByAdminUser.Email != null && f.CreatedByAdminUser.Email.ToLower().Contains(creator)));
            }

            var fines = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

            var tourBookingIds = fines.Where(f => f.SourceType == ProviderFineSourceType.TourGuideCancellation && f.TourBookingId.HasValue).Select(f => f.TourBookingId.Value).Distinct().ToList();
            var tourBookings = tourBookingIds.Any() ? await _context.TourBookings.Include(b => b.Tour).Include(b => b.TourGuide).Where(b => tourBookingIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id) : new Dictionary<long, TravAi.TourGuide.Models.TourBooking>();

            var result = new List<ProviderFineListItemDto>();
            foreach (var f in fines)
            {
                var dto = new ProviderFineListItemDto
                {
                    Id = f.Id,
                    ProviderType = f.ProviderType,
                    ProviderId = f.ProviderId,
                    SourceType = f.SourceType,
                    ComplaintId = f.ComplaintId,
                    RelatedBookingId = f.HotelBookingId ?? f.TourBookingId ?? f.AirlineBookingId,
                    Amount = f.Amount,
                    Currency = f.Currency,
                    Reason = f.Reason,
                    Status = f.Status,
                    CreatedAt = f.CreatedAt,
                    CreatedByAdminName = f.CreatedByAdminUser?.UserName ?? "Admin"
                };

                // Fetch provider display name
                if (f.ProviderType == ProviderType.Hotel)
                {
                    var hotel = await _context.Hotels.FindAsync(f.ProviderId);
                    dto.ProviderDisplayName = hotel?.HotelName ?? $"Hotel #{f.ProviderId}";
                }
                else if (f.ProviderType == ProviderType.TourGuide)
                {
                    var tg = await _context.TourGuides.FindAsync(f.ProviderId);
                    dto.ProviderDisplayName = tg?.Name ?? $"Tour Guide #{f.ProviderId}";
                }
                else if (f.ProviderType == ProviderType.Airline)
                {
                    var airlineName = await _context.Airlines
                        .Where(a => a.Id == f.ProviderId)
                        .Select(a => (string?)a.Name)
                        .FirstOrDefaultAsync();
                    dto.ProviderDisplayName = !string.IsNullOrWhiteSpace(airlineName) ? airlineName : $"Airline #{f.ProviderId}";
                }

                if (f.SourceType == ProviderFineSourceType.TourGuideCancellation && f.TourBookingId.HasValue && tourBookings.TryGetValue(f.TourBookingId.Value, out var tb))
                {
                    dto.TourBookingId = tb.Id;
                    dto.TourId = tb.TourId;
                    dto.TourTitle = tb.Tour?.TourTitle;
                    dto.TourDate = tb.TourDate;
                    dto.ParticipantsCount = tb.ParticipantsCount;
                    dto.TotalPrice = tb.TotalPrice;
                    dto.TotalPaid = tb.PaymentStatus == TravAi.TourGuide.Models.Enums.PaymentStatus.Completed ? tb.TotalPrice : 0;
                    dto.TicketPricePerParticipant = tb.ParticipantsCount > 0 ? tb.TotalPrice / tb.ParticipantsCount : null;
                    dto.TourGuideId = tb.TourGuideId;
                    dto.TourGuideName = tb.TourGuide?.Name;
                    dto.CancellationReason = tb.CancellationReason;
                    dto.CancellationReviewStatus = tb.CancellationReviewStatus;
                }

                result.Add(dto);
            }

            if (!string.IsNullOrWhiteSpace(filter.ProviderName))
            {
                var pName = filter.ProviderName.ToLower();
                result = result.Where(r => r.ProviderDisplayName.ToLower().Contains(pName)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower();
                result = result.Where(r => r.ProviderDisplayName.ToLower().Contains(term) ||
                                           r.Reason.ToLower().Contains(term) ||
                                           r.Id.ToString() == term ||
                                           r.ProviderId.ToString() == term ||
                                           (r.ComplaintId != null && r.ComplaintId.ToString() == term) ||
                                           (r.RelatedBookingId != null && r.RelatedBookingId.ToString() == term) ||
                                           r.CreatedByAdminName.ToLower().Contains(term)).ToList();
            }

            return result;
        }

        public async Task<ProviderFineDetailsDto?> GetFineDetailsAsync(long id)
        {
            var fine = await _context.ProviderFines
                .Include(f => f.CreatedByAdminUser)
                .Include(f => f.CancelledByAdminUser)
                .Include(f => f.Complaint)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fine == null) return null;

            var dto = new ProviderFineDetailsDto
            {
                Id = fine.Id,
                ProviderType = fine.ProviderType,
                ProviderId = fine.ProviderId,
                SourceType = fine.SourceType,
                ComplaintId = fine.ComplaintId,
                RelatedBookingId = fine.HotelBookingId ?? fine.TourBookingId ?? fine.AirlineBookingId,
                Amount = fine.Amount,
                Currency = fine.Currency,
                Reason = fine.Reason,
                Status = fine.Status,
                CreatedAt = fine.CreatedAt,
                CreatedByAdminName = fine.CreatedByAdminUser?.UserName ?? "Admin",
                AdminNotes = fine.AdminNotes,
                CancelledAt = fine.CancelledAt,
                CancellationReason = fine.CancellationReason,
                CancelledByAdminName = fine.CancelledByAdminUser?.UserName
            };

            // Provider Details
            if (fine.ProviderType == ProviderType.Hotel)
            {
                var hotel = await _context.Hotels.FindAsync(fine.ProviderId);
                dto.ProviderDisplayName = hotel?.HotelName ?? $"Hotel #{fine.ProviderId}";
                dto.ProviderDetails = new { Id = hotel?.Id, Name = hotel?.HotelName };
            }
            else if (fine.ProviderType == ProviderType.TourGuide)
            {
                var tg = await _context.TourGuides.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == fine.ProviderId);
                dto.ProviderDisplayName = tg?.Name ?? $"Tour Guide #{fine.ProviderId}";
                dto.ProviderDetails = new { Id = tg?.Id, Name = tg?.Name, Email = tg?.User?.Email };
            }
            else if (fine.ProviderType == ProviderType.Airline)
            {
                var airlineData = await _context.Airlines
                    .Where(a => a.Id == fine.ProviderId)
                    .Select(a => new { a.Id, Name = (string?)a.Name, LicenseNumber = (string?)a.LicenseNumber })
                    .FirstOrDefaultAsync();

                if (airlineData != null)
                {
                    dto.ProviderDisplayName = !string.IsNullOrWhiteSpace(airlineData.Name) ? airlineData.Name : $"Airline #{airlineData.Id}";
                    dto.ProviderDetails = new { Id = airlineData.Id, Name = airlineData.Name, Code = airlineData.LicenseNumber };
                }
                else
                {
                    dto.ProviderDisplayName = $"Airline #{fine.ProviderId}";
                    dto.ProviderDetails = new { Id = fine.ProviderId, Name = "Unknown", Code = "Unknown" };
                }
            }

            // Complaint Summary
            if (fine.Complaint != null)
            {
                dto.ComplaintSummary = new
                {
                    Id = fine.Complaint.Id,
                    Subject = fine.Complaint.Subject,
                    Status = fine.Complaint.Status.ToString(),
                    Priority = fine.Complaint.Priority.ToString(),
                    CreatedAt = fine.Complaint.CreatedAt
                };
            }

            // Booking Summary
            if (fine.HotelBookingId.HasValue)
            {
                var b = await _context.HotelBookings.FindAsync(fine.HotelBookingId.Value);
                if (b != null) dto.BookingSummary = new { Id = b.Id, Date = b.CreatedAt, TotalPrice = b.TotalPrice };
            }
            else if (fine.TourBookingId.HasValue)
            {
                var b = await _context.TourBookings.Include(t => t.Tour).Include(t => t.TourGuide).FirstOrDefaultAsync(t => t.Id == fine.TourBookingId.Value);
                if (b != null) 
                {
                    dto.BookingSummary = new { Id = b.Id, Date = b.CreatedAt, TotalPrice = b.TotalPrice };
                    
                    if (fine.SourceType == ProviderFineSourceType.TourGuideCancellation)
                    {
                        dto.TourBookingId = b.Id;
                        dto.TourId = b.TourId;
                        dto.TourTitle = b.Tour?.TourTitle;
                        dto.TourDate = b.TourDate;
                        dto.ParticipantsCount = b.ParticipantsCount;
                        dto.TotalPrice = b.TotalPrice;
                        dto.TotalPaid = b.PaymentStatus == TravAi.TourGuide.Models.Enums.PaymentStatus.Completed ? b.TotalPrice : 0;
                        dto.TicketPricePerParticipant = b.ParticipantsCount > 0 ? b.TotalPrice / b.ParticipantsCount : null;
                        dto.TourGuideId = b.TourGuideId;
                        dto.TourGuideName = b.TourGuide?.Name;
                        dto.CancellationReason = b.CancellationReason;
                        dto.CancellationReviewStatus = b.CancellationReviewStatus;
                    }
                }
            }
            else if (fine.AirlineBookingId.HasValue)
            {
                var bData = await _context.Bookings
                    .Where(b => b.Id == fine.AirlineBookingId.Value)
                    .Select(b => new { b.Id, Date = (DateTime?)b.BookingDate, TotalPrice = (decimal?)b.TotalPrice })
                    .FirstOrDefaultAsync();

                if (bData != null) dto.BookingSummary = new { Id = bData.Id, Date = bData.Date ?? DateTime.UtcNow, TotalPrice = bData.TotalPrice ?? 0 };
            }

            return dto;
        }

        public async Task<ProviderFineDetailsDto> CreateFineAsync(CreateProviderFineDto dto, long adminUserId)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Reason is required.");

            var fine = new ProviderFine
            {
                ProviderType = dto.ProviderType,
                SourceType = dto.SourceType,
                Amount = dto.Amount,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency,
                Reason = dto.Reason,
                AdminNotes = dto.AdminNotes,
                CreatedByAdminUserId = adminUserId,
                Status = ProviderFineStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.SourceType == ProviderFineSourceType.Complaint)
            {
                if (!dto.ComplaintId.HasValue)
                    throw new ArgumentException("ComplaintId is required for Complaint source type.");

                var complaint = await _context.Complaints
                    .Include(c => c.Booking)
                    .Include(c => c.TourBooking)
                    .FirstOrDefaultAsync(c => c.Id == dto.ComplaintId.Value);

                if (complaint == null)
                    throw new ArgumentException("Complaint not found.");

                fine.ComplaintId = complaint.Id;

                if (dto.ProviderType == ProviderType.Hotel)
                {
                    if (complaint.ComplaintType != ComplaintType.Hotel)
                        throw new ArgumentException("Complaint type must be Hotel.");
                    
                    if (complaint.HotelId.HasValue)
                        fine.ProviderId = complaint.HotelId.Value;
                    else if (complaint.Booking != null)
                        fine.ProviderId = complaint.Booking.HotelId;
                    else
                        throw new ArgumentException("Complaint is not linked to a hotel provider.");

                    fine.HotelBookingId = complaint.BookingId;
                }
                else if (dto.ProviderType == ProviderType.Airline)
                {
                    if (complaint.ComplaintType != ComplaintType.Airline)
                        throw new ArgumentException("Complaint type must be Airline.");
                    
                    if (complaint.AirlineBookingId.HasValue)
                    {
                        var flightData = await _context.Bookings
                            .Where(b => b.Id == complaint.AirlineBookingId.Value)
                            .Select(b => new { AirlineId = b.Flight != null ? (long?)b.Flight.AirlineId : null })
                            .FirstOrDefaultAsync();

                        if (flightData == null || flightData.AirlineId == null)
                            throw new ArgumentException("Could not determine Airline provider from booking.");

                        fine.ProviderId = flightData.AirlineId.Value;
                        fine.AirlineBookingId = complaint.AirlineBookingId.Value;
                    }
                    else
                    {
                        throw new ArgumentException("Complaint is not linked to an airline booking.");
                    }
                }
                else if (dto.ProviderType == ProviderType.TourGuide)
                {
                    if (complaint.ComplaintType != ComplaintType.Tour)
                        throw new ArgumentException("Complaint type must be Tour.");
                    
                    if (complaint.TourBooking == null)
                    {
                        var tBooking = await _context.TourBookings.FirstOrDefaultAsync(b => b.Id == complaint.TourBookingId);
                        if (tBooking == null)
                            throw new ArgumentException("Complaint is not linked to a tour booking provider.");
                        fine.ProviderId = tBooking.TourGuideId;
                        fine.TourBookingId = tBooking.Id;
                    }
                    else
                    {
                        fine.ProviderId = complaint.TourBooking.TourGuideId;
                        fine.TourBookingId = complaint.TourBooking.Id;
                    }
                }

                // Check for duplicate
                var existing = await _context.ProviderFines.FirstOrDefaultAsync(f => 
                    f.Status == ProviderFineStatus.Active && 
                    f.SourceType == ProviderFineSourceType.Complaint &&
                    f.ComplaintId == complaint.Id &&
                    f.ProviderId == fine.ProviderId);
                
                if (existing != null)
                    throw new InvalidOperationException("An active fine already exists for this source.");
            }
            else if (dto.SourceType == ProviderFineSourceType.TourGuideCancellation)
            {
                if (dto.ProviderType != ProviderType.TourGuide)
                    throw new ArgumentException("TourGuideCancellation source type is only valid for TourGuide providers.");
                
                if (!dto.TourBookingId.HasValue)
                    throw new ArgumentException("TourBookingId is required for TourGuideCancellation source type.");

                var booking = await _context.TourBookings.FindAsync(dto.TourBookingId.Value);
                if (booking == null)
                    throw new ArgumentException("Tour booking not found.");
                
                if (booking.Status != TravAi.TourGuide.Models.Enums.BookingStatus.Cancelled)
                    throw new ArgumentException("Tour booking is not cancelled.");
                
                if (string.IsNullOrEmpty(booking.CancelledByRole))
                    throw new ArgumentException("This tour booking is cancelled, but the system cannot confirm it was cancelled by the tour guide. Please use a complaint source or Others source.");
                
                if (booking.CancelledByRole != "TourGuide")
                    throw new ArgumentException("Tour booking was not cancelled by the TourGuide. Fines can only be applied if the provider cancelled.");

                if (booking.CancellationReviewStatus != "Rejected")
                    throw new ArgumentException("Tour booking was cancelled by TourGuide, but the admin has not rejected the excuse/reason yet. Fine cannot be applied.");
                
                fine.ProviderId = booking.TourGuideId;
                fine.TourBookingId = booking.Id;

                // Check duplicate
                var existing = await _context.ProviderFines.FirstOrDefaultAsync(f => 
                    f.Status == ProviderFineStatus.Active && 
                    f.SourceType == ProviderFineSourceType.TourGuideCancellation &&
                    f.TourBookingId == booking.Id &&
                    f.ProviderId == fine.ProviderId);
                
                if (existing != null)
                    throw new InvalidOperationException("An active fine already exists for this source.");
            }
            else if (dto.SourceType == ProviderFineSourceType.Others)
            {
                if (!dto.ProviderId.HasValue)
                    throw new ArgumentException("ProviderId is required for Others source type.");

                fine.ProviderId = dto.ProviderId.Value;
                fine.ComplaintId = null;
                fine.TourBookingId = null;
                fine.HotelBookingId = null;
                fine.AirlineBookingId = null;

                // Validate provider exists
                if (dto.ProviderType == ProviderType.Hotel)
                {
                    if (!await _context.Hotels.AnyAsync(h => h.Id == fine.ProviderId))
                        throw new ArgumentException("Hotel provider not found.");
                }
                else if (dto.ProviderType == ProviderType.TourGuide)
                {
                    if (!await _context.TourGuides.AnyAsync(t => t.Id == fine.ProviderId))
                        throw new ArgumentException("Tour guide not found.");
                }
                else if (dto.ProviderType == ProviderType.Airline)
                {
                    if (!await _context.Airlines.AnyAsync(a => a.Id == fine.ProviderId))
                        throw new ArgumentException("Airline provider not found.");
                }

                // Check duplicate
                var existing = await _context.ProviderFines.FirstOrDefaultAsync(f => 
                    f.Status == ProviderFineStatus.Active && 
                    f.SourceType == ProviderFineSourceType.Others &&
                    f.ProviderId == fine.ProviderId &&
                    f.Amount == fine.Amount &&
                    f.Reason == fine.Reason);
                
                if (existing != null)
                    throw new InvalidOperationException("An identical active fine already exists for this provider.");
            }
            else
            {
                throw new ArgumentException("Unsupported source type.");
            }

            _context.ProviderFines.Add(fine);
            await _context.SaveChangesAsync();

            return await GetFineDetailsAsync(fine.Id);
        }

        public async Task<ProviderFineDetailsDto> UpdateFineAsync(long id, UpdateProviderFineDto dto, long adminUserId)
        {
            var fine = await _context.ProviderFines.FindAsync(id);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status != ProviderFineStatus.Active)
                throw new InvalidOperationException("Only active fines can be edited.");

            if (dto.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Reason is required.");

            fine.Amount = dto.Amount;
            fine.Reason = dto.Reason;
            fine.AdminNotes = dto.AdminNotes;

            await _context.SaveChangesAsync();

            return await GetFineDetailsAsync(fine.Id);
        }

        public async Task CancelFineAsync(long id, CancelProviderFineDto dto, long adminUserId)
        {
            var fine = await _context.ProviderFines.FindAsync(id);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status != ProviderFineStatus.Active)
                throw new InvalidOperationException("Only active fines can be cancelled.");

            if (string.IsNullOrWhiteSpace(dto.CancellationReason))
                throw new ArgumentException("Cancellation reason is required.");

            fine.Status = ProviderFineStatus.Cancelled;
            fine.CancelledByAdminUserId = adminUserId;
            fine.CancelledAt = DateTime.UtcNow;
            fine.CancellationReason = dto.CancellationReason;

            await _context.SaveChangesAsync();
        }

        public async Task<List<EligibleFineComplaintDto>> GetEligibleComplaintsAsync(ProviderType type, string? search)
        {
            ComplaintType cType = type switch
            {
                ProviderType.Hotel => ComplaintType.Hotel,
                ProviderType.TourGuide => ComplaintType.Tour,
                ProviderType.Airline => ComplaintType.Airline,
                _ => throw new ArgumentException("Invalid provider type")
            };

            var activeFines = await _context.ProviderFines
                .Where(f => f.SourceType == ProviderFineSourceType.Complaint && f.Status == ProviderFineStatus.Active)
                .Select(f => f.ComplaintId)
                .ToListAsync();

            var result = new List<EligibleFineComplaintDto>();
            var term = search?.ToLower() ?? "";

            if (type == ProviderType.Airline)
            {
                // To bypass "Data is Null" EF exception on AirlineBooking / Flight, we project to anonymous type
                // with explicit nullable casts for any potentially problematic columns.
                var query = _context.Complaints
                    .Where(c => c.ComplaintType == ComplaintType.Airline)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    query = query.Where(c =>
                        c.Id.ToString() == term ||
                        (c.AirlineBookingId != null && c.AirlineBookingId.ToString() == term) ||
                        c.Subject.ToLower().Contains(term) ||
                        (c.User != null && c.User.UserName != null && c.User.UserName.ToLower().Contains(term)) ||
                        (c.User != null && c.User.Email != null && c.User.Email.ToLower().Contains(term)));
                }

                var data = await query.Select(c => new {
                    c.Id,
                    c.ComplaintType,
                    c.Subject,
                    Status = (string?)c.Status.ToString(),
                    c.CreatedAt,
                    UserName = c.User != null ? (string?)c.User.UserName : null,
                    UserEmail = c.User != null ? (string?)c.User.Email : null,
                    AirlineBookingId = c.AirlineBookingId,
                    FlightId = c.AirlineBooking != null ? (long?)c.AirlineBooking.FlightId : null,
                    AirlineId = c.AirlineBooking != null && c.AirlineBooking.Flight != null ? (long?)c.AirlineBooking.Flight.AirlineId : null,
                    FlightNumber = c.AirlineBooking != null && c.AirlineBooking.Flight != null ? (string?)c.AirlineBooking.Flight.FlightNumber : null,
                    AirlineName = c.AirlineBooking != null && c.AirlineBooking.Flight != null && c.AirlineBooking.Flight.Airline != null ? (string?)c.AirlineBooking.Flight.Airline.Name : null,
                    BookingStatus = c.AirlineBooking != null ? (string?)c.AirlineBooking.Status : null,
                    PaymentStatus = c.AirlineBooking != null ? (string?)c.AirlineBooking.PaymentStatus : null,
                    TotalPrice = c.AirlineBooking != null ? (decimal?)c.AirlineBooking.TotalPrice : null
                }).Take(50).ToListAsync();

                foreach (var d in data)
                {
                    result.Add(new EligibleFineComplaintDto
                    {
                        ComplaintId = d.Id,
                        ComplaintType = d.ComplaintType.ToString(),
                        Subject = d.Subject,
                        ComplaintStatus = d.Status ?? "Unknown",
                        UserName = d.UserName,
                        UserEmail = d.UserEmail,
                        ProviderId = d.AirlineId ?? 0,
                        ProviderName = !string.IsNullOrWhiteSpace(d.AirlineName) ? d.AirlineName : (d.AirlineId.HasValue ? $"Airline #{d.AirlineId}" : "Unknown Airline"),
                        CreatedAt = d.CreatedAt,
                        HasActiveFine = activeFines.Contains(d.Id),
                        RelatedBookingId = d.AirlineBookingId,
                        BookingTitle = !string.IsNullOrWhiteSpace(d.FlightNumber) ? d.FlightNumber : "Flight Booking",
                        BookingStatus = d.BookingStatus ?? "Unknown",
                        PaymentStatus = d.PaymentStatus ?? "Unknown",
                        TotalPrice = d.TotalPrice ?? 0,
                        TotalPaid = d.PaymentStatus == "Paid" ? (d.TotalPrice ?? 0) : 0,
                        Currency = "USD"
                    });
                }
            }
            else
            {
                var complaintsQuery = _context.Complaints
                    .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                    .Include(c => c.TourBooking).ThenInclude(tb => tb.Tour)
                    .Include(c => c.User)
                    .Where(c => c.ComplaintType == cType)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    complaintsQuery = complaintsQuery.Where(c =>
                        c.Id.ToString() == term ||
                        (c.BookingId != null && c.BookingId.ToString() == term) ||
                        (c.TourBookingId != null && c.TourBookingId.ToString() == term) ||
                        c.Subject.ToLower().Contains(term) ||
                        (c.User != null && c.User.UserName != null && c.User.UserName.ToLower().Contains(term)) ||
                        (c.User != null && c.User.Email != null && c.User.Email.ToLower().Contains(term)));
                }

                var complaints = await complaintsQuery.Take(50).ToListAsync();

                foreach (var c in complaints)
                {
                    long? providerId = null;
                    string providerName = "";

                    if (type == ProviderType.Hotel)
                    {
                        providerId = c.HotelId ?? c.Booking?.HotelId;
                        if (providerId.HasValue)
                        {
                            var hotel = await _context.Hotels.FindAsync(providerId.Value);
                            providerName = hotel?.HotelName ?? "Unknown Hotel";
                        }
                    }
                    else if (type == ProviderType.TourGuide)
                    {
                        providerId = c.TourBooking?.TourGuideId;
                        if (providerId.HasValue)
                        {
                            var tg = await _context.TourGuides.FindAsync(providerId.Value);
                            providerName = tg?.Name ?? "Unknown Tour Guide";
                        }
                    }

                    if (providerId.HasValue)
                    {
                        var dto = new EligibleFineComplaintDto
                        {
                            ComplaintId = c.Id,
                            ComplaintType = c.ComplaintType.ToString(),
                            Subject = c.Subject,
                            ComplaintStatus = c.Status.ToString(),
                            UserName = c.User?.UserName,
                            UserEmail = c.User?.Email,
                            ProviderId = providerId.Value,
                            ProviderName = !string.IsNullOrWhiteSpace(providerName) ? providerName : $"Provider #{providerId.Value}",
                            CreatedAt = c.CreatedAt,
                            HasActiveFine = activeFines.Contains(c.Id)
                        };

                        if (type == ProviderType.Hotel && c.Booking != null)
                        {
                            dto.RelatedBookingId = c.Booking.Id;
                            dto.BookingTitle = c.Booking.Hotel?.HotelName ?? "Hotel Booking";
                            dto.BookingStatus = c.Booking.Status.ToString();
                            dto.PaymentStatus = c.Booking.PaymentStatus.ToString();
                            dto.TotalPrice = c.Booking.TotalPrice;
                            dto.TotalPaid = c.Booking.PaymentStatus == TravAi.Models.Enums.PaymentStatus.Paid ? c.Booking.TotalPrice : 0;
                            dto.Currency = "USD";
                        }
                        else if (type == ProviderType.TourGuide && c.TourBooking != null)
                        {
                            dto.RelatedBookingId = c.TourBooking.Id;
                            dto.BookingTitle = c.TourBooking.Tour?.TourTitle ?? "Tour Booking";
                            dto.BookingStatus = c.TourBooking.Status.ToString();
                            dto.PaymentStatus = c.TourBooking.PaymentStatus.ToString();
                            dto.TotalPrice = c.TourBooking.TotalPrice;
                            dto.TotalPaid = c.TourBooking.PaymentStatus == TravAi.TourGuide.Models.Enums.PaymentStatus.Completed ? c.TourBooking.TotalPrice : 0;
                            dto.Currency = c.TourBooking.Currency ?? "USD";
                        }

                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        public async Task<List<EligibleTourCancellationDto>> GetTourGuideCancelledBookingsAsync(string? search)
        {
            var query = _context.TourBookings
                .Include(b => b.Tour)
                .Include(b => b.TourGuide)
                .Include(b => b.User)
                .Where(b => b.Status == TravAi.TourGuide.Models.Enums.BookingStatus.Cancelled &&
                            b.CancelledByRole == "TourGuide" &&
                            b.CancellationReviewStatus == "Rejected")
                .OrderByDescending(b => b.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(b => 
                    b.Id.ToString() == term ||
                    b.TourGuideId.ToString() == term ||
                    (b.Tour != null && b.Tour.TourTitle != null && b.Tour.TourTitle.ToLower().Contains(term)) ||
                    (b.TourGuide != null && b.TourGuide.Name != null && b.TourGuide.Name.ToLower().Contains(term)) ||
                    (b.User != null && b.User.UserName != null && b.User.UserName.ToLower().Contains(term)) ||
                    (b.User != null && b.User.Email != null && b.User.Email.ToLower().Contains(term)) ||
                    (b.CancellationReason != null && b.CancellationReason.ToLower().Contains(term)) ||
                    (b.CancellationReviewNotes != null && b.CancellationReviewNotes.ToLower().Contains(term))
                );
            }

            var bookings = await query.Take(50).ToListAsync();

            var activeFines = await _context.ProviderFines
                .Where(f => f.SourceType == ProviderFineSourceType.TourGuideCancellation && f.Status == ProviderFineStatus.Active)
                .Select(f => f.TourBookingId)
                .ToListAsync();

            var result = bookings.Select(b => new EligibleTourCancellationDto
            {
                TourBookingId = b.Id,
                TourId = b.TourId,
                TourName = b.Tour?.TourTitle ?? "Unknown Tour",
                TourGuideId = b.TourGuideId,
                TourGuideName = b.TourGuide?.Name ?? "Unknown Tour Guide",
                BookingDate = b.CreatedAt,
                TourDate = b.TourDate,
                ParticipantsCount = b.ParticipantsCount,
                Status = b.Status.ToString(),
                CancellationReason = b.CancellationReason,
                CancellationReviewStatus = b.CancellationReviewStatus,
                TotalPrice = b.TotalPrice,
                TicketPricePerParticipant = b.ParticipantsCount > 0 ? b.TotalPrice / b.ParticipantsCount : null,
                TotalPaid = b.PaymentStatus == TravAi.TourGuide.Models.Enums.PaymentStatus.Completed ? b.TotalPrice : 0,
                Currency = b.Currency ?? "USD",
                HasActiveFine = activeFines.Contains(b.Id)
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                result = result.Where(b => 
                    b.TourBookingId.ToString() == term ||
                    b.TourGuideId.ToString() == term ||
                    b.TourName.ToLower().Contains(term) ||
                    b.TourGuideName.ToLower().Contains(term)
                ).ToList();
            }

            return result;
        }

        public async Task<List<ProviderLookupDto>> GetProvidersLookupAsync(ProviderType type, string? search)
        {
            var result = new List<ProviderLookupDto>();
            var term = search?.ToLower() ?? "";

            if (type == ProviderType.Hotel)
            {
                var query = _context.Hotels.Include(h => h.User).AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    query = query.Where(h => h.Id.ToString() == term || h.HotelName.ToLower().Contains(term) || (h.User != null && h.User.Email.ToLower().Contains(term)));
                }
                var hotels = await query.Take(50).ToListAsync();
                result = hotels.Select(h => new ProviderLookupDto
                {
                    ProviderId = h.Id,
                    ProviderName = h.HotelName ?? $"Hotel #{h.Id}",
                    ProviderType = type,
                    ExtraInfo = h.User?.Email
                }).ToList();
            }
            else if (type == ProviderType.TourGuide)
            {
                var query = _context.TourGuides.Include(tg => tg.User).AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    query = query.Where(t => t.Id.ToString() == term || t.Name.ToLower().Contains(term) || (t.User != null && t.User.Email.ToLower().Contains(term)));
                }
                var guides = await query.Take(50).ToListAsync();
                result = guides.Select(g => new ProviderLookupDto
                {
                    ProviderId = g.Id,
                    ProviderName = g.Name ?? $"Tour Guide #{g.Id}",
                    ProviderType = type,
                    ExtraInfo = g.User?.Email
                }).ToList();
            }
            else if (type == ProviderType.Airline)
            {
                var query = _context.Airlines.AsQueryable();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    query = query.Where(a => a.Id.ToString() == term || 
                        (a.Name != null && a.Name.ToLower().Contains(term)) || 
                        (a.LicenseNumber != null && a.LicenseNumber.ToLower().Contains(term)));
                }
                var airlines = await query.Select(a => new {
                    a.Id,
                    Name = (string?)a.Name,
                    LicenseNumber = (string?)a.LicenseNumber
                }).Take(50).ToListAsync();

                result = airlines.Select(a => new ProviderLookupDto
                {
                    ProviderId = a.Id,
                    ProviderName = !string.IsNullOrWhiteSpace(a.Name) ? a.Name : $"Airline #{a.Id}",
                    ProviderType = type,
                    ExtraInfo = a.LicenseNumber
                }).ToList();
            }

            return result;
        }
        public async Task<List<ProviderFineListItemDto>> GetFinesByComplaintIdAsync(long complaintId)
        {
            var filter = new ProviderFineFilterDto { ComplaintId = complaintId };
            return await GetFinesAsync(filter);
        }

        public async Task<EligibleTourCancellationDto?> GetTourCancellationDetailsAsync(long tourBookingId)
        {
            var b = await _context.TourBookings
                .Include(tb => tb.Tour)
                .Include(tb => tb.TourGuide)
                .Include(tb => tb.User)
                .FirstOrDefaultAsync(tb => tb.Id == tourBookingId);

            if (b == null) return null;

            var activeFines = await _context.ProviderFines
                .Where(f => f.SourceType == ProviderFineSourceType.TourGuideCancellation && f.Status == ProviderFineStatus.Active && f.TourBookingId == tourBookingId)
                .Select(f => f.TourBookingId)
                .ToListAsync();

            return new EligibleTourCancellationDto
            {
                TourBookingId = b.Id,
                TourId = b.TourId,
                TourName = b.Tour?.TourTitle ?? "Unknown Tour",
                TourGuideId = b.TourGuideId,
                TourGuideName = b.TourGuide?.Name ?? "Unknown Tour Guide",
                BookingDate = b.CreatedAt,
                TourDate = b.TourDate,
                ParticipantsCount = b.ParticipantsCount,
                Status = b.Status.ToString(),
                CancellationReason = b.CancellationReason,
                CancellationReviewStatus = b.CancellationReviewStatus,
                TotalPrice = b.TotalPrice,
                TicketPricePerParticipant = b.ParticipantsCount > 0 ? b.TotalPrice / b.ParticipantsCount : null,
                TotalPaid = b.PaymentStatus == TravAi.TourGuide.Models.Enums.PaymentStatus.Completed ? b.TotalPrice : 0,
                Currency = b.Currency ?? "USD",
                HasActiveFine = activeFines.Contains(b.Id)
            };
        }
    }
}
