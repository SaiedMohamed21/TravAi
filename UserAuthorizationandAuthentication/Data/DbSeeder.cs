using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.Models.Hotels;
using UserAuthorizationandAuthentication.Models.Hotels.Bookings;
using UserAuthorizationandAuthentication.Airline.Models;
using UserAuthorizationandAuthentication.Airline.Models.Airlines;
using UserAuthorizationandAuthentication.TourGuide.Models;
using TourGuideBookingStatus = UserAuthorizationandAuthentication.TourGuide.Models.Enums.BookingStatus;
using HotelBookingStatus = UserAuthorizationandAuthentication.Models.Enums.BookingStatus;
using TourGuideStatus = UserAuthorizationandAuthentication.TourGuide.Models.Enums.TourGuideStatus;
using Language = UserAuthorizationandAuthentication.TourGuide.Models.Enums.Language;
using AirlineReview = UserAuthorizationandAuthentication.Airline.Models.Review;
using TourReview = UserAuthorizationandAuthentication.TourGuide.Models.Review;

namespace UserAuthorizationandAuthentication.Data
{
    public static class DbSeeder
    {
        private static readonly string CommonPasswordHash = SimpleHash("123456789");
        private static readonly DateTime SeedingTime = DateTime.UtcNow;

        private static void SaveWithIdentity(ApplicationDbContext context, string tableName, Action action)
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} ON");
                    action();
                    context.SaveChanges();
                    context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} OFF");
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            context.ChangeTracker.Clear();
        }

        public static void Seed(ApplicationDbContext context)
        {
            try
            {
                Console.WriteLine("Unified Seeder: Starting FULL RECONSTRUCTION...");
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                Cleanup(context);
                SeedUsers(context);
                SeedLookups(context);
                SeedHotels(context);
                SeedAirline(context);
                SeedTourGuide(context);
                SeedTransactions(context);

                Console.WriteLine("Unified Seeder: RECONSTRUCTION COMPLETED SUCCESSFULLY.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL SEEDER ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void Cleanup(ApplicationDbContext context)
        {
            context.ChangeTracker.Clear();
            context.TourReviews.RemoveRange(context.TourReviews);
            context.TourBookingPayments.RemoveRange(context.TourBookingPayments);
            context.TourBookingParticipants.RemoveRange(context.TourBookingParticipants);
            context.TourBookings.RemoveRange(context.TourBookings);
            context.TourImages.RemoveRange(context.TourImages);
            context.Tours.RemoveRange(context.Tours);
            context.TourGuidePhones.RemoveRange(context.TourGuidePhones);
            context.TourGuideLanguages.RemoveRange(context.TourGuideLanguages);
            context.TourGuideEmails.RemoveRange(context.TourGuideEmails);
            context.TourGuideCities.RemoveRange(context.TourGuideCities);
            context.UrgentRequests.RemoveRange(context.UrgentRequests);
            context.WithdrawRequests.RemoveRange(context.WithdrawRequests);
            context.TourGuides.RemoveRange(context.TourGuides);

            context.AirlineReviews.RemoveRange(context.AirlineReviews);
            context.WalletTransactions.RemoveRange(context.WalletTransactions);
            context.ChatMessages.RemoveRange(context.ChatMessages);
            context.PassengerPhones.RemoveRange(context.PassengerPhones);
            context.PassengerEmergencyContacts.RemoveRange(context.PassengerEmergencyContacts);
            context.Passengers.RemoveRange(context.Passengers);
            context.Bookings.RemoveRange(context.Bookings);
            context.Flights.RemoveRange(context.Flights);
            context.Airlines.RemoveRange(context.Airlines);
            context.Airports.RemoveRange(context.Airports);

            context.HotelReviews.RemoveRange(context.HotelReviews);
            context.HotelBookingRooms.RemoveRange(context.HotelBookingRooms);
            context.HotelBookings.RemoveRange(context.HotelBookings);
            context.HotelRooms.RemoveRange(context.HotelRooms);
            context.HotelAmenities.RemoveRange(context.HotelAmenities);
            context.HotelDocuments.RemoveRange(context.HotelDocuments);
            context.HotelImages.RemoveRange(context.HotelImages);
            context.HotelPolicies.RemoveRange(context.HotelPolicies);
            context.HotelContacts.RemoveRange(context.HotelContacts);
            context.HotelFieldValues.RemoveRange(context.HotelFieldValues);
            context.Hotels.RemoveRange(context.Hotels);

            context.DocumentTypes.RemoveRange(context.DocumentTypes);
            context.HotelFieldDefinitions.RemoveRange(context.HotelFieldDefinitions);
            context.Amenities.RemoveRange(context.Amenities);

            context.UserCompanions.RemoveRange(context.UserCompanions);
            context.UserPhones.RemoveRange(context.UserPhones);
            context.RefreshTokens.RemoveRange(context.RefreshTokens);
            context.Users.RemoveRange(context.Users);

            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Cleanup: Database Wiped.");
        }

        private static void SeedUsers(ApplicationDbContext context)
        {
            var users = new List<User>
            {
                new User { Id = 1, UserName = "admin1", Name = "Admin 1", Email = "admin1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Admin, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 2, UserName = "admin2", Name = "Admin 2", Email = "admin2@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Admin, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 3, UserName = "admin3", Name = "Admin 3", Email = "admin3@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Admin, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 4, UserName = "hotel1", Name = "Hotel Partner 1", Email = "hotel1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Hotel, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 5, UserName = "hotel2", Name = "Hotel Partner 2", Email = "hotel2@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Hotel, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 6, UserName = "hotel3", Name = "Hotel Partner 3", Email = "hotel3@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Hotel, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 7, UserName = "user1", Name = "User 1", Email = "user1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.User, Status = UserStatus.Active, WalletBalance = 5000, CreatedAt = SeedingTime },
                new User { Id = 8, UserName = "user2", Name = "User 2", Email = "user2@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.User, Status = UserStatus.Active, WalletBalance = 5000, CreatedAt = SeedingTime },
                new User { Id = 9, UserName = "user3", Name = "User 3", Email = "user3@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.User, Status = UserStatus.Active, WalletBalance = 5000, CreatedAt = SeedingTime },
                new User { Id = 10, UserName = "airline1", Name = "Airline Partner 1", Email = "airline1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Airline, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 11, UserName = "tourguide1", Name = "TourGuide Partner 1", Email = "tourguide1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Tourguide, Status = UserStatus.Active, CreatedAt = SeedingTime }
            };

            SaveWithIdentity(context, "Users", () => context.Users.AddRange(users));

            context.UserCompanions.Add(new UserCompanion { UserId = 7, FirstName = "Jane", LastName = "Doe", AgeType = "Adult", PassportNumber = "P1234567" });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Step 1: Users Seeded.");
        }

        private static void SeedLookups(ApplicationDbContext context)
        {
            var docTypes = new List<DocumentTypeDefinition> {
                new DocumentTypeDefinition { Id = 1, Name = "License Number", KeyName = "LicenseNumber", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 2, Name = "Tax ID", KeyName = "TaxId", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 3, Name = "Registration Document", KeyName = "RegistrationDoc", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 4, Name = "Bank Name", KeyName = "BankName", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 5, Name = "Bank Account Number", KeyName = "BankAccountNumber", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 6, Name = "Bank IBAN", KeyName = "BankIban", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 7, Name = "Application ID", KeyName = "ApplicationId", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 8, Name = "Bank Account Details", KeyName = "BankAccountDetailsUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 9, Name = "Civil Protection Certificate", KeyName = "CivilProtectionCertificateUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 10, Name = "Commercial Registry", KeyName = "CommercialRegistryUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 11, Name = "Contact Fax", KeyName = "ContactFax", IsRequired = false, IsActive = true },
                new DocumentTypeDefinition { Id = 12, Name = "Health Certificate", KeyName = "HealthCertificateUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 13, Name = "Hotel Classification Certificate", KeyName = "HotelClassificationCertificateUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 14, Name = "Operating License Document", KeyName = "OperatingLicenseUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 15, Name = "Ownership/Lease Proof", KeyName = "OwnershipLeaseProofUrl", IsRequired = true, IsActive = true },
                new DocumentTypeDefinition { Id = 16, Name = "Tax Card", KeyName = "TaxCardUrl", IsRequired = true, IsActive = true }
            };

            var fieldDefs = new List<HotelFieldDefinition> {
                new HotelFieldDefinition { Id = 1, DisplayName = "Check-in Time", KeyName = "CheckInTime", FieldType = HotelFieldType.Text, IsRequired = true },
                new HotelFieldDefinition { Id = 2, DisplayName = "Check-out Time", KeyName = "CheckOutTime", FieldType = HotelFieldType.Text, IsRequired = true },
                new HotelFieldDefinition { Id = 3, DisplayName = "Reception 24/7", KeyName = "Reception24h", FieldType = HotelFieldType.Text, IsRequired = true }
            };

            var amenities = new List<Amenity> {
                // High Frequency / Popular
                new Amenity { Id = 1, Name = "Free WiFi", Category = "Internet", IsHighlighted = true },
                new Amenity { Id = 2, Name = "Swimming pool", Category = "Wellness & Pools", IsHighlighted = true },
                new Amenity { Id = 3, Name = "Airport shuttle", Category = "General", IsHighlighted = true },
                new Amenity { Id = 4, Name = "Family rooms", Category = "General", IsHighlighted = true },
                new Amenity { Id = 5, Name = "Fitness centre", Category = "Wellness & Pools", IsHighlighted = true },
                new Amenity { Id = 6, Name = "Non-smoking rooms", Category = "General", IsHighlighted = true },
                new Amenity { Id = 7, Name = "Room service", Category = "General", IsHighlighted = true },
                new Amenity { Id = 8, Name = "Restaurant", Category = "Food & Drink", IsHighlighted = true },
                new Amenity { Id = 9, Name = "Bar", Category = "Food & Drink", IsHighlighted = true },
                new Amenity { Id = 10, Name = "Very good breakfast", Category = "Food & Drink", IsHighlighted = true },

                // Internet & Parking
                new Amenity { Id = 11, Name = "WiFi available in all areas", Category = "Internet", IsHighlighted = false },
                new Amenity { Id = 12, Name = "Free private parking on site", Category = "Parking", IsHighlighted = false },
                new Amenity { Id = 13, Name = "Valet parking", Category = "Parking", IsHighlighted = false },
                new Amenity { Id = 14, Name = "Electric vehicle charging station", Category = "Parking", IsHighlighted = false },

                // Food & Drink
                new Amenity { Id = 15, Name = "Coffee house on site", Category = "Food & Drink", IsHighlighted = false },
                new Amenity { Id = 16, Name = "Wine/champagne", Category = "Food & Drink", IsHighlighted = false },
                new Amenity { Id = 17, Name = "Kid meals", Category = "Food & Drink", IsHighlighted = false },
                new Amenity { Id = 18, Name = "Special diet menus (on request)", Category = "Food & Drink", IsHighlighted = false },

                // Wellness & Pools
                new Amenity { Id = 19, Name = "Spa and wellness centre", Category = "Wellness & Pools", IsHighlighted = false },
                new Amenity { Id = 20, Name = "Sauna", Category = "Wellness & Pools", IsHighlighted = false },
                new Amenity { Id = 21, Name = "Massage", Category = "Wellness & Pools", IsHighlighted = false },
                new Amenity { Id = 22, Name = "Hot tub/Jacuzzi", Category = "Wellness & Pools", IsHighlighted = false },
                new Amenity { Id = 23, Name = "Hammam", Category = "Wellness & Pools", IsHighlighted = false },

                // Reception Services
                new Amenity { Id = 24, Name = "24-hour front desk", Category = "Reception Services", IsHighlighted = false },
                new Amenity { Id = 25, Name = "Concierge service", Category = "Reception Services", IsHighlighted = false },
                new Amenity { Id = 26, Name = "Luggage storage", Category = "Reception Services", IsHighlighted = false },
                new Amenity { Id = 27, Name = "Currency exchange", Category = "Reception Services", IsHighlighted = false },
                new Amenity { Id = 28, Name = "ATM/cash machine on site", Category = "Reception Services", IsHighlighted = false },

                // Cleaning Services
                new Amenity { Id = 29, Name = "Daily housekeeping", Category = "Cleaning Services", IsHighlighted = false },
                new Amenity { Id = 30, Name = "Laundry", Category = "Cleaning Services", IsHighlighted = false },
                new Amenity { Id = 31, Name = "Dry cleaning", Category = "Cleaning Services", IsHighlighted = false },
                new Amenity { Id = 32, Name = "Ironing service", Category = "Cleaning Services", IsHighlighted = false },

                // General
                new Amenity { Id = 33, Name = "Air conditioning", Category = "General", IsHighlighted = true },
                new Amenity { Id = 34, Name = "Lift", Category = "General", IsHighlighted = false },
                new Amenity { Id = 35, Name = "Soundproof rooms", Category = "General", IsHighlighted = false },
                new Amenity { Id = 36, Name = "Facilities for disabled guests", Category = "General", IsHighlighted = false },
                new Amenity { Id = 37, Name = "Heating", Category = "General", IsHighlighted = false },

                // Safety & Security
                new Amenity { Id = 38, Name = "24-hour security", Category = "Safety & Security", IsHighlighted = false },
                new Amenity { Id = 39, Name = "Security alarm", Category = "Safety & Security", IsHighlighted = false },
                new Amenity { Id = 40, Name = "Smoke alarms", Category = "Safety & Security", IsHighlighted = false },
                new Amenity { Id = 41, Name = "CCTV in common areas", Category = "Safety & Security", IsHighlighted = false },
                new Amenity { Id = 42, Name = "Fire extinguishers", Category = "Safety & Security", IsHighlighted = false }
            };

            var airports = new List<Airport> {
                new Airport { Code = "CAI", Name = "Cairo International Airport", City = "Cairo", Country = "Egypt" },
                new Airport { Code = "LHR", Name = "London Heathrow", City = "London", Country = "UK" },
                new Airport { Code = "DXB", Name = "Dubai International", City = "Dubai", Country = "UAE" }
            };

            SaveWithIdentity(context, "hotel_DocumentTypes", () => context.DocumentTypes.AddRange(docTypes));
            SaveWithIdentity(context, "hotel_HotelFieldDefinitions", () => context.HotelFieldDefinitions.AddRange(fieldDefs));
            SaveWithIdentity(context, "hotel_Amenities", () => context.Amenities.AddRange(amenities));

            context.Airports.AddRange(airports);
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Step 2: Lookups Seeded.");
        }

        private static void SeedHotels(ApplicationDbContext context)
        {
            var hotels = new List<Hotel> {
                new Hotel { Id = 1, UserId = 4, HotelName = "Grand Nile Tower", Country = "Egypt", Governorate = "Cairo", CityArea = "Downtown", StarRating = 5, PriceUsd = 150, Verified = true, VerificationStatus = VerificationStatus.Verified, Active = true, CreatedAt = SeedingTime, NumReviews = 1, AvgReviewScore = 5.0m },
                new Hotel { Id = 2, UserId = 5, HotelName = "Mediterranean View", Country = "Egypt", Governorate = "Alexandria", CityArea = "Corniche", StarRating = 5, PriceUsd = 200, Verified = true, VerificationStatus = VerificationStatus.Verified, Active = true, CreatedAt = SeedingTime, NumReviews = 0, AvgReviewScore = 0m },
                new Hotel { Id = 3, UserId = 6, HotelName = "Coral Reef Resort", Country = "Egypt", Governorate = "Sharm El-Sheikh", CityArea = "Naama Bay", StarRating = 5, PriceUsd = 250, Verified = true, VerificationStatus = VerificationStatus.Verified, Active = true, CreatedAt = SeedingTime, NumReviews = 0, AvgReviewScore = 0m }
            };

            SaveWithIdentity(context, "hotel_Hotels", () => context.Hotels.AddRange(hotels));

            foreach (var h in hotels)
            {
                context.HotelContacts.Add(new HotelContact { HotelId = h.Id, ContactType = HotelContactType.Email, ContactValue = "info@hotel.com" });
                context.HotelImages.Add(new HotelImage { HotelId = h.Id, ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945", IsPrimary = true, SortOrder = 1 });
                context.HotelPolicies.Add(new HotelPolicy { HotelId = h.Id, ServiceChargePct = 12, IncludeVat = true, CancellationStrategy = CancellationStrategy.WindowBased });
                context.HotelRooms.Add(new HotelRoom { HotelId = h.Id, Name = HotelRoomName.StandardRoom, ROPrice = h.PriceUsd, BBPrice = h.PriceUsd + 20, Quantity = 10, Occupancy = 2, BedType = BedType.Double, State = RoomState.Active });
                
                for(int d=1; d<=16; d++) context.HotelDocuments.Add(new HotelDocument { HotelId = h.Id, DocumentTypeId = d, FileUrl = "/samples/doc.pdf", UploadedAt = SeedingTime, Notes = "Seeded" });
                for(int f=1; f<=3; f++) context.HotelFieldValues.Add(new HotelFieldValue { HotelId = h.Id, FieldDefinitionId = f, Value = "Standard" });
                
                // Add popular amenities to each hotel
                for(int a=1; a<=10; a++) context.HotelAmenities.Add(new HotelAmenity { HotelId = h.Id, AmenityId = a });
                // Add some random amenities
                context.HotelAmenities.Add(new HotelAmenity { HotelId = h.Id, AmenityId = 33 }); // AC
                context.HotelAmenities.Add(new HotelAmenity { HotelId = h.Id, AmenityId = 12 }); // Parking
            }
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Step 3: Hotels Module Seeded.");
        }

        private static void SeedAirline(ApplicationDbContext context)
        {
            var airline = new UserAuthorizationandAuthentication.Airline.Models.Airlines.Airline { Id = 1, UserId = 10, Name = "EgyptAir", Country = "Egypt", Status = "Active", IsApproved = true };
            SaveWithIdentity(context, "airline_Airlines", () => context.Airlines.Add(airline));

            var flight = new Flight { Id = 1, AirlineId = 1, FlightNumber = "MS985", DepartureAirportCode = "CAI", ArrivalAirportCode = "LHR", Price = 500, AvailableSeats = 150, DepartureTime = SeedingTime.AddDays(2), ArrivalTime = SeedingTime.AddDays(2).AddHours(5), Status = "Active" };
            SaveWithIdentity(context, "airline_Flights", () => context.Flights.Add(flight));

            var booking = new UserAuthorizationandAuthentication.Airline.Models.Booking { Id = 1, UserId = 7, FlightId = 1, TotalPrice = 500, Status = "Confirmed", BookingDate = SeedingTime };
            SaveWithIdentity(context, "airline_Bookings", () => context.Bookings.Add(booking));

            context.Passengers.Add(new Passenger { Id = 1, BookingId = 1, FirstName = "User", LastName = "1", PassportNumber = "P777", Nationality = "Egyptian" });
            SaveWithIdentity(context, "airline_Passengers", () => context.SaveChanges());

            context.WalletTransactions.Add(new WalletTransaction { UserId = 7, Amount = -500, Type = "Withdrawal", Description = "Flight MS985", CreatedAt = SeedingTime });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Step 4: Airline Module Seeded.");
        }

        private static void SeedTourGuide(ApplicationDbContext context)
        {
            var guide = new UserAuthorizationandAuthentication.TourGuide.Models.TourGuide { Id = 1, UserId = 11, Name = "Ahmed Guide", Status = TourGuideStatus.Active };
            SaveWithIdentity(context, "tourguide_TourGuides", () => context.TourGuides.Add(guide));

            context.TourGuidePhones.Add(new TourGuidePhone { TourGuideId = 1, PhoneNumber = "+2010000000" });
            context.TourGuideLanguages.Add(new TourGuideLanguage { TourGuideId = 1, Language = Language.English });

            var tour = new Tour { Id = 1, TourGuideId = 1, TourTitle = "Giza Pyramids Day Tour", BasePriceUsd = 80, DurationHours = 6, Active = true, City = "Giza" };
            SaveWithIdentity(context, "tourguide_Tours", () => context.Tours.Add(tour));

            context.TourImages.Add(new TourImage { TourId = 1, ImageUrl = "https://images.unsplash.com/photo-1549488344-cbb6c34cf08b", IsPrimary = true });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Console.WriteLine("Step 5: TourGuide Module Seeded.");
        }

        private static void SeedTransactions(ApplicationDbContext context)
        {
            var hBooking = new HotelBooking { Id = 1, UserId = 7, HotelId = 1, TotalPrice = 360, Status = HotelBookingStatus.Confirmed, CreatedAt = SeedingTime, CheckInDate = SeedingTime.AddDays(-5), CheckOutDate = SeedingTime.AddDays(-2) };
            SaveWithIdentity(context, "hotel_HotelBookings", () => context.HotelBookings.Add(hBooking));
            
            var tBooking = new TourBooking { Id = 1, UserId = 7, TourId = 1, TourGuideId = 1, TotalPrice = 80, Status = TourGuideBookingStatus.Confirmed, CreatedAt = SeedingTime };
            SaveWithIdentity(context, "tourguide_TourBookings", () => context.TourBookings.Add(tBooking));

            context.HotelReviews.Add(new HotelReview { Id = 1, HotelId = 1, UserId = 7, Rating = 5, Comment = "Amazing stay!", CreatedAt = SeedingTime });
            SaveWithIdentity(context, "hotel_HotelReviews", () => context.SaveChanges());

            context.AirlineReviews.Add(new AirlineReview { Id = 1, FlightId = 1, UserId = 7, Rating = 5, Comment = "Great Flight!", ReviewDate = SeedingTime });
            SaveWithIdentity(context, "airline_Reviews", () => context.SaveChanges());

            context.TourReviews.Add(new TourReview { Id = 1, TourId = 1, UserId = 7, Rating = 5, Comment = "Brilliant Guide!", CreatedAt = SeedingTime });
            SaveWithIdentity(context, "tourguide_Reviews", () => context.SaveChanges());

            context.ChangeTracker.Clear();
            Console.WriteLine("Step 6: Transactions Seeded.");
        }

        private static string SimpleHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
