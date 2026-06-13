using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using TravAi.DTOs.Hotel;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.Models.Hotels;
using TravAi.Models.Hotels.Bookings;
using TravAi.Services.FileStorage;
using TravAi.DTOs.AdminManagement;

namespace TravAi.Services.HotelService
{
    public class HotelService : IHotelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly TravAi.Services.Common.IWalletService _walletService;
        private readonly TravAi.Options.StripeOptions _stripeOptions;

        public HotelService(ApplicationDbContext context, IFileService fileService, TravAi.Services.Common.IWalletService walletService, Microsoft.Extensions.Options.IOptions<TravAi.Options.StripeOptions> stripeOptions)
        {
            _context = context;
            _fileService = fileService;
            _walletService = walletService;
            _stripeOptions = stripeOptions.Value;
        }

        // --- Active Hotel Operations ---

        public async Task<HotelDetailsDto> GetMyHotelProfileAsync(long userId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel not found for user.");
            return await GetHotelDetailsAsync(hotel.Id);
        }

        public async Task<bool> UpdateHotelAsync(long userId, long hotelId, UpdateHotelRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId && h.UserId == userId);
            if (hotel == null) return false;

            hotel.HotelName = request.HotelName;
            hotel.Description = request.Description;
            hotel.CityArea = request.CityArea;
            hotel.Governorate = request.Governorate;
            hotel.Country = request.Country ?? hotel.Country;
            hotel.StarRating = request.StarRating;
            hotel.PropertyType = request.PropertyType ?? hotel.PropertyType;
            hotel.AccommodationType = request.AccommodationType ?? hotel.AccommodationType;
            hotel.AddressDetails = request.AddressDetails ?? hotel.AddressDetails;
            hotel.AddressProofUrl = request.AddressProofUrl ?? hotel.AddressProofUrl;
            
            // Derive TypeNorm
            hotel.TypeNorm = GenerateTypeNorm(hotel.PropertyType, hotel.AccommodationType);

            hotel.UpdatedAt = DateTime.UtcNow;
            ValidateHotelCore(hotel);

            if (request.Contacts != null)
                await UpsertHotelContactsAsync(hotel.Id, request.Contacts);

            if (request.Documents != null)
                await UpsertHotelDocumentsAsync(hotel.Id, request.Documents);

            if (request.DynamicFields != null)
                await UpsertHotelFieldValuesAsync(hotel.Id, request.DynamicFields);

            if (request.Policy != null)
                await UpsertHotelPolicyAsync(hotel.Id, request.Policy);

            if (request.AmenityIds != null)
                await ReplaceHotelAmenitiesAsync(hotel.Id, request.AmenityIds);

            _context.Hotels.Update(hotel);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Application Logic ---
        public async Task<bool> ApplyAsHotelAsync(long userId, HotelApplicationRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel != null && hotel.Verified) throw new Exception("User already has a verified hotel.");

            // Handle Address Proof File
            var addressProofUrl = request.AddressProofUrl;
            if (request.AddressProofFile != null)
            {
                var saved = await _fileService.SaveHotelDocumentAsync(request.AddressProofFile);
                if (!string.IsNullOrEmpty(saved)) addressProofUrl = saved;
            }

            if (hotel == null)
            {
                hotel = new Hotel
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Verified = false
                };
                await _context.Hotels.AddAsync(hotel);
            }

            // Map data
            hotel.HotelName = request.HotelName;
            hotel.Description = request.Description;
            hotel.PropertyType = request.PropertyType;
            hotel.AccommodationType = request.AccommodationType;
            hotel.StarRating = request.StarRating;
            hotel.Country = request.Country;
            hotel.Governorate = request.Governorate;
            hotel.CityArea = request.CityArea;
            hotel.AddressDetails = request.AddressDetails;
            if (!string.IsNullOrEmpty(addressProofUrl)) hotel.AddressProofUrl = addressProofUrl;
            hotel.UpdatedAt = DateTime.UtcNow;
            hotel.TypeNorm = GenerateTypeNorm(request.PropertyType, request.AccommodationType);

            await _context.SaveChangesAsync();

            // Clear existing data for a clean update (Optional, but simple for initial rooms/images/etc in a draft flow)
            _context.HotelRooms.RemoveRange(_context.HotelRooms.Where(r => r.HotelId == hotel.Id));
            _context.HotelImages.RemoveRange(_context.HotelImages.Where(i => i.HotelId == hotel.Id));
            _context.HotelDocuments.RemoveRange(_context.HotelDocuments.Where(d => d.HotelId == hotel.Id));
            await _context.SaveChangesAsync();

            // Initial Rooms
            if (request.InitialRooms != null && request.InitialRooms.Any())
            {
                foreach (var rReq in request.InitialRooms)
                {
                    var room = new HotelRoom
                    {
                        HotelId = hotel.Id,
                        Name = rReq.Name,
                        Occupancy = rReq.Occupancy ?? 2,
                        BedType = rReq.BedType,
                        ROPrice = rReq.ROPrice,
                        BBPrice = rReq.BBPrice,
                        HBPrice = rReq.HBPrice,
                        FBPrice = rReq.FBPrice,
                        AIPrice = rReq.AIPrice,
                        Quantity = rReq.Quantity.GetValueOrDefault(1),
                        State = RoomState.Active
                    };
                    await _context.HotelRooms.AddAsync(room);
                }
                await _context.SaveChangesAsync();
                await RecalculateHotelPriceAsync(hotel.Id);
            }

            // Amenities
            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                await ReplaceHotelAmenitiesAsync(hotel.Id, request.AmenityIds);
            }

            // New Sections
            if (request.Contacts != null && request.Contacts.Any())
                await UpsertHotelContactsAsync(hotel.Id, request.Contacts);
            
            // Documents
            if (request.Documents != null && request.Documents.Any())
            {
                foreach (var doc in request.Documents)
                {
                    if (doc.File != null)
                    {
                        var saved = await _fileService.SaveHotelDocumentAsync(doc.File);
                        if (!string.IsNullOrEmpty(saved)) doc.FileUrl = saved;
                    }
                }
                await UpsertHotelDocumentsAsync(hotel.Id, request.Documents);
            }
            
            // Always call validation even if list is empty to ensure required fields are checked
            await UpsertHotelFieldValuesAsync(hotel.Id, request.DynamicFields ?? new List<HotelFieldValueInputDto>());
            
            if (request.Policy != null)
                await UpsertHotelPolicyAsync(hotel.Id, request.Policy);
            
            // Images
            if (request.Images != null && request.Images.Any())
            {
                foreach (var img in request.Images)
                {
                    var finalUrl = img.ImageUrl;
                    if (img.ImageFile != null)
                    {
                        var saved = await _fileService.SaveHotelImageAsync(img.ImageFile);
                        if (!string.IsNullOrEmpty(saved)) finalUrl = saved;
                    }

                    if (string.IsNullOrEmpty(finalUrl)) continue;

                    _context.HotelImages.Add(new HotelImage
                    {
                        HotelId = hotel.Id,
                        ImageUrl = finalUrl,
                        Caption = img.Caption,
                        IsPrimary = img.IsPrimary,
                        SortOrder = img.SortOrder
                    });
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<HotelDetailsDto> GetMyApplicationStatusAsync(long userId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return null;
            return await GetHotelDetailsAsync(hotel.Id);
        }

        public async Task<bool> DeleteMyApplicationAsync(long userId)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .Include(h => h.Images)
                .Include(h => h.Documents)
                .Include(h => h.Contacts)
                .Include(h => h.FieldValues)
                .Include(h => h.Policy).ThenInclude(p => p.CancellationRules)
                .Include(h => h.HotelAmenities)
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Verified == false);

            if (hotel == null) return false;

            // Remove everything
            if (hotel.Policy != null)
                _context.HotelCancellationRules.RemoveRange(hotel.Policy.CancellationRules);
            
            if (hotel.Policy != null)
                _context.HotelPolicies.Remove(hotel.Policy);

            _context.HotelRooms.RemoveRange(hotel.Rooms);
            _context.HotelImages.RemoveRange(hotel.Images);
            _context.HotelDocuments.RemoveRange(hotel.Documents);
            _context.HotelContacts.RemoveRange(hotel.Contacts);
            _context.HotelFieldValues.RemoveRange(hotel.FieldValues);
            _context.HotelAmenities.RemoveRange(hotel.HotelAmenities);

            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Hotel Rooms CRUD ---
        public async Task<RoomDto> CreateRoomAsync(long userId, CreateRoomRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId && h.Verified);
            if (hotel == null) throw new Exception("You must have a verified hotel account to create rooms.");

            var room = new HotelRoom
            {
                HotelId = hotel.Id,
                Name = request.Name,
                Occupancy = request.Occupancy,
                BedType = request.BedType,
                ROPrice = request.ROPrice,
                BBPrice = request.BBPrice,
                HBPrice = request.HBPrice,
                FBPrice = request.FBPrice,
                AIPrice = request.AIPrice,
                Quantity = request.Quantity.GetValueOrDefault(1),
                State = RoomState.Active
            };

            await _context.HotelRooms.AddAsync(room);
            await _context.SaveChangesAsync();

            // Recalculate hotel price
            await RecalculateHotelPriceAsync(hotel.Id);

            return MapRoomToDto(room, room.Quantity);
        }

        public async Task<List<RoomDto>> GetRoomsByHotelAsync(long hotelId)
        {
            return await _context.HotelRooms
                .Where(r => r.HotelId == hotelId)
                .Select(r => MapRoomToDto(r, r.Availability))
                .ToListAsync();
        }

        public async Task<RoomDto> UpdateRoomAsync(long userId, long roomId, UpdateRoomRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel account not found.");

            var room = await _context.HotelRooms.FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotel.Id);
            if (room == null) throw new Exception("Room not found or you don't have permission.");

            room.Name = request.Name ?? room.Name;
            room.Occupancy = request.Occupancy ?? room.Occupancy;
            room.BedType = request.BedType ?? room.BedType;
            room.ROPrice = request.ROPrice ?? room.ROPrice;
            room.BBPrice = request.BBPrice ?? room.BBPrice;
            room.HBPrice = request.HBPrice ?? room.HBPrice;
            room.FBPrice = request.FBPrice ?? room.FBPrice;
            room.AIPrice = request.AIPrice ?? room.AIPrice;
            room.Quantity = request.Quantity ?? room.Quantity;

            _context.HotelRooms.Update(room);
            await _context.SaveChangesAsync();

            // Recalculate hotel price
            await RecalculateHotelPriceAsync(hotel.Id);

            return MapRoomToDto(room, room.Quantity);
        }

        public async Task<bool> DeleteRoomAsync(long userId, long roomId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return false;

            var room = await _context.HotelRooms.FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotel.Id);
            if (room == null) return false;

            _context.HotelRooms.Remove(room);
            await _context.SaveChangesAsync();

            // Recalculate hotel price
            await RecalculateHotelPriceAsync(hotel.Id);

            return true;
        }

        // --- Amenities ---
        public async Task<HotelAmenitiesDto> GetAmenitiesAsync(long hotelId)
        {
            var names = await _context.HotelAmenities
                .Include(x => x.Amenity)
                .Where(x => x.HotelId == hotelId)
                .Select(x => x.Amenity.Name)
                .ToListAsync();

            return ToAmenityDto(names);
        }

        public async Task<bool> UpdateAmenitiesAsync(long userId, long hotelId, UpdateAmenitiesRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId && h.UserId == userId);
            if (hotel == null) return false;
            var keys = new List<string>();
            if (request.FreeWifi) keys.Add("FreeWifi");
            if (request.SwimmingPool) keys.Add("SwimmingPool");
            if (request.Parking) keys.Add("Parking");
            if (request.AirConditioning) keys.Add("AirConditioning");
            if (request.Breakfast) keys.Add("Breakfast");
            if (request.Gym) keys.Add("Gym");
            if (request.Restaurant) keys.Add("Restaurant");
            if (request.Spa) keys.Add("Spa");
            if (request.RoomService) keys.Add("RoomService");

            var amenityIds = await _context.Amenities
                .Where(a => keys.Contains(a.Name))
                .Select(a => a.Id)
                .ToListAsync();

            await ReplaceHotelAmenitiesAsync(hotelId, amenityIds);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Public Search & Details ---
        public async Task<HotelSearchResponse> SearchHotelsAsync(HotelSearchRequest request)
        {
            var startDate = request.CheckIn ?? DateTime.Today;
            var endDate = request.CheckOut ?? startDate.AddDays(1);
            var totalPersons = request.Persons ?? 1;
            var reqRooms = request.Rooms ?? 1;

            var query = _context.Hotels
                .Include(h => h.Images)
                .Include(h => h.Rooms)
                .Where(h => h.Verified && h.Active && h.VerificationStatus == VerificationStatus.Verified);

            // 1. Destination Filter
            if (!string.IsNullOrEmpty(request.Destination))
            {
                query = query.Where(h =>
                    (h.Governorate != null && h.Governorate.Contains(request.Destination)) ||
                    (h.CityArea != null && h.CityArea.Contains(request.Destination)) ||
                    h.HotelName.Contains(request.Destination));
            }

            // 2. Official Star Rating Filter
            if (request.MinStarRating.HasValue)
                query = query.Where(h => h.StarRating >= request.MinStarRating.Value);

            // 2.1 User Review Rating Filter
            var reviewFilter = request.AvgRating ?? request.MinReviewRating;
            if (reviewFilter.HasValue && reviewFilter.Value > 0)
                query = query.Where(h => h.NumReviews > 0 && h.AvgReviewScore >= reviewFilter.Value);

            // 3. Price Filter using room prices
            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                query = query.Where(h => h.Rooms.Any(r =>
                    (!request.MinPrice.HasValue || r.ROPrice >= request.MinPrice) &&
                    (!request.MaxPrice.HasValue || r.ROPrice <= request.MaxPrice)));
            }

            // 4. Amenities Filter (AND logic via relation table)
            if (request.Amenities != null && request.Amenities.Any())
            {
                foreach(var amenityName in request.Amenities)
                {
                    query = query.Where(h => _context.HotelAmenities.Any(ha => ha.HotelId == h.Id && ha.Amenity.Name.ToLower() == amenityName.ToLower()));
                }
            }

            // 5. Bed Type Filter
            BedType? targetBedType = null;
            if (!string.IsNullOrEmpty(request.BedType) && Enum.TryParse<BedType>(request.BedType, out var bt))
            {
                targetBedType = bt;
                query = query.Where(h => h.Rooms.Any(r => r.BedType == targetBedType));
            }

            // 6. Availability & Capacity Filter (Persons & Rooms)
            
            // Filter by Total Available Units >= reqRooms
            query = query.Where(h => 
                h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Sum(r => r.Quantity) -
                _context.HotelBookingRooms.Count(br => 
                    br.Room.HotelId == h.Id && 
                    (targetBedType == null || br.Room.BedType == targetBedType) &&
                    br.Booking.Status != BookingStatus.Cancelled &&
                    br.Booking.Status != BookingStatus.Completed &&
                    br.Booking.CheckInDate < endDate &&
                    startDate < br.Booking.CheckOutDate) >= reqRooms);
            
            // Filter by Total Available Capacity >= totalPersons
            query = query.Where(h => 
                h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Sum(r => r.Quantity * (r.Occupancy ?? 0)) -
                _context.HotelBookingRooms.Where(br => 
                    br.Room.HotelId == h.Id && 
                    (targetBedType == null || br.Room.BedType == targetBedType) &&
                    br.Booking.Status != BookingStatus.Cancelled &&
                    br.Booking.Status != BookingStatus.Completed &&
                    br.Booking.CheckInDate < endDate &&
                    startDate < br.Booking.CheckOutDate)
                .Sum(br => br.Room.Occupancy ?? 0) >= totalPersons);

            // Hard Capacity Rule
            query = query.Where(h => h.Rooms
                .Where(r => (targetBedType == null || r.BedType == targetBedType) && (r.Quantity - _context.HotelBookingRooms.Count(br => br.RoomId == r.Id && br.Booking.Status != BookingStatus.Cancelled && br.Booking.Status != BookingStatus.Completed && br.Booking.CheckInDate < endDate && startDate < br.Booking.CheckOutDate)) > 0)
                .Max(r => (int?)r.Occupancy ?? 0) * reqRooms >= totalPersons);

            // Sorting
            query = (request.SortBy?.ToLower()) switch
            {
                "popular" => query.OrderByDescending(h => h.NumReviews),
                "recent" => query.OrderByDescending(h => h.CreatedAt),
                "price_low" => query.OrderBy(h => h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Min(r => r.ROPrice) ?? decimal.MaxValue),
                "price_high" => query.OrderByDescending(h => h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Min(r => r.ROPrice) ?? 0),
                "rating" => query.OrderByDescending(h => h.AvgReviewScore),
                _ => query.OrderByDescending(h => h.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var hotelList = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(h => h.Rooms)
                .Include(h => h.Images)
                .ToListAsync();

            var roomIds = hotelList.SelectMany(h => h.Rooms.Select(r => r.Id)).Distinct().ToList();
            var bookedCounts = new Dictionary<long, int>();
            if (roomIds.Count > 0)
            {
                var counts = await _context.HotelBookingRooms
                    .Where(br => br.RoomId != null && roomIds.Contains(br.RoomId.Value) &&
                                 br.Booking.Status != BookingStatus.Cancelled &&
                                 br.Booking.Status != BookingStatus.Completed &&
                                 br.Booking.CheckInDate < endDate &&
                                 startDate < br.Booking.CheckOutDate)
                    .GroupBy(br => br.RoomId!.Value)
                    .Select(g => new { RoomId = g.Key, Count = g.Count() })
                    .ToListAsync();
                foreach (var c in counts)
                    bookedCounts[c.RoomId] = c.Count;
            }

            var hotels = hotelList.Select(h =>
            {
                var availableRooms = h.Rooms
                    .Where(r => targetBedType == null || r.BedType == targetBedType)
                    .Sum(r =>
                    {
                        var booked = bookedCounts.GetValueOrDefault(r.Id, 0);
                        return Math.Max(0, r.Quantity - booked);
                    });
                
                var primaryImg = h.Images?.FirstOrDefault(i => i.IsPrimary) ?? h.Images?.FirstOrDefault();
                
                // Get amenity tags
                var tags = _context.HotelAmenities
                    .Include(x => x.Amenity)
                    .Where(x => x.HotelId == h.Id && x.Amenity.IsHighlighted)
                    .Select(x => x.Amenity.Name)
                    .Take(6)
                    .ToList();

                return new HotelCardDto
                {
                    Id = h.Id,
                    HotelName = h.HotelName,
                    Location = h.Governorate ?? h.CityArea ?? "",
                    StarRating = h.StarRating,
                    AvgRating = h.NumReviews > 0 ? h.AvgReviewScore : 0,
                    ReviewCount = h.NumReviews,
                    PricePerNight = h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Min(r => r.ROPrice),
                    PrimaryImageUrl = primaryImg?.ImageUrl,
                    TotalRooms = h.Rooms.Where(r => targetBedType == null || r.BedType == targetBedType).Sum(r => r.Quantity),
                    AvailableRooms = availableRooms,
                    AmenityTags = tags,
                    VerificationStatus = h.VerificationStatus.ToString()
                };
            }).ToList();

            return new HotelSearchResponse
            {
                Hotels = hotels,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public async Task<HotelDetailsDto> GetHotelDetailsAsync(long hotelId, DateTime? checkIn = null, DateTime? checkOut = null)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Images)
                .Include(h => h.Rooms)
                .Include(h => h.Contacts)
                .Include(h => h.Documents).ThenInclude(d => d.DocumentType)
                .Include(h => h.FieldValues).ThenInclude(v => v.FieldDefinition)
                .Include(h => h.Policy).ThenInclude(p => p.CancellationRules)
                .FirstOrDefaultAsync(h => h.Id == hotelId);

            if (hotel == null) throw new Exception("Hotel not found.");

            var startDate = checkIn ?? DateTime.Today;
            var endDate = checkOut ?? (checkIn?.AddDays(1) ?? DateTime.Today.AddDays(1));

            var totalCapacity = hotel.Rooms.Sum(r => r.Quantity);

            var bookedRooms = await _context.HotelBookingRooms
                .Where(br => br.Room != null && br.Room.HotelId == hotelId &&
                             br.Booking.Status != BookingStatus.Cancelled &&
                             br.Booking.Status != BookingStatus.Completed &&
                             br.Booking.CheckInDate < endDate &&
                             startDate < br.Booking.CheckOutDate)
                .GroupBy(br => br.RoomId)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoomId!.Value, x => x.Count);

            var availableRoomsTotal = 0;
            var roomDtos = new List<RoomDto>();

            foreach(var r in hotel.Rooms)
            {
                int booked = bookedRooms.ContainsKey(r.Id) ? bookedRooms[r.Id] : 0;
                int free = Math.Max(0, r.Quantity - booked);
                availableRoomsTotal += free;
                roomDtos.Add(MapRoomToDto(r, free));
            }

            var result = new HotelDetailsDto
            {
                Id = hotel.Id,
                HotelName = hotel.HotelName,
                Location = hotel.Governorate ?? hotel.CityArea ?? "",
                CityArea = hotel.CityArea,
                Governorate = hotel.Governorate,
                StarRating = hotel.StarRating,
                AvgRating = hotel.NumReviews > 0 ? hotel.AvgReviewScore : 0,
                ReviewCount = hotel.NumReviews,
                Description = hotel.Description,
                PriceUsd = hotel.PriceUsd,
                TypeNorm = hotel.TypeNorm,

                TotalRooms = totalCapacity,
                AvailableRooms = availableRoomsTotal,
                Images = hotel.Images.OrderBy(i => i.SortOrder).Select(i => new HotelImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    Caption = i.Caption,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                }).ToList(),
                Amenities = new HotelAmenitiesDto(),
                Rooms = roomDtos,
                Contacts = hotel.Contacts.Select(c => new HotelContactDto
                {
                    Id = c.Id,
                    ContactType = c.ContactType.ToString(),
                    ContactValue = c.ContactValue
                }).ToList(),
                Documents = hotel.Documents.Select(d => new HotelDocumentDto
                {
                    Id = d.Id,
                    DocumentTypeId = d.DocumentTypeId,
                    DocumentTypeName = d.DocumentType.Name,
                    DocumentTypeKeyName = d.DocumentType.KeyName,
                    IsRequired = d.DocumentType.IsRequired,
                    FileUrl = d.FileUrl,
                    Notes = d.Notes,
                    UploadedAt = d.UploadedAt
                }).ToList(),
                DynamicFields = hotel.FieldValues.Select(v => new HotelDynamicFieldValueDto
                {
                    Id = v.Id,
                    FieldDefinitionId = v.FieldDefinitionId,
                    KeyName = v.FieldDefinition.KeyName,
                    DisplayName = v.FieldDefinition.DisplayName,
                    FieldType = v.FieldDefinition.FieldType.ToString(),
                    IsRequired = v.FieldDefinition.IsRequired,
                    Value = v.Value
                }).ToList(),
                Policy = hotel.Policy == null ? null : new HotelPolicyDto
                {
                    Id = hotel.Policy.Id,
                    ServiceChargePct = hotel.Policy.ServiceChargePct,
                    IncludeServiceCharge = hotel.Policy.IncludeServiceCharge,
                    IncludeVat = hotel.Policy.IncludeVat,
                    IncludeCityTax = hotel.Policy.IncludeCityTax,
                    CancellationStrategy = hotel.Policy.CancellationStrategy.ToString(),
                    CancellationRules = hotel.Policy.CancellationRules.OrderBy(r => r.FromHoursBeforeCheckIn).Select(r => new HotelCancellationRuleDto
                    {
                        Id = r.Id,
                        FromHoursBeforeCheckIn = r.FromHoursBeforeCheckIn,
                        ToHoursBeforeCheckIn = r.ToHoursBeforeCheckIn,
                        PenaltyPct = r.PenaltyPct
                    }).ToList()
                },
                Verified = hotel.Verified,
                VerificationStatus = hotel.VerificationStatus.ToString(),
                RejectionReason = hotel.RejectionReason,
                UserId = hotel.UserId,
                OwnerName = hotel.User?.Name,
                OwnerEmail = hotel.User?.Email,
                CreatedAt = hotel.CreatedAt,
                PropertyType = hotel.PropertyType?.ToString(),
                AccommodationType = hotel.AccommodationType?.ToString(),
                AddressDetails = hotel.AddressDetails,
                Country = hotel.Country,
                AddressProofUrl = hotel.AddressProofUrl
            };
            var amenities = await _context.HotelAmenities
                .Include(ha => ha.Amenity)
                .Where(ha => ha.HotelId == hotelId)
                .Select(ha => ha.Amenity)
                .ToListAsync();

            result.AmenityNames = amenities.Select(a => a.Name).ToList();
            result.AmenityDetails = amenities.Select(a => new AmenityDto { 
                Id = a.Id, 
                Name = a.Name, 
                Category = a.Category, 
                IsHighlighted = a.IsHighlighted 
            }).ToList();
            result.Amenities = ToAmenityDto(result.AmenityNames);

            return result;
        }

        // --- Image Upload & Management ---
        public async Task<ImageUploadResponse> UploadImageAsync(long userId, UploadImageRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId && h.Id == request.HotelId);
            if (hotel == null) throw new Exception("Hotel not found or permission denied.");

            if (request.Image == null || request.Image.Length == 0) throw new Exception("No image file provided.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) throw new Exception("Invalid file type.");
            if (request.Image.Length > 5 * 1024 * 1024) throw new Exception("File size must be < 5MB.");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "hotels");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            if (request.IsPrimary)
            {
                var existingPrimary = await _context.HotelImages.Where(i => i.HotelId == request.HotelId && i.IsPrimary).ToListAsync();
                foreach (var img in existingPrimary) img.IsPrimary = false;
                _context.HotelImages.UpdateRange(existingPrimary);
            }

            var image = new HotelImage
            {
                HotelId = request.HotelId,
                ImageUrl = $"/uploads/hotels/{fileName}",
                Caption = request.Caption,
                IsPrimary = request.IsPrimary,
                SortOrder = request.SortOrder
            };

            await _context.HotelImages.AddAsync(image);
            await _context.SaveChangesAsync();

            return new ImageUploadResponse
            {
                Id = image.Id,
                HotelId = image.HotelId,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder,
                FileSizeBytes = request.Image.Length,
                UploadedAt = DateTime.UtcNow
            };
        }

        public async Task<ImageUploadResponse> UpdateImageAsync(long userId, long imageId, UpdateImageRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) throw new Exception("Hotel account not found.");

            var image = await _context.HotelImages.FirstOrDefaultAsync(i => i.Id == imageId && i.HotelId == hotel.Id);
            if (image == null) throw new Exception("Image not found.");

            if (request.Caption != null) image.Caption = request.Caption;
            if (request.SortOrder.HasValue) image.SortOrder = request.SortOrder.Value;

            if (request.IsPrimary.HasValue && request.IsPrimary.Value)
            {
                var existingPrimary = await _context.HotelImages.Where(i => i.HotelId == hotel.Id && i.IsPrimary && i.Id != imageId).ToListAsync();
                foreach (var img in existingPrimary) img.IsPrimary = false;
                _context.HotelImages.UpdateRange(existingPrimary);
                image.IsPrimary = true;
            }

            _context.HotelImages.Update(image);
            await _context.SaveChangesAsync();

            return new ImageUploadResponse
            {
                Id = image.Id,
                HotelId = image.HotelId,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder,
                UploadedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> DeleteImageAsync(long userId, long imageId)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hotel == null) return false;

            var image = await _context.HotelImages.FirstOrDefaultAsync(i => i.Id == imageId && i.HotelId == hotel.Id);
            if (image == null) return false;

            _context.HotelImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ImageUploadResponse>> GetHotelImagesAsync(long hotelId)
        {
            return await _context.HotelImages
                .Where(i => i.HotelId == hotelId)
                .OrderBy(i => i.SortOrder)
                .Select(i => new ImageUploadResponse
                {
                    Id = i.Id,
                    HotelId = i.HotelId,
                    ImageUrl = i.ImageUrl,
                    Caption = i.Caption,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder,
                    UploadedAt = DateTime.UtcNow
                })
                .ToListAsync();
        }

        // --- Reviews ---
        public async Task<HotelReviewDto> AddReviewAsync(long userId, CreateReviewRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == request.HotelId);
            if (hotel == null) throw new Exception("Hotel not found.");
            if (hotel.UserId == userId) throw new Exception("You cannot review your own hotel.");

            bool alreadyReviewed = await _context.HotelReviews.AnyAsync(r => r.UserId == userId && r.HotelId == request.HotelId);
            if (alreadyReviewed) throw new Exception("Only one review is allowed per user.");

            var review = new HotelReview
            {
                HotelId = request.HotelId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _context.HotelReviews.AddAsync(review);
            await _context.SaveChangesAsync();
            await RecalculateHotelStatsAsync(request.HotelId);

            var user = await _context.Users.FindAsync(userId);
            return new HotelReviewDto
            {
                Id = review.Id,
                UserId = userId,
                UserName = user?.UserName ?? "Anonymous",
                UserProfilePicture = user?.ProfilePic,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                TimeAgo = "Just now"
            };
        }

        public async Task<HotelReviewDto> UpdateReviewAsync(long userId, long reviewId, UpdateReviewRequest request)
        {
            var review = await _context.HotelReviews.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null) throw new Exception("Review not found.");
            if (review.UserId != userId) throw new Exception("Not authorized to edit this review.");

            review.Rating = request.Rating;
            review.Comment = request.Comment;

            _context.HotelReviews.Update(review);
            await _context.SaveChangesAsync();
            await RecalculateHotelStatsAsync(review.HotelId);

            return new HotelReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.UserName ?? "Anonymous",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                TimeAgo = "Updated"
            };
        }

        public async Task<bool> DeleteReviewAsync(long userId, long reviewId)
        {
            var review = await _context.HotelReviews.FindAsync(reviewId);
            if (review == null || review.UserId != userId) return false;

            var hotelId = review.HotelId;
            _context.HotelReviews.Remove(review);
            await _context.SaveChangesAsync();
            await RecalculateHotelStatsAsync(hotelId);

            return true;
        }

        private async Task RecalculateHotelStatsAsync(long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return;

            var stats = await _context.HotelReviews
                .Where(r => r.HotelId == hotelId)
                .GroupBy(r => r.HotelId)
                .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();

            if (stats != null)
            {
                hotel.AvgReviewScore = (decimal)stats.Avg;
                hotel.NumReviews = stats.Count;
            }
            else
            {
                hotel.AvgReviewScore = 0;
                hotel.NumReviews = 0;
            }

            hotel.UpdatedAt = DateTime.UtcNow;
            _context.Hotels.Update(hotel);
            await _context.SaveChangesAsync();
        }

        public async Task<HotelReviewsResponse> GetHotelReviewsAsync(long hotelId, int page, int pageSize, bool isRandom = false)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) throw new Exception("Hotel not found.");

            var query = _context.HotelReviews.Include(r => r.User).Where(r => r.HotelId == hotelId);
            if (isRandom) query = query.OrderBy(r => Guid.NewGuid());
            else query = query.OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(r => new HotelReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User.UserName,
                UserProfilePicture = r.User.ProfilePic,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                TimeAgo = r.CreatedAt.ToString("MMM dd, yyyy")
            }).ToListAsync();

            return new HotelReviewsResponse { Reviews = reviews, AvgRating = hotel.AvgReviewScore, TotalCount = totalCount };
        }

        // --- Public Room Search ---
        public async Task<RoomSearchResponse> SearchRoomsAsync(RoomSearchRequest request)
        {
            var query = _context.HotelRooms.Include(r => r.Hotel).AsQueryable();

            if (request.HotelId.HasValue) query = query.Where(r => r.HotelId == request.HotelId.Value);
            if (request.MinPrice.HasValue) query = query.Where(r => r.ROPrice >= request.MinPrice.Value);
            if (request.MaxPrice.HasValue) query = query.Where(r => r.ROPrice <= request.MaxPrice.Value);
            if (request.MinOccupancy.HasValue) query = query.Where(r => r.Occupancy >= request.MinOccupancy.Value);
            
            if (!string.IsNullOrEmpty(request.BedType) && Enum.TryParse<BedType>(request.BedType, out var bt)) 
                query = query.Where(r => r.BedType == bt);

            if (request.OnlyAvailable == true && (!request.CheckInDate.HasValue || !request.CheckOutDate.HasValue))
                query = query.Where(r => r.State == RoomState.Active);

            if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
            {
                query = query.Where(r => 
                    r.Quantity > _context.HotelBookingRooms
                        .Count(br => br.RoomId == r.Id && 
                                     br.Booking.Status != BookingStatus.Cancelled &&
                                     br.Booking.Status != BookingStatus.Completed &&
                                     br.Booking.CheckInDate < request.CheckOutDate && 
                                     request.CheckInDate < br.Booking.CheckOutDate));
            }

            var totalCount = await query.CountAsync();
            var rooms = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(r => new RoomCardDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                HotelName = r.Hotel.HotelName,
                RoomCode = r.RoomCode,
                RoomName = r.Name.ToString(),
                Occupancy = r.Occupancy,
                BedType = r.BedType.ToString(),
                Price = r.ROPrice,
                State = r.State.ToString()
            }).ToListAsync();

            return new RoomSearchResponse { Rooms = rooms, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
        }

        public async Task<RoomDetailsDto> GetRoomDetailsAsync(long roomId)
        {
            var room = await _context.HotelRooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) throw new Exception("Room not found.");

            return new RoomDetailsDto
            {
                Id = room.Id,
                HotelId = room.HotelId,
                HotelName = room.Hotel.HotelName,
                HotelLocation = room.Hotel.Governorate ?? room.Hotel.CityArea ?? "",
                HotelStarRating = room.Hotel.StarRating,
                RoomCode = room.RoomCode,
                RoomName = room.Name.ToString(),
                Occupancy = room.Occupancy,
                BedType = room.BedType.ToString(),
                Price = room.ROPrice,
                Availability = room.Quantity,
                State = room.State.ToString()
            };
        }

        // --- Bookings ---
        public async Task<BookingDto> CreateBookingAsync(long userId, CreateBookingRequest request)
        {
            if (request.CheckInDate >= request.CheckOutDate) throw new ArgumentException("Check-out date must be after check-in date.");
            if (request.CheckInDate < DateTime.Today) throw new ArgumentException("Check-in date cannot be in the past.");

            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == request.HotelId);
            if (hotel == null) throw new KeyNotFoundException("Hotel not found.");

            var requestedRoomCounts = request.Rooms.GroupBy(r => r.RoomId).ToDictionary(g => g.Key, g => g.Count());
            var uniqueRoomIds = requestedRoomCounts.Keys.ToList();

            var rooms = await _context.HotelRooms.Where(r => uniqueRoomIds.Contains(r.Id) && r.HotelId == request.HotelId).ToListAsync();
            if (rooms.Count != uniqueRoomIds.Count) throw new ArgumentException("Some rooms were not found or do not belong to this hotel.");

            foreach (var room in rooms)
            {
                int reqQuantity = requestedRoomCounts[room.Id];
                var overlappingBookingsCount = await _context.HotelBookingRooms
                    .Where(br => br.RoomId == room.Id &&
                                 br.Booking.Status != BookingStatus.Cancelled &&
                                 br.Booking.Status != BookingStatus.Completed &&
                                 br.Booking.CheckInDate < request.CheckOutDate && 
                                 request.CheckInDate < br.Booking.CheckOutDate)
                    .CountAsync();

                if (overlappingBookingsCount + reqQuantity > room.Quantity)
                    throw new InvalidOperationException($"Not enough availability for room '{room.Name}'.");
            }

            int nights = (request.CheckOutDate - request.CheckInDate).Days;
            
            var unsavedBookingRooms = new List<HotelBookingRoom>();
            decimal totalPrice = 0;

            foreach (var reqRoom in request.Rooms)
            {
                var room = rooms.First(r => r.Id == reqRoom.RoomId);
                decimal roomPrice = reqRoom.MealPlan switch {
                    "RO" => room.ROPrice ?? 0,
                    "BB" => room.BBPrice ?? room.ROPrice ?? 0,
                    "HB" => room.HBPrice ?? room.ROPrice ?? 0,
                    "FB" => room.FBPrice ?? room.ROPrice ?? 0,
                    "AI" => room.AIPrice ?? room.ROPrice ?? 0,
                    _ => room.ROPrice ?? 0
                };
                
                decimal subtotal = roomPrice * nights;
                totalPrice += subtotal;
                
                unsavedBookingRooms.Add(new HotelBookingRoom
                {
                    RoomId = room.Id,
                    RoomName = room.Name.ToString(),
                    MealPlan = reqRoom.MealPlan,
                    PricePerNight = roomPrice,
                    Nights = nights,
                    Subtotal = subtotal
                });
            }

            var booking = new HotelBooking
            {
                UserId = userId,
                HotelId = request.HotelId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Nights = nights,
                TotalRooms = request.Rooms.Count,
                TotalPrice = totalPrice,
                PaymentStatus = PaymentStatus.Pending,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _context.HotelBookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            foreach (var br in unsavedBookingRooms)
            {
                br.BookingId = booking.Id;
            }

            await _context.HotelBookingRooms.AddRangeAsync(unsavedBookingRooms);
            await _context.SaveChangesAsync();

            return MapBookingToDto(booking);
        }

        public async Task<List<BookingDto>> GetMyBookingsAsync(long userId)
        {
            var bookings = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.Payments)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings.Select(MapBookingToDto).ToList();
        }

        public async Task<List<BookingDto>> GetHotelBookingsAsync(long userId, long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null || hotel.UserId != userId) throw new UnauthorizedAccessException("You are not authorized to view bookings for this hotel.");

            var bookings = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Include(b => b.User)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.Payments)
                .Where(b => b.HotelId == hotelId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings.Select(MapBookingToDto).ToList();
        }

        public async Task<BookingDto> GetBookingByIdAsync(long userId, long bookingId)
        {
            var booking = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new KeyNotFoundException("Booking not found.");
            
            var currentUser = await _context.Users.FindAsync(userId);
            bool isAdmin = currentUser?.Role == UserRole.Admin;

            if (!isAdmin && booking.UserId != userId && booking.Hotel.UserId != userId) throw new UnauthorizedAccessException("Not authorized.");

            return MapBookingToDto(booking);
        }

        public async Task<CancelPreviewDto> PreviewCancelBookingAsync(long userId, long bookingId)
        {
            var booking = await _context.HotelBookings
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Policy)
                        .ThenInclude(p => p.CancellationRules)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new KeyNotFoundException("Booking not found.");
            
            if (booking.UserId != userId && booking.Hotel.UserId != userId) 
                throw new UnauthorizedAccessException("You are not authorized to preview cancellation for this booking.");

            if (booking.Status == BookingStatus.Cancelled) 
                throw new InvalidOperationException("Booking is already cancelled.");
            
            if (booking.Status == BookingStatus.Completed) 
                throw new InvalidOperationException("Cannot cancel a completed booking.");

            decimal refundPercentage = 100m;
            var strategy = booking.Hotel?.Policy?.CancellationStrategy ?? CancellationStrategy.FreeAll;
            string appliedRuleText = "";
            
            if (strategy == CancellationStrategy.NonRefundable)
            {
                refundPercentage = 0m;
                appliedRuleText = "Non-Refundable Policy Applied: This booking is non-refundable. You will not receive any refund upon cancellation.";
            }
            else if (strategy == CancellationStrategy.FreeAll)
            {
                refundPercentage = 100m;
                appliedRuleText = "Free Cancellation Policy Applied: You are eligible for a 100% refund.";
            }
            else if (strategy == CancellationStrategy.WindowBased && booking.Hotel?.Policy?.CancellationRules != null && booking.CheckInDate.HasValue)
            {
                var hoursBeforeCheckin = (booking.CheckInDate.Value - DateTime.UtcNow).TotalHours;
                
                var applicableRule = booking.Hotel.Policy.CancellationRules
                    .Where(r => (!r.FromHoursBeforeCheckIn.HasValue || hoursBeforeCheckin >= r.FromHoursBeforeCheckIn.Value) &&
                                (!r.ToHoursBeforeCheckIn.HasValue || hoursBeforeCheckin <= r.ToHoursBeforeCheckIn.Value))
                    .OrderByDescending(r => r.PenaltyPct)
                    .FirstOrDefault();
                    
                if (applicableRule != null)
                {
                    refundPercentage = 100m - applicableRule.PenaltyPct;
                    appliedRuleText = $"Window-Based Policy Match: The rule matching the window has a penalty of {applicableRule.PenaltyPct}%, resulting in a {refundPercentage}% refund.";
                }
                else
                {
                    refundPercentage = 100m;
                    appliedRuleText = $"Window-Based Policy: No penalty rule is configured for this time window. You are eligible for a 100% refund.";
                }
            }
            
            if (refundPercentage < 0) refundPercentage = 0;
            if (refundPercentage > 100) refundPercentage = 100;

            decimal refundAmount = booking.TotalPrice * (refundPercentage / 100m);
            decimal cancellationFee = booking.TotalPrice - refundAmount;

            if (booking.PaymentStatus != PaymentStatus.Paid && booking.PaymentStatus != PaymentStatus.Refunded)
            {
                refundAmount = 0m;
                cancellationFee = 0m;
                refundPercentage = 0m;
                appliedRuleText = "Payment Status Check: The booking has not been marked as Paid, so no refund can be processed.";
            }

            var rulesList = booking.Hotel?.Policy?.CancellationRules?
                .OrderBy(r => r.FromHoursBeforeCheckIn)
                .Select(r => new HotelCancellationRuleDto
                {
                    Id = r.Id,
                    FromHoursBeforeCheckIn = r.FromHoursBeforeCheckIn,
                    ToHoursBeforeCheckIn = r.ToHoursBeforeCheckIn,
                    PenaltyPct = r.PenaltyPct
                }).ToList() ?? new List<HotelCancellationRuleDto>();

            // Determine if Stripe refund is possible
            bool hasStripePayment = await _context.PaymentTransactionItems
                .Include(pti => pti.PaymentTransaction)
                .AnyAsync(pti => pti.BookingType == "Hotel" && pti.BookingId == bookingId && pti.PaymentTransaction.Provider == "Stripe" && pti.Status == "Paid");

            var availableRefundMethods = new List<string> { "Wallet" };
            if (hasStripePayment)
            {
                availableRefundMethods.Add("OriginalPaymentMethod");
            }

            return new CancelPreviewDto
            {
                BookingId = booking.Id,
                TotalPrice = booking.TotalPrice,
                RefundAmount = refundAmount,
                CancellationFee = cancellationFee,
                RefundPercentage = refundPercentage,
                PolicyStrategy = strategy.ToString(),
                CancellationRules = rulesList,
                AppliedRuleText = appliedRuleText,
                OriginalPaymentMethodAvailable = hasStripePayment,
                AvailableRefundMethods = availableRefundMethods
            };
        }

        public async Task<BookingDto> CancelBookingAsync(long userId, long bookingId, string reason, string? refundMethod = null)
        {
            var booking = await _context.HotelBookings
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Policy)
                        .ThenInclude(p => p.CancellationRules)
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) throw new KeyNotFoundException("Booking not found.");
            
            // Authorization: Either the user who booked or the hotel owner
            if (booking.UserId != userId && booking.Hotel.UserId != userId) 
                throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");

            if (booking.Status == BookingStatus.Cancelled) 
                throw new InvalidOperationException("Booking is already cancelled.");
            
            if (booking.Status == BookingStatus.Completed) 
                throw new InvalidOperationException("Cannot cancel a completed booking.");

            // Calculate refund if paid
            if (booking.PaymentStatus == PaymentStatus.Paid || booking.PaymentStatus == PaymentStatus.Refunded) 
            {
                if (booking.PaymentStatus != PaymentStatus.Refunded)
                {
                    decimal refundPercentage = 100m;
                    var strategy = booking.Hotel?.Policy?.CancellationStrategy ?? CancellationStrategy.FreeAll;
                    
                    if (strategy == CancellationStrategy.NonRefundable)
                    {
                        refundPercentage = 0m;
                    }
                    else if (strategy == CancellationStrategy.WindowBased && booking.Hotel?.Policy?.CancellationRules != null && booking.CheckInDate.HasValue)
                    {
                        var hoursBeforeCheckin = (booking.CheckInDate.Value - DateTime.UtcNow).TotalHours;
                        
                        var applicableRule = booking.Hotel.Policy.CancellationRules
                            .Where(r => (!r.FromHoursBeforeCheckIn.HasValue || hoursBeforeCheckin >= r.FromHoursBeforeCheckIn.Value) &&
                                        (!r.ToHoursBeforeCheckIn.HasValue || hoursBeforeCheckin <= r.ToHoursBeforeCheckIn.Value))
                            .OrderByDescending(r => r.PenaltyPct) // get highest applicable penalty
                            .FirstOrDefault();
                            
                        if (applicableRule != null)
                        {
                            refundPercentage = 100m - applicableRule.PenaltyPct;
                        }
                    }
                    
                    if (refundPercentage < 0) refundPercentage = 0;
                    if (refundPercentage > 100) refundPercentage = 100;

                    decimal refundAmount = booking.TotalPrice * (refundPercentage / 100m);
                    decimal cancellationFee = booking.TotalPrice - refundAmount;

                    booking.RefundAmount = refundAmount;
                    booking.CancellationFee = cancellationFee;

                    if (refundAmount > 0)
                    {
                        var transactionItem = await _context.PaymentTransactionItems
                            .Include(pti => pti.PaymentTransaction)
                            .FirstOrDefaultAsync(pti => pti.BookingType == "Hotel" && pti.BookingId == booking.Id && pti.Status == "Paid");

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
                            await _walletService.RefundToWalletAsync(booking.UserId, refundAmount, $"Refund-Hotel-{booking.Id}", $"Refund for cancelled hotel booking #{booking.Id}");
                        }
                    }
                    
                    // Prevent payout for this item
                    var itemToRefund = await _context.PaymentTransactionItems
                        .FirstOrDefaultAsync(pti => pti.BookingType == "Hotel" && pti.BookingId == booking.Id);
                    if (itemToRefund != null)
                    {
                        itemToRefund.Status = "Refunded";
                    }

                    booking.PaymentStatus = PaymentStatus.Refunded;
                }
            } // Added missing bracket

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledByUserId = userId;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.HotelBookings.Update(booking);
            await _context.SaveChangesAsync();
            
            return MapBookingToDto(booking);
        }

        public async Task<BookingDto> UpdateBookingStatusAsync(long userId, long bookingId, string status)
        {
            var booking = await _context.HotelBookings.Include(b => b.Hotel).Include(b => b.BookingRooms).FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) throw new KeyNotFoundException("Booking not found.");
            if (booking.Hotel.UserId != userId) throw new UnauthorizedAccessException("Not authorized.");

            if (!Enum.TryParse(status, out BookingStatus newStatus)) throw new ArgumentException("Invalid status.");

            booking.Status = newStatus;
            _context.HotelBookings.Update(booking);
            await _context.SaveChangesAsync();
            return MapBookingToDto(booking);
        }

        public async Task<PaymentResponseDto> ProcessPaymentAsync(long userId, ProcessPaymentRequest request)
        {
            var booking = await _context.HotelBookings
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.UserId == userId);

            if (booking == null) throw new KeyNotFoundException("Booking not found or not authorized.");
            
            if (booking.PaymentStatus == PaymentStatus.Paid) 
                throw new InvalidOperationException("Booking is already paid.");

            var payment = new HotelPayment
            {
                BookingId = booking.Id,
                UserId = userId,
                HotelId = booking.HotelId,
                Amount = booking.TotalPrice,
                PaymentMethod = request.PaymentMethod,
                TransactionId = "TRX-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                Status = HotelPaymentStatus.Paid,
                PaidAt = DateTime.UtcNow,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.HotelPayments.AddAsync(payment);

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.HotelBookings.Update(booking);
            await _context.SaveChangesAsync();
            
            return new PaymentResponseDto
            {
                BookingId = booking.Id,
                PaymentStatus = booking.PaymentStatus.ToString(),
                PaymentMethod = payment.PaymentMethod.ToString(),
                TransactionId = payment.TransactionId,
                PaidAt = payment.PaidAt
            };
        }

        // --- Helper Mappers ---
        private static RoomDto MapRoomToDto(HotelRoom room, int currentAvailability)
        {
            return new RoomDto
            {
                Id = room.Id,
                HotelId = room.HotelId,
                RoomCode = room.RoomCode,
                Name = room.Name,
                Occupancy = room.Occupancy,
                BedType = room.BedType,
                ROPrice = room.ROPrice,
                BBPrice = room.BBPrice,
                HBPrice = room.HBPrice,
                FBPrice = room.FBPrice,
                AIPrice = room.AIPrice,
                Quantity = room.Quantity,
                Price = room.ROPrice.GetValueOrDefault(),
                Availability = currentAvailability,
                InventoryCount = room.Quantity
            };
        }

        public async Task<List<BookingDto>> GetUserTripsAsync(long userId, string tab)
        {
            var query = _context.HotelBookings
                .Include(b => b.Hotel).ThenInclude(h => h.Images)
                .Include(b => b.BookingRooms)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (tab == "upcoming")
                query = query.Where(b => b.Status != BookingStatus.Cancelled && b.CheckInDate.HasValue && b.CheckInDate.Value > DateTime.UtcNow);
            else if (tab == "past")
                query = query.Where(b => b.Status != BookingStatus.Cancelled && b.CheckOutDate.HasValue && b.CheckOutDate.Value <= DateTime.UtcNow);
            else if (tab == "cancelled")
                query = query.Where(b => b.Status == BookingStatus.Cancelled);

            var bookings = await query.OrderByDescending(b => b.CheckInDate).ToListAsync();
            return bookings.Select(MapBookingToDto).ToList();
        }

        private static BookingDto MapBookingToDto(HotelBooking booking)
        {
            var checkInDate = booking.CheckInDate ?? DateTime.Now;
            var checkOutDate = booking.CheckOutDate ?? DateTime.Now;
            
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
                PropertyType = booking.Hotel?.PropertyType.ToString() ?? "Hotel",
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Nights = booking.Nights ?? 0,
                TotalRooms = booking.TotalRooms,
                TotalPrice = booking.TotalPrice,
                PaymentStatus = booking.PaymentStatus.ToString(),
                Status = displayStatus.ToString(),
                CreatedAt = booking.CreatedAt,
                ImageUrl = booking.Hotel?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? booking.Hotel?.Images?.FirstOrDefault()?.ImageUrl,
                RoomName = booking.BookingRooms?.FirstOrDefault()?.RoomName ?? "Various Rooms",
                CanCancel = booking.Status != BookingStatus.Cancelled && checkInDate > DateTime.UtcNow.AddDays(2),
                CanReview = booking.Status == BookingStatus.Completed || (booking.Status == BookingStatus.Confirmed && checkOutDate <= DateTime.UtcNow),
                CanRebook = checkOutDate <= DateTime.UtcNow || booking.Status == BookingStatus.Cancelled,
                CancellationReason = booking.CancellationReason,
                CancelledAt = booking.CancelledAt,
                CancelledByUserId = booking.CancelledByUserId,
                Payments = booking.Payments?.Select(p => new PaymentResponseDto
                {
                    BookingId = p.BookingId,
                    PaymentStatus = p.Status.ToString(),
                    PaymentMethod = p.PaymentMethod.ToString(),
                    TransactionId = p.TransactionId ?? "N/A",
                    PaidAt = p.PaidAt
                }).ToList() ?? new List<PaymentResponseDto>(),
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

        private static void ValidateHotelCore(Hotel hotel)
        {
            if (string.IsNullOrWhiteSpace(hotel.HotelName))
                throw new Exception("HotelName is required.");
            if (hotel.PriceUsd < 0)
                throw new Exception("PriceUsd cannot be negative.");
        }

        private async Task UpsertHotelContactsAsync(long hotelId, List<HotelContactInputDto> contacts)
        {
            if (contacts.Any(c => string.IsNullOrWhiteSpace(c.ContactValue)))
                throw new Exception("ContactValue is required.");

            _context.HotelContacts.RemoveRange(_context.HotelContacts.Where(c => c.HotelId == hotelId));
            foreach (var c in contacts)
            {
                _context.HotelContacts.Add(new HotelContact
                {
                    HotelId = hotelId,
                    ContactType = c.ContactType,
                    ContactValue = c.ContactValue.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task UpsertHotelDocumentsAsync(long hotelId, List<HotelDocumentInputDto> documents)
        {
            var typeIds = documents.Select(d => d.DocumentTypeId).Distinct().ToList();
            var validIds = await _context.DocumentTypes.Where(t => t.IsActive && typeIds.Contains(t.Id)).Select(t => t.Id).ToListAsync();
            if (validIds.Count != typeIds.Count)
                throw new Exception("Invalid or inactive document type.");

            _context.HotelDocuments.RemoveRange(_context.HotelDocuments.Where(d => d.HotelId == hotelId));
            foreach (var d in documents)
            {
                if (string.IsNullOrEmpty(d.FileUrl)) continue;

                _context.HotelDocuments.Add(new HotelDocument
                {
                    HotelId = hotelId,
                    DocumentTypeId = d.DocumentTypeId,
                    FileUrl = d.FileUrl,
                    Notes = d.Notes,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task UpsertHotelFieldValuesAsync(long hotelId, List<HotelFieldValueInputDto> values)
        {
            // 1. Check for duplicates in the request
            if(values.Select(v => v.FieldDefinitionId).Distinct().Count() != values.Count)
                throw new Exception("Duplicate field values sent for the same definition.");

            // 2. Validate against ALL currently active required definitions in DB
            var requiredDefs = await _context.HotelFieldDefinitions.Where(d => d.IsActive && d.IsRequired).ToListAsync();
            foreach (var req in requiredDefs)
            {
                if (!values.Any(v => v.FieldDefinitionId == req.Id && !string.IsNullOrWhiteSpace(v.Value)))
                    throw new Exception($"Mandatory Detail Missing: {req.DisplayName}");
            }

            // 3. Optional: Validate that all sent IDs actually exist and are active
            var sentIds = values.Select(v => v.FieldDefinitionId).ToList();
            var validCount = await _context.HotelFieldDefinitions.CountAsync(d => d.IsActive && sentIds.Contains(d.Id));
            if (validCount != sentIds.Count)
                throw new Exception("One or more field definitions are invalid or inactive.");

            // 4. Clear and replace
            _context.HotelFieldValues.RemoveRange(_context.HotelFieldValues.Where(v => v.HotelId == hotelId));
            foreach (var v in values)
            {
                _context.HotelFieldValues.Add(new HotelFieldValue
                {
                    HotelId = hotelId,
                    FieldDefinitionId = v.FieldDefinitionId,
                    Value = v.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task UpsertHotelPolicyAsync(long hotelId, UpdateHotelPolicyInputDto policyInput)
        {
            var policy = await _context.HotelPolicies.Include(p => p.CancellationRules).FirstOrDefaultAsync(p => p.HotelId == hotelId);
            if (policy == null)
            {
                policy = new HotelPolicy { HotelId = hotelId, CreatedAt = DateTime.UtcNow };
                _context.HotelPolicies.Add(policy);
            }

            if (policyInput.ServiceChargePct.HasValue) policy.ServiceChargePct = policyInput.ServiceChargePct.Value;
            if (policyInput.IncludeServiceCharge.HasValue) policy.IncludeServiceCharge = policyInput.IncludeServiceCharge.Value;
            if (policyInput.IncludeVat.HasValue) policy.IncludeVat = policyInput.IncludeVat.Value;
            if (policyInput.IncludeCityTax.HasValue) policy.IncludeCityTax = policyInput.IncludeCityTax.Value;
            if (policyInput.CancellationStrategy.HasValue) policy.CancellationStrategy = policyInput.CancellationStrategy.Value;
            policy.UpdatedAt = DateTime.UtcNow;

            if (policyInput.CancellationRules != null)
            {
                _context.HotelCancellationRules.RemoveRange(policy.CancellationRules);
                foreach (var r in policyInput.CancellationRules)
                {
                    _context.HotelCancellationRules.Add(new HotelCancellationRule
                    {
                        HotelPolicy = policy,
                        FromHoursBeforeCheckIn = r.FromHoursBeforeCheckIn,
                        ToHoursBeforeCheckIn = r.ToHoursBeforeCheckIn,
                        PenaltyPct = r.PenaltyPct,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task ReplaceHotelAmenitiesAsync(long hotelId, List<long> amenityIds)
        {
            var validIds = await _context.Amenities.Where(a => amenityIds.Contains(a.Id)).Select(a => a.Id).ToListAsync();
            if (validIds.Count != amenityIds.Distinct().Count())
                throw new Exception("Invalid amenity id.");

            _context.HotelAmenities.RemoveRange(_context.HotelAmenities.Where(x => x.HotelId == hotelId));
            foreach (var id in amenityIds.Distinct())
            {
                _context.HotelAmenities.Add(new HotelAmenity
                {
                    HotelId = hotelId,
                    AmenityId = id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        private static HotelAmenitiesDto ToAmenityDto(IEnumerable<string> names)
        {
            var set = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return new HotelAmenitiesDto
            {
                FreeWifi = set.Contains("FreeWifi"),
                SwimmingPool = set.Contains("SwimmingPool"),
                Parking = set.Contains("Parking"),
                AirConditioning = set.Contains("AirConditioning"),
                Breakfast = set.Contains("Breakfast"),
                Gym = set.Contains("Gym"),
                Restaurant = set.Contains("Restaurant"),
                Spa = set.Contains("Spa"),
                RoomService = set.Contains("RoomService")
            };
        }


        private async Task RecalculateHotelPriceAsync(long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return;

            var rooms = await _context.HotelRooms.Where(r => r.HotelId == hotelId && r.ROPrice.HasValue).ToListAsync();
            if (rooms.Any())
            {
                hotel.PriceUsd = rooms.Average(r => r.ROPrice.Value);
            }
            else
            {
                hotel.PriceUsd = 0;
            }

            hotel.UpdatedAt = DateTime.UtcNow;
            _context.Hotels.Update(hotel);
            await _context.SaveChangesAsync();
        }

        // Replaced by method at line 214

        public async Task<List<HotelDetailsDto>> GetAllApplicationsAsync()
        {
            var apps = await _context.Hotels
                .Include(h => h.User)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new HotelDetailsDto
                {
                    Id = h.Id,
                    HotelName = h.HotelName,
                    CityArea = h.CityArea,
                    Governorate = h.Governorate,
                    Country = h.Country,
                    TotalRooms = h.Rooms.Sum(r => r.Quantity),
                    Verified = h.Verified,
                    VerificationStatus = h.VerificationStatus.ToString(),
                    RejectionReason = h.RejectionReason,
                    UserId = h.UserId,
                    OwnerName = h.User != null ? h.User.Name : null,
                    OwnerEmail = h.User != null ? h.User.Email : null,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return apps;
        }

        public async Task<List<HotelDetailsDto>> GetPendingApplicationsAsync()
        {
            var apps = await _context.Hotels
                .Include(h => h.User)
                .Where(h => h.VerificationStatus == VerificationStatus.Pending || h.Verified == false)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new HotelDetailsDto
                {
                    Id = h.Id,
                    HotelName = h.HotelName,
                    CityArea = h.CityArea,
                    Governorate = h.Governorate,
                    Country = h.Country,
                    TotalRooms = h.Rooms.Sum(r => r.Quantity),
                    Verified = h.Verified,
                    VerificationStatus = h.VerificationStatus.ToString(),
                    RejectionReason = h.RejectionReason,
                    UserId = h.UserId,
                    OwnerName = h.User != null ? h.User.Name : null,
                    OwnerEmail = h.User != null ? h.User.Email : null,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return apps;
        }

        public async Task<bool> ApproveApplicationAsync(long hotelId)
        {
            var hotel = await _context.Hotels.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == hotelId);
            if (hotel == null) return false;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    hotel.Verified = true;
                    hotel.VerificationStatus = VerificationStatus.Verified;
                    hotel.VerificationDate = DateTime.UtcNow;
                    hotel.RejectionReason = null;
                    
                    // Update User Role to Hotel (if not already admin)
                    if (hotel.User != null && hotel.User.Role != UserRole.Admin)
                    {
                        hotel.User.Role = UserRole.Hotel;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> RejectApplicationAsync(long hotelId, string reason)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId);
            if (hotel == null) return false;

            hotel.Verified = false;
            hotel.VerificationStatus = VerificationStatus.Rejected;
            hotel.RejectionReason = reason;
            
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateTypeNorm(PropertyType? prop, AccommodationType? acc)
        {
            if (prop == null && acc == null) return "Unknown";
            if (prop == null) return acc.ToString();
            if (acc == null) return prop.ToString();
            return $"{prop} - {acc}";
        }

        public async Task<List<UserBookingMinimalDto>> GetEligibleBookingsForComplaintAsync(long userId)
        {
            var bookings = await _context.HotelBookings
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            return bookings.Select(b => new UserBookingMinimalDto
            {
                BookingId = b.Id,
                HotelId = b.HotelId,
                HotelName = b.Hotel?.HotelName ?? "Unknown Hotel",
                City = b.Hotel?.CityArea ?? (b.Hotel?.Governorate ?? "N/A"),
                RoomName = string.Join(", ", b.BookingRooms.Select(br => br.RoomName ?? "Room")),
                Dates = $"{(b.CheckInDate?.ToString("dd MMM yyyy") ?? "N/A")} - {(b.CheckOutDate?.ToString("dd MMM yyyy") ?? "N/A")}",
                TotalPrice = $"${b.TotalPrice:N2}"
            }).ToList();
        }

        public async Task<long> CreateComplaintAsync(long userId, ComplaintCreateDto dto)
        {
            var complaint = new Complaint
            {
                UserId = userId,
                ComplaintType = dto.ComplaintType,
                Subject = dto.Subject,
                Message = dto.Message,
                Status = ComplaintStatus.Pending,
                Priority = ComplaintPriority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (dto.ComplaintType == ComplaintType.Booking)
            {
                if (!dto.BookingId.HasValue) 
                    throw new ArgumentException("Booking ID is required for booking complaints.");
                
                var booking = await _context.HotelBookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.UserId == userId);
                if (booking == null) 
                    throw new UnauthorizedAccessException("Booking not found or does not belong to you.");
                
                complaint.BookingId = dto.BookingId;
                complaint.HotelId = booking.HotelId;
            }
            else // Platform Complaint
            {
                if (dto.BookingId.HasValue)
                    throw new ArgumentException("Booking ID should not be provided for platform complaints.");

                complaint.BookingId = null;
                complaint.HotelId = null;
            }

            // Handling attachments before SaveChanges to ensure Atomicity
            if (dto.Attachments != null && dto.Attachments.Count > 0)
            {
                foreach (var file in dto.Attachments)
                {
                    // FileService throws on invalid extensions, failing the whole operation (Atomic)
                    var fileUrl = await _fileService.SaveComplaintAttachmentAsync(file);
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        complaint.Attachments.Add(new ComplaintAttachment
                        {
                            FileUrl = fileUrl,
                            ReplyId = null, // Global Integrity Rule: initial complaint evidence has no reply id
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            return complaint.Id;
        }

        public async Task<bool> UpdateComplaintAsync(long userId, long complaintId, ComplaintCreateDto dto)
        {
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.UserId == userId);

            if (complaint == null) throw new KeyNotFoundException("Complaint not found.");
            if (complaint.Status != ComplaintStatus.Pending) throw new InvalidOperationException("Only pending complaints can be edited.");

            complaint.Subject = dto.Subject;
            complaint.Message = dto.Message;
            complaint.ComplaintType = dto.ComplaintType;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (dto.ComplaintType == ComplaintType.Booking && dto.BookingId.HasValue)
            {
                var booking = await _context.HotelBookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.UserId == userId);
                if (booking == null) throw new UnauthorizedAccessException("Booking not found or does not belong to you.");
                
                complaint.BookingId = dto.BookingId;
                complaint.HotelId = booking.HotelId;
            }
            else
            {
                complaint.BookingId = null;
                complaint.HotelId = null;
            }

            // Optional: Remove existing attachments
            if (dto.RemovedAttachmentIds != null && dto.RemovedAttachmentIds.Count > 0)
            {
                var attachmentsToRemove = await _context.ComplaintAttachments
                    .Where(a => a.ComplaintId == complaintId && dto.RemovedAttachmentIds.Contains(a.Id))
                    .ToListAsync();
                
                if (attachmentsToRemove.Any())
                {
                    _context.ComplaintAttachments.RemoveRange(attachmentsToRemove);
                }
            }

            // Optional: New attachments
            if (dto.Attachments != null && dto.Attachments.Count > 0)
            {
                foreach (var file in dto.Attachments)
                {
                    var fileUrl = await _fileService.SaveComplaintAttachmentAsync(file);
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        _context.ComplaintAttachments.Add(new ComplaintAttachment
                        {
                            ComplaintId = complaint.Id,
                            ReplyId = null, // Global Integrity Rule: initial complaint evidence has no reply id
                            FileUrl = fileUrl,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ComplaintSummaryDto>> GetMyComplaintsAsync(long userId)
        {
            return await _context.Complaints
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ComplaintSummaryDto
                {
                    Id = c.Id,
                    ComplaintType = c.ComplaintType.ToString(),
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt
                }).ToListAsync();
        }

        public async Task<ComplaintDetailsDto> GetComplaintDetailsAsync(long userId, long complaintId)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .Include(c => c.Booking).ThenInclude(b => b.BookingRooms)
                .Include(c => c.Attachments)
                .Include(c => c.Replies).ThenInclude(r => r.AdminUser)
                .Include(c => c.Replies).ThenInclude(r => r.Attachments)
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.UserId == userId);

            if (complaint == null) throw new KeyNotFoundException("Complaint not found.");

            return new ComplaintDetailsDto
            {
                Id = complaint.Id,
                ComplaintType = complaint.ComplaintType.ToString(),
                BookingId = complaint.BookingId,
                HotelName = complaint.Booking?.Hotel?.HotelName ?? "Platform/Unknown",
                HotelCity = complaint.Booking?.Hotel != null 
                    ? (complaint.Booking.Hotel.CityArea ?? complaint.Booking.Hotel.Governorate ?? "N/A") 
                    : "N/A",
                RoomName = (complaint.Booking?.BookingRooms != null && complaint.Booking.BookingRooms.Any()) 
                    ? string.Join(", ", complaint.Booking.BookingRooms.Select(br => br.RoomName ?? "Room")) 
                    : null,
                BookingDates = complaint.Booking != null 
                    ? $"{(complaint.Booking.CheckInDate?.ToString("dd MMM yyyy") ?? "N/A")} - {(complaint.Booking.CheckOutDate?.ToString("dd MMM yyyy") ?? "N/A")}" 
                    : "N/A",
                TotalPrice = complaint.Booking != null ? $"${complaint.Booking.TotalPrice:N2}" : null,
                Subject = complaint.Subject,
                Message = complaint.Message,
                Status = complaint.Status.ToString(),
                Priority = complaint.Priority.ToString(),
                CreatedAt = complaint.CreatedAt,
                Attachments = complaint.Attachments?
                    .Where(a => a.ReplyId == null)
                    .Select(a => new ComplaintAttachmentDto
                    {
                        Id = a.Id,
                        FileUrl = a.FileUrl
                    }).ToList() ?? new List<ComplaintAttachmentDto>(),
                Replies = complaint.Replies?.Select(r => new ComplaintReplyDto
                {
                    Id = r.Id,
                    SenderName = r.AdminUser?.UserName ?? "Support Team",
                    ReplyMessage = r.ReplyMessage,
                    CreatedAt = r.CreatedAt,
                    Attachments = r.Attachments?.Select(ra => new ComplaintAttachmentDto
                    {
                        Id = ra.Id,
                        FileUrl = ra.FileUrl
                    }).ToList() ?? new List<ComplaintAttachmentDto>()
                }).ToList() ?? new List<ComplaintReplyDto>()
            };
        }

        public async Task<List<AdminComplaintSummaryDto>> GetAdminComplaintsAsync(string? type, string? status, string? search, long? bookingId, string? hotelName)
        {
            var query = _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ComplaintType>(type, true, out var typeEnum))
                query = query.Where(c => c.ComplaintType == typeEnum);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ComplaintStatus>(status, true, out var statusEnum))
                query = query.Where(c => c.Status == statusEnum);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Subject.Contains(search) || 
                                         (c.User != null && c.User.UserName.Contains(search)) || 
                                         (c.Booking != null && c.Booking.Hotel.HotelName.Contains(search)));
            }

            if (bookingId.HasValue)
                query = query.Where(c => c.BookingId == bookingId);

            if (!string.IsNullOrEmpty(hotelName))
                query = query.Where(c => c.Booking != null && c.Booking.Hotel.HotelName.Contains(hotelName));

            var list = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return list.Select(c => new AdminComplaintSummaryDto
            {
                Id = c.Id,
                ComplaintType = c.ComplaintType.ToString(),
                BookingId = c.BookingId,
                FromUser = c.User?.UserName ?? "Unknown",
                Regarding = c.ComplaintType == ComplaintType.Booking && c.Booking?.Hotel != null 
                    ? (c.Booking.Hotel.HotelName ?? "Unknown Hotel") 
                    : "Platform",
                Subject = c.Subject,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<AdminComplaintDetailsDto> GetAdminComplaintDetailsAsync(long complaintId)
        {
            var c = await _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Attachments)
                .Include(c => c.Replies).ThenInclude(r => r.AdminUser)
                .Include(c => c.Replies).ThenInclude(r => r.Attachments)
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .Include(c => c.Booking).ThenInclude(b => b.BookingRooms)
                .Include(c => c.Booking).ThenInclude(b => b.Payments)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (c == null) throw new KeyNotFoundException("Complaint not found.");

            var dto = new AdminComplaintDetailsDto
            {
                Id = c.Id,
                ComplaintType = c.ComplaintType.ToString(),
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                CreatedAt = c.CreatedAt,
                FromUser = c.User?.UserName ?? "Unknown",
                Subject = c.Subject,
                Message = c.Message,
                Attachments = c.Attachments
                    .Where(a => a.ReplyId == null)
                    .Select(a => new ComplaintAttachmentDto
                    {
                        Id = a.Id,
                        FileUrl = a.FileUrl
                    }).ToList(),
                Replies = c.Replies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new ComplaintReplyDto
                    {
                        Id = r.Id,
                        AdminUserId = r.AdminUserId,
                        SenderName = r.AdminUser?.UserName ?? "Support",
                        ReplyMessage = r.ReplyMessage,
                        CreatedAt = r.CreatedAt,
                        Attachments = r.Attachments.Select(ra => new ComplaintAttachmentDto
                        {
                            Id = ra.Id,
                            FileUrl = ra.FileUrl
                        }).ToList()
                    }).ToList()
            };

            if (c.ComplaintType == ComplaintType.Booking && c.Booking != null)
            {
                var b = c.Booking;
                var paidPayment = b.Payments.Where(p => p.Status == HotelPaymentStatus.Paid).OrderByDescending(p => p.CreatedAt).FirstOrDefault();

                dto.BookingContext = new AdminBookingContextDto
                {
                    BookingId = b.Id,
                    HotelName = b.Hotel?.HotelName ?? "Unknown Hotel",
                    City = b.Hotel?.CityArea ?? b.Hotel?.Governorate ?? "N/A",
                    RoomName = string.Join(", ", b.BookingRooms.Select(br => br.RoomName ?? "Room")),
                    BookingDates = (b.CheckInDate.HasValue && b.CheckOutDate.HasValue)
                        ? b.CheckInDate.Value.ToString("dd MMM yyyy") + " - " + b.CheckOutDate.Value.ToString("dd MMM yyyy")
                        : "N/A",
                    Nights = b.Nights ?? 0,
                    TotalRooms = b.TotalRooms,
                    TotalPrice = "$" + b.TotalPrice.ToString("N2"),
                    BookingStatus = b.Status.ToString(),
                    PaymentStatus = b.PaymentStatus.ToString(),
                    PaymentMethod = paidPayment?.PaymentMethod.ToString(),
                    TransactionId = paidPayment?.TransactionId,
                    PaidAt = paidPayment?.CreatedAt,
                    RefundAmount = b.RefundAmount
                };
            }

            return dto;
        }

        public async Task<bool> AdminReplyToComplaintAsync(long adminUserId, long complaintId, AdminReplyCreateDto dto)
        {
            var complaint = await _context.Complaints.FindAsync(complaintId);
            if (complaint == null) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reply = new ComplaintReply
                {
                    ComplaintId = complaintId,
                    AdminUserId = adminUserId,
                    ReplyMessage = dto.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ComplaintReplies.Add(reply);
                await _context.SaveChangesAsync(); 

                if (dto.Attachments != null && dto.Attachments.Count > 0)
                {
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "complaints");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    foreach (var file in dto.Attachments)
                    {
                        if (file.Length == 0) continue;

                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // ATTACHMENT INTEGRITY: Link to ReplyId ONLY (not both)
                        _context.ComplaintAttachments.Add(new ComplaintAttachment
                        {
                            ReplyId = reply.Id,
                            ComplaintId = null, // Global Integrity Rule
                            FileUrl = $"/uploads/complaints/{fileName}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (complaint.Status == ComplaintStatus.Pending || complaint.Status == ComplaintStatus.Resolved)
                {
                    complaint.Status = ComplaintStatus.InReview;
                }
                complaint.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ResolveComplaintAsync(long complaintId)
        {
            var complaint = await _context.Complaints.FindAsync(complaintId);
            if (complaint == null) return false;

            // BACKEND GUARD: Only resolve if not already resolved/closed
            if (complaint.Status == ComplaintStatus.Resolved || complaint.Status == ComplaintStatus.Closed)
                return true;

            complaint.Status = ComplaintStatus.Resolved;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteComplaintAsync(long userId, long complaintId)
        {
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.UserId == userId);

            if (complaint == null) return false;

            // Only allow deleting Pending complaints
            if (complaint.Status != ComplaintStatus.Pending)
                throw new InvalidOperationException("Only pending complaints can be deleted.");

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAdminReplyAsync(long adminUserId, long replyId)
        {
            var reply = await _context.ComplaintReplies.FindAsync(replyId);
            if (reply == null) return false;

            if (reply.AdminUserId != adminUserId)
                throw new UnauthorizedAccessException("You can only delete your own replies.");

            _context.ComplaintReplies.Remove(reply);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditAdminReplyAsync(long adminUserId, long replyId, AdminReplyEditDto dto)
        {
            var reply = await _context.ComplaintReplies
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null) return false;

            if (reply.AdminUserId != adminUserId)
                throw new UnauthorizedAccessException("You can only edit your own replies.");

            // 1. Update text
            reply.ReplyMessage = dto.Message;

            // 2. Remove attachments
            if (dto.RemovedAttachmentIds != null && dto.RemovedAttachmentIds.Any())
            {
                var toRemove = reply.Attachments.Where(a => dto.RemovedAttachmentIds.Contains(a.Id)).ToList();
                foreach (var att in toRemove)
                {
                    _context.ComplaintAttachments.Remove(att);
                    reply.Attachments.Remove(att); // Clean up collection too
                }
            }

            // 3. Add new attachments
            if (dto.NewAttachments != null && dto.NewAttachments.Any())
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "complaints");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                foreach (var file in dto.NewAttachments)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    reply.Attachments.Add(new ComplaintAttachment
                    {
                        ReplyId = reply.Id,
                        ComplaintId = null,
                        FileUrl = "/uploads/complaints/" + fileName,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Database Error: {ex.Message} -> {ex.InnerException?.Message}");
            }
        }

        public async Task<PaginatedAdminReviewsResponse> GetAdminReviewsAsync(int pageNumber, int pageSize, string? userName, string? hotelName, string? keyword, int? rating)
        {
            var query = _context.HotelReviews
                .Include(r => r.User)
                .Include(r => r.Hotel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var s = userName.ToLower();
                query = query.Where(r => r.User != null && r.User.UserName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(hotelName))
            {
                var s = hotelName.ToLower();
                query = query.Where(r => r.Hotel != null && r.Hotel.HotelName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var s = keyword.ToLower();
                query = query.Where(r => r.Comment != null && r.Comment.ToLower().Contains(s));
            }

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminHotelReviewDto
                {
                    Id = r.Id,
                    UserName = r.User != null ? r.User.UserName : "Unknown",
                    HotelName = r.Hotel != null ? r.Hotel.HotelName : "Unknown",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return new PaginatedAdminReviewsResponse
            {
                Items = items,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<bool> DeleteReviewAsync(long reviewId)
        {
            var review = await _context.HotelReviews.FindAsync(reviewId);
            if (review == null) return false;

            _context.HotelReviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        // 13. Commission Settings
        public async Task<CommissionSettingDto?> GetCurrentCommissionSettingAsync()
        {
            var current = await _context.CommissionSettings
                .Include(c => c.CreatedByAdminUser)
                .Where(c => c.IsActive && c.EffectiveTo == null)
                .FirstOrDefaultAsync();

            if (current == null) return null;

            return MapToDto(current);
        }

        public async Task<List<CommissionSettingDto>> GetCommissionSettingsHistoryAsync()
        {
            var history = await _context.CommissionSettings
                .Include(c => c.CreatedByAdminUser)
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.EffectiveFrom)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return history.Select(MapToDto).ToList();
        }

        public async Task<CommissionSettingDto> SaveCommissionSettingAsync(long adminUserId, CreateCommissionSettingRequest request)
        {
            if (request.CityTaxMode == CityTaxMode.Percentage && request.CityTaxValue > 100)
            {
                throw new ArgumentException("City Tax Percentage cannot exceed 100.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                var currentActive = await _context.CommissionSettings
                    .Where(c => c.IsActive && c.EffectiveTo == null)
                    .FirstOrDefaultAsync();

                if (currentActive != null)
                {
                    currentActive.IsActive = false;
                    currentActive.EffectiveTo = now;
                    _context.CommissionSettings.Update(currentActive);
                }

                var newSetting = new CommissionSetting
                {
                    PlatformCommissionPct = request.PlatformCommissionPct,
                    VatPct = request.VatPct,
                    CityTaxMode = request.CityTaxMode,
                    CityTaxValue = request.CityTaxValue,
                    IsActive = true,
                    EffectiveFrom = now,
                    EffectiveTo = null,
                    CreatedAt = now,
                    CreatedByAdminUserId = adminUserId
                };

                await _context.CommissionSettings.AddAsync(newSetting);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                // Load navigation property before returning DTO
                await _context.Entry(newSetting).Reference(c => c.CreatedByAdminUser).LoadAsync();

                return MapToDto(newSetting);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private CommissionSettingDto MapToDto(CommissionSetting c)
        {
            return new CommissionSettingDto
            {
                Id = c.Id,
                PlatformCommissionPct = c.PlatformCommissionPct,
                VatPct = c.VatPct,
                CityTaxMode = c.CityTaxMode.ToString(),
                CityTaxValue = c.CityTaxValue,
                IsActive = c.IsActive,
                EffectiveFrom = c.EffectiveFrom,
                EffectiveTo = c.EffectiveTo,
                CreatedAt = c.CreatedAt,
                CreatedByAdminName = c.CreatedByAdminUser != null ? c.CreatedByAdminUser.UserName : "System"
            };
        }

        // --- My Bookings ---

        public async Task<List<MyTripHotelDto>> GetMyTripsAsync(long userId, string tab)
        {
            var query = _context.HotelBookings
                .Include(b => b.Hotel).ThenInclude(h => h.Images)
                .Include(b => b.BookingRooms)
                .Where(b => b.UserId == userId &&
                            b.Hotel.Verified &&
                            (b.Hotel.VerificationStatus == VerificationStatus.Verified || b.Hotel.VerificationStatus == VerificationStatus.Approved) &&
                            (b.PaymentStatus == PaymentStatus.Paid || b.PaymentStatus == PaymentStatus.Refunded));

            // Use Egypt time (+3) to ensure dates roll over correctly at local midnight
            DateTime today = DateTime.UtcNow.AddHours(3).Date;

            if (tab.Equals("upcoming", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) && b.CheckOutDate >= today);
            }
            else if (tab.Equals("past", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => 
                    (b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.Completed || ((b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn) && b.CheckOutDate < today)) 
                    && b.Status != BookingStatus.Cancelled);
            }
            else if (tab.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.Status == BookingStatus.Cancelled);
            }
            else
            {
                query = query.Where(b => false);
            }

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            var userHotelReviews = await _context.HotelReviews
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var trips = bookings.Select(b =>
            {
                var primaryImg = b.Hotel.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl;
                var roomName = b.BookingRooms?.FirstOrDefault()?.RoomName ?? "Standard Room";

                bool isPast = b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.Completed || b.CheckOutDate < today;
                var existingReview = userHotelReviews.FirstOrDefault(r => r.HotelId == b.HotelId);
                bool hasReviewed = existingReview != null;
                bool canReview = isPast && !hasReviewed;

                return new MyTripHotelDto
                {
                    BookingId = b.Id,
                    HotelId = b.HotelId,
                    HotelName = b.Hotel.HotelName,
                    ImageUrl = primaryImg,
                    RoomName = roomName,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    TotalPrice = b.TotalPrice,
                    BookingStatus = b.Status.ToString(),
                    RefundAmount = b.RefundAmount,
                    CancellationFee = b.CancellationFee,
                    CanCancel = (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) && b.CheckInDate > today,
                    CanRebook = true,
                    CanReview = canReview,
                    HasReviewed = hasReviewed,
                    ReviewId = existingReview?.Id,
                    ReviewRating = existingReview?.Rating,
                    ReviewComment = existingReview?.Comment
                };
            }).ToList();

            return trips;
        }

        // --- Admin Management and Dashboard ---

        public async Task<AdminDashboardKpiDto> GetAdminDashboardKpisAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var verifiedHotels = _context.Hotels
                .Where(h => h.Verified && 
                            (h.VerificationStatus == VerificationStatus.Verified || 
                             h.VerificationStatus == VerificationStatus.Approved));

            var totalRevenue = await (from p in _context.HotelPayments
                                      join h in verifiedHotels on p.HotelId equals h.Id
                                      where p.Status == HotelPaymentStatus.Paid
                                      select (decimal?)p.Amount).SumAsync() ?? 0m;

            var revenueThisMonth = await (from p in _context.HotelPayments
                                          join h in verifiedHotels on p.HotelId equals h.Id
                                          where p.Status == HotelPaymentStatus.Paid && 
                                                p.PaidAt != null && 
                                                p.PaidAt >= startOfMonth && 
                                                p.PaidAt < startOfNextMonth
                                          select (decimal?)p.Amount).SumAsync() ?? 0m;

            var validBookingStatuses = new[] { BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.CheckedOut };
            var totalBookings = await (from b in _context.HotelBookings
                                       join h in verifiedHotels on b.HotelId equals h.Id
                                       where validBookingStatuses.Contains(b.Status)
                                       select b.Id).CountAsync();

            var topHotels = await (from h in verifiedHotels
                                   join b in _context.HotelBookings on h.Id equals b.HotelId
                                   where validBookingStatuses.Contains(b.Status)
                                   group b by new { h.Id, h.HotelName } into g
                                   orderby g.Count() descending
                                   select new TopHotelDto
                                   {
                                       HotelId = g.Key.Id,
                                       HotelName = g.Key.HotelName,
                                       BookingsCount = g.Count()
                                   })
                                   .Take(10)
                                   .ToListAsync();

            var topCities = await (from p in _context.HotelPayments
                                   join h in verifiedHotels on p.HotelId equals h.Id
                                   where p.Status == HotelPaymentStatus.Paid
                                   group p by h.Governorate into g
                                   orderby g.Sum(x => (decimal?)x.Amount) descending
                                   select new TopCityDto
                                   {
                                       CityName = g.Key ?? "Unknown",
                                       TotalRevenue = g.Sum(x => (decimal?)x.Amount) ?? 0m
                                   })
                                   .Take(10)
                                   .ToListAsync();

            return new AdminDashboardKpiDto
            {
                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                TotalBookings = totalBookings,
                TotalHotels = await verifiedHotels.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalFlights = await _context.Flights.CountAsync(),
                TotalTours = await _context.Tours.CountAsync(),
                PlatformCommission = null,
                PlatformCommissionSupported = false,
                CommissionThisMonth = null,
                CommissionThisMonthSupported = false,
                TopHotels = topHotels,
                TopCities = topCities
            };
        }

        public async Task<AdminChartDataDto> GetAdminChartDataAsync(int? selectedYear)
        {
            var currentYear = DateTime.UtcNow.Year;
            var startYear = currentYear - 3;

            if (selectedYear.HasValue && (selectedYear < startYear || selectedYear > currentYear))
            {
                selectedYear = currentYear;
            }

            var verifiedHotels = _context.Hotels
                .Where(h => h.Verified &&
                            (h.VerificationStatus == VerificationStatus.Verified ||
                             h.VerificationStatus == VerificationStatus.Approved));

            var targetYears = Enumerable.Range(startYear, 4).ToList();
            var platformGrowthData = await (from p in _context.HotelPayments
                                            join h in verifiedHotels on p.HotelId equals h.Id
                                            where p.Status == HotelPaymentStatus.Paid &&
                                                  p.PaidAt != null &&
                                                  p.PaidAt.Value.Year >= startYear &&
                                                  p.PaidAt.Value.Year <= currentYear
                                            group p by p.PaidAt.Value.Year into g
                                            select new YearlyGrowthDto
                                            {
                                                Year = g.Key,
                                                TotalPaidAmount = g.Sum(x => (decimal?)x.Amount) ?? 0m
                                            })
                                            .OrderBy(x => x.Year)
                                            .ToListAsync();

            var finalGrowth = targetYears.Select(y => 
                platformGrowthData.FirstOrDefault(d => d.Year == y) ?? new YearlyGrowthDto { Year = y, TotalPaidAmount = 0m }
            ).ToList();

            var validBookingStatuses = new[] { BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.CheckedOut };

            var trendQuery = from b in _context.HotelBookings
                             join h in verifiedHotels on b.HotelId equals h.Id
                             where validBookingStatuses.Contains(b.Status)
                             select b;

            if (selectedYear.HasValue)
            {
                trendQuery = trendQuery.Where(b => b.CreatedAt.Year == selectedYear.Value);
            }

            var monthlyBookingsData = await trendQuery
                                             .GroupBy(b => b.CreatedAt.Month)
                                             .Select(g => new
                                             {
                                                 MonthNumber = g.Key,
                                                 BookingsCount = g.Count()
                                             })
                                             .ToListAsync();

            var finalTrend = Enumerable.Range(1, 12).Select(m => new MonthlyTrendDto
            {
                MonthNumber = m,
                MonthName = new DateTime(currentYear, m, 1).ToString("MMM"),
                BookingsCount = monthlyBookingsData.FirstOrDefault(x => x.MonthNumber == m)?.BookingsCount ?? 0
            }).ToList();

            var distQuery = from br in _context.HotelBookingRooms
                            join b in _context.HotelBookings on br.BookingId equals b.Id
                            join h in verifiedHotels on b.HotelId equals h.Id
                            where validBookingStatuses.Contains(b.Status)
                            select new { br.PricePerNight, b.CreatedAt };

            if (selectedYear.HasValue)
            {
                distQuery = distQuery.Where(x => x.CreatedAt.Year == selectedYear.Value);
            }

            var distributionData = await distQuery
                                          .GroupBy(x => 1)
                                          .Select(g => new
                                          {
                                              r0_50 = g.Count(x => x.PricePerNight >= 0 && x.PricePerNight < 50),
                                              r50_100 = g.Count(x => x.PricePerNight >= 50 && x.PricePerNight < 100),
                                              r100_200 = g.Count(x => x.PricePerNight >= 100 && x.PricePerNight < 200),
                                              r200_300 = g.Count(x => x.PricePerNight >= 200 && x.PricePerNight < 300),
                                              r300_500 = g.Count(x => x.PricePerNight >= 300 && x.PricePerNight < 500),
                                              r500plus = g.Count(x => x.PricePerNight >= 500)
                                          })
                                          .FirstOrDefaultAsync();

            var finalDistribution = new List<DistributionRangeDto>
            {
                new DistributionRangeDto { RangeLabel = "$0 - $50", Count = distributionData?.r0_50 ?? 0 },
                new DistributionRangeDto { RangeLabel = "$50 - $100", Count = distributionData?.r50_100 ?? 0 },
                new DistributionRangeDto { RangeLabel = "$100 - $200", Count = distributionData?.r100_200 ?? 0 },
                new DistributionRangeDto { RangeLabel = "$200 - $300", Count = distributionData?.r200_300 ?? 0 },
                new DistributionRangeDto { RangeLabel = "$300 - $500", Count = distributionData?.r300_500 ?? 0 },
                new DistributionRangeDto { RangeLabel = "$500+", Count = distributionData?.r500plus ?? 0 }
            };

            var earningsData = await (from p in _context.HotelPayments
                                      join h in verifiedHotels on p.HotelId equals h.Id
                                      where p.Status == HotelPaymentStatus.Paid &&
                                            p.PaidAt != null &&
                                            p.PaidAt.Value.Year == (selectedYear ?? currentYear)
                                      group p by p.PaidAt.Value.Month into g
                                      select new
                                      {
                                          MonthNumber = g.Key,
                                          TotalEarnings = g.Sum(x => (decimal?)x.Amount) ?? 0m
                                      })
                                      .ToListAsync();

            var finalEarnings = Enumerable.Range(1, 12).Select(m => new MonthlyEarningsDto
            {
                MonthName = new DateTime(currentYear, m, 1).ToString("MMM"),
                TotalEarnings = earningsData.FirstOrDefault(x => x.MonthNumber == m)?.TotalEarnings ?? 0m
            }).ToList();

            return new AdminChartDataDto
            {
                PlatformGrowth = finalGrowth,
                BookingsTrend = finalTrend,
                BookingValueDistribution = finalDistribution,
                EarningsTrend = finalEarnings
            };
        }

        public async Task<List<HotelManageSummaryDto>> GetHotelManagementListAsync(string? searchQuery, VerificationStatus? status, string? city)
        {
            var query = _context.Hotels
                .Where(h => h.VerificationStatus == VerificationStatus.Approved || 
                            h.VerificationStatus == VerificationStatus.Verified ||
                            h.VerificationStatus == VerificationStatus.Suspended || 
                            h.VerificationStatus == VerificationStatus.Banned)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(h => h.HotelName.Contains(searchQuery));
            }

            if (status.HasValue)
            {
                query = query.Where(h => h.VerificationStatus == status.Value);
            }

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(h => (h.CityArea != null && h.CityArea.Contains(city)) || (h.Governorate != null && h.Governorate.Contains(city)));
            }

            var hotels = await query
                .Select(h => new HotelManageSummaryDto
                {
                    HotelId = h.Id,
                    HotelName = h.HotelName,
                    City = h.CityArea ?? h.Governorate ?? "Unknown",
                    Status = h.VerificationStatus,
                    IsActive = h.Active,
                    TotalBookings = h.Bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut),
                    TotalRevenue = _context.HotelPayments
                        .Where(p => p.HotelId == h.Id && p.Status == HotelPaymentStatus.Paid)
                        .Sum(p => (decimal?)p.Amount) ?? 0,
                    CanApprove = h.VerificationStatus == VerificationStatus.Rejected || h.VerificationStatus == VerificationStatus.Suspended || h.VerificationStatus == VerificationStatus.Banned,
                    CanSuspend = h.VerificationStatus == VerificationStatus.Approved || h.VerificationStatus == VerificationStatus.Verified,
                    CanBan = h.VerificationStatus != VerificationStatus.Banned
                })
                .ToListAsync();

            return hotels;
        }

        public async Task<bool> SuspendHotelAsync(long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return false;
            hotel.VerificationStatus = VerificationStatus.Suspended;
            hotel.Active = false;
            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BanHotelAsync(long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return false;
            hotel.VerificationStatus = VerificationStatus.Banned;
            hotel.Active = false;
            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveHotelAsync(long hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return false;
            
            if (!hotel.Verified) throw new InvalidOperationException("Hotel must complete the initial verification flow first.");

            hotel.VerificationStatus = VerificationStatus.Approved;
            hotel.Active = true;
            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdminBookingPaginationResponse> GetAdminBookingsAsync(AdminBookingSearchRequest request)
        {
            var query = _context.HotelBookings
                .Include(b => b.User)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.BookingId))
            {
                var searchIdStr = request.BookingId.Replace("#BK-", "").Replace("#", "");
                if (long.TryParse(searchIdStr, out long bid))
                    query = query.Where(b => b.Id == bid);
            }

            if (!string.IsNullOrEmpty(request.Hotel))
                query = query.Where(b => b.Hotel.HotelName.Contains(request.Hotel));

            if (request.CheckIn.HasValue)
            {
                var d = request.CheckIn.Value.Date;
                query = query.Where(b => b.CheckInDate >= d && b.CheckInDate < d.AddDays(1));
            }

            if (request.CheckOut.HasValue)
            {
                var d = request.CheckOut.Value.Date;
                query = query.Where(b => b.CheckOutDate >= d && b.CheckOutDate < d.AddDays(1));
            }

            if (request.Status.HasValue)
                query = query.Where(b => b.Status == request.Status.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            if (request.PageNumber < 1) request.PageNumber = 1;

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = bookings.Select(b => new AdminBookingItemDto
            {
                BookingId = b.Id,
                User = b.User?.UserName ?? "Unknown",
                Hotel = b.Hotel?.HotelName ?? "Unknown Hotel",
                City = b.Hotel?.Governorate ?? "N/A",
                RoomType = string.Join(", ", b.BookingRooms.Select(br => br.RoomName ?? "Room")),
                CheckIn = b.CheckInDate?.ToString("dd MMM yyyy") ?? "N/A",
                CheckOut = b.CheckOutDate?.ToString("dd MMM yyyy") ?? "N/A",
                Price = $"${b.TotalPrice:N0}",
                Status = b.Status.ToString()
            }).ToList();

            return new AdminBookingPaginationResponse
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };
        }
    }
}

