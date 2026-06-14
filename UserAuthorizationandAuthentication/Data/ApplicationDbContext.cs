using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TravAi.Models;
using TravAi.Models.Auth;
using TravAi.Models.Enums;
using TravAi.Models.Hotels;
using TravAi.Models.Hotels.Bookings;
using TravAi.Airline.Models;
using TravAi.Airline.Models.Airlines;
using TravAi.TourGuide.Models;
using TravAi.Models.Common;
using TravAi.Models.AI;
using TravAi.Models.Admin;
using TravAi.Airline.Models.Airlines;

namespace TravAi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserPhone> UserPhones { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<HotelRoom> HotelRooms { get; set; }
        public DbSet<HotelImage> HotelImages { get; set; }
        public DbSet<HotelReview> HotelReviews { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<HotelAmenity> HotelAmenities { get; set; }
        public DbSet<HotelContact> HotelContacts { get; set; }
        public DbSet<DocumentTypeDefinition> DocumentTypes { get; set; }
        public DbSet<HotelDocument> HotelDocuments { get; set; }
        public DbSet<HotelFieldDefinition> HotelFieldDefinitions { get; set; }
        public DbSet<HotelFieldValue> HotelFieldValues { get; set; }

        public DbSet<HotelBooking> HotelBookings { get; set; }
        public DbSet<HotelBookingRoom> HotelBookingRooms { get; set; }

        public DbSet<HotelPolicy> HotelPolicies { get; set; }
        public DbSet<HotelCancellationRule> HotelCancellationRules { get; set; }
        
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }
        public DbSet<ComplaintReply> ComplaintReplies { get; set; }
        public DbSet<CommissionSetting> CommissionSettings { get; set; }
        public DbSet<HotelPayment> HotelPayments { get; set; }
        public DbSet<HotelAdminInboxMessage> HotelAdminInboxMessages { get; set; }
        public DbSet<HotelAdminInboxReply> HotelAdminInboxReplies { get; set; }
        public DbSet<HotelToAdminMessage> HotelToAdminMessages { get; set; }
        public DbSet<HotelPendingProfile> HotelPendingProfiles { get; set; }
        public DbSet<HotelPendingPolicy> HotelPendingPolicies { get; set; }
        public DbSet<HotelPendingCancellationRule> HotelPendingCancellationRules { get; set; }
        public DbSet<HotelPendingLegalDocument> HotelPendingLegalDocuments { get; set; }

        // Admin DbSets
        public DbSet<ProviderFine> ProviderFines { get; set; }
        public DbSet<PayoutBatch> PayoutBatches { get; set; }
        public DbSet<PayoutItem> PayoutItems { get; set; }
        public DbSet<PayoutFineDeduction> PayoutFineDeductions { get; set; }
        public DbSet<ProviderStripePayoutAccount> ProviderStripePayoutAccounts { get; set; }
        public DbSet<PayoutStripePayment> PayoutStripePayments { get; set; }

        // Airline DbSets
        public DbSet<Airport> Airports { get; set; }
        public DbSet<TravAi.Airline.Models.Booking> Bookings { get; set; }

        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<PassengerEmergencyContact> PassengerEmergencyContacts { get; set; }
        public DbSet<PassengerPhone> PassengerPhones { get; set; }
        public DbSet<TravAi.Airline.Models.Review> AirlineReviews { get; set; }
        public DbSet<UserCompanion> UserCompanions { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<TravAi.Airline.Models.Airlines.Airline> Airlines { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<FlightSegment> FlightSegments { get; set; }
        public DbSet<FlightLayover> FlightLayovers { get; set; }

        // TourGuide DbSets
        public DbSet<TravAi.TourGuide.Models.Review> TourReviews { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourBooking> TourBookings { get; set; }
        public DbSet<TourBookingParticipant> TourBookingParticipants { get; set; }
        public DbSet<TourBookingPayment> TourBookingPayments { get; set; }
        public DbSet<TourBookingResolution> TourBookingResolutions { get; set; }
        public DbSet<UserTourCompensationCoupon> UserTourCompensationCoupons { get; set; }
        public DbSet<TravAi.TourGuide.Models.TourGuide> TourGuides { get; set; }
        public DbSet<TourGuideCity> TourGuideCities { get; set; }
        public DbSet<TourGuideEmail> TourGuideEmails { get; set; }
        public DbSet<TourGuideLanguage> TourGuideLanguages { get; set; }
        public DbSet<TourGuidePhone> TourGuidePhones { get; set; }
        public DbSet<TourImage> TourImages { get; set; }
        public DbSet<TourParticipantEmergencyNumber> TourParticipantEmergencyNumbers { get; set; }
        public DbSet<TourParticipantPhone> TourParticipantPhones { get; set; }
        public DbSet<UrgentRequest> UrgentRequests { get; set; }
        public DbSet<WithdrawRequest> WithdrawRequests { get; set; }

        public DbSet<CheckoutSession> CheckoutSessions { get; set; }
        public DbSet<CheckoutSessionItem> CheckoutSessionItems { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentTransactionItem> PaymentTransactionItems { get; set; }
        public DbSet<StripeWebhookEvent> StripeWebhookEvents { get; set; }

        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PlatformCommission> PlatformCommissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Shared Auth Tables (Global)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
            modelBuilder.Entity<UserPhone>().ToTable("UserPhones");
            // Hotel Prefixes
            modelBuilder.Entity<Hotel>().ToTable("hotel_Hotels");
            modelBuilder.Entity<HotelRoom>().ToTable("hotel_HotelRooms");
            modelBuilder.Entity<HotelImage>().ToTable("hotel_HotelImages");
            modelBuilder.Entity<HotelReview>().ToTable("hotel_HotelReviews");
            modelBuilder.Entity<Amenity>().ToTable("hotel_Amenities");
            modelBuilder.Entity<HotelAmenity>().ToTable("hotel_HotelAmenities");
            modelBuilder.Entity<HotelContact>().ToTable("hotel_HotelContacts");
            modelBuilder.Entity<DocumentTypeDefinition>().ToTable("hotel_DocumentTypes");
            modelBuilder.Entity<HotelDocument>().ToTable("hotel_HotelDocuments");
            modelBuilder.Entity<HotelFieldDefinition>().ToTable("hotel_HotelFieldDefinitions");
            modelBuilder.Entity<HotelFieldValue>().ToTable("hotel_HotelFieldValues");
            modelBuilder.Entity<HotelBooking>().ToTable("hotel_HotelBookings");
            modelBuilder.Entity<HotelBookingRoom>().ToTable("hotel_HotelBookingRooms");
            modelBuilder.Entity<HotelPolicy>().ToTable("hotel_HotelPolicies");
            modelBuilder.Entity<HotelCancellationRule>().ToTable("hotel_HotelCancellationRules");

            // Airline Prefixes
            modelBuilder.Entity<Airport>().ToTable("airline_Airports");
            modelBuilder.Entity<TravAi.Airline.Models.Booking>().ToTable("airline_Bookings");

            modelBuilder.Entity<Passenger>().ToTable("airline_Passengers");
            modelBuilder.Entity<PassengerEmergencyContact>().ToTable("airline_PassengerEmergencyContacts");
            modelBuilder.Entity<PassengerPhone>().ToTable("airline_PassengerPhones");
            modelBuilder.Entity<TravAi.Airline.Models.Review>().ToTable("airline_Reviews");
            modelBuilder.Entity<UserCompanion>().ToTable("airline_UserCompanions");
            modelBuilder.Entity<WalletTransaction>().ToTable("airline_WalletTransactions");
            modelBuilder.Entity<TravAi.Airline.Models.Airlines.Airline>().ToTable("airline_Airlines");
            modelBuilder.Entity<Flight>().ToTable("airline_Flights");
            modelBuilder.Entity<FlightSegment>().ToTable("airline_FlightSegments");
            modelBuilder.Entity<FlightLayover>().ToTable("airline_FlightLayovers");

            // Admin Prefixes
            modelBuilder.Entity<ProviderFine>().ToTable("admin_ProviderFines");

            // TourGuide Prefixes
            modelBuilder.Entity<TravAi.TourGuide.Models.Review>().ToTable("tourguide_Reviews");
            modelBuilder.Entity<Tour>().ToTable("tourguide_Tours");
            modelBuilder.Entity<TourBooking>().ToTable("tourguide_TourBookings");
            modelBuilder.Entity<TourBookingParticipant>().ToTable("tourguide_TourBookingParticipants");
            modelBuilder.Entity<TourBookingPayment>().ToTable("tourguide_TourBookingPayments");
            modelBuilder.Entity<TourBookingResolution>().ToTable("tourguide_TourBookingResolutions");
            modelBuilder.Entity<UserTourCompensationCoupon>().ToTable("tourguide_UserTourCompensationCoupons");
            modelBuilder.Entity<TravAi.TourGuide.Models.TourGuide>().ToTable("tourguide_TourGuides");
            modelBuilder.Entity<TourGuideCity>().ToTable("tourguide_TourGuideCities");
            modelBuilder.Entity<TourGuideEmail>().ToTable("tourguide_TourGuideEmails");
            modelBuilder.Entity<TourGuideLanguage>().ToTable("tourguide_TourGuideLanguages");
            modelBuilder.Entity<TourGuidePhone>().ToTable("tourguide_TourGuidePhones");
            modelBuilder.Entity<TourImage>().ToTable("tourguide_TourImages");
            modelBuilder.Entity<TourParticipantEmergencyNumber>().ToTable("tourguide_TourParticipantEmergencyNumbers");
            modelBuilder.Entity<TourParticipantPhone>().ToTable("tourguide_TourParticipantPhones");
            modelBuilder.Entity<UrgentRequest>().ToTable("tourguide_UrgentRequests");
            modelBuilder.Entity<WithdrawRequest>().ToTable("tourguide_WithdrawRequests");
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
                
            modelBuilder.Entity<User>()
                .Property(u => u.Gender)
                .HasConversion<string>();
                
            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<string>();
            modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Hotel configuration
            modelBuilder.Entity<Hotel>()
                .Property(h => h.AvgReviewScore)
                .HasPrecision(4, 2);
            modelBuilder.Entity<Hotel>()
                .Property(h => h.PriceUsd)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Hotel>()
                .Property(h => h.PropertyType)
                .HasConversion<string>();
            modelBuilder.Entity<Hotel>()
                .Property(h => h.AccommodationType)
                .HasConversion<string>();
            modelBuilder.Entity<Hotel>()
                .Property(h => h.VerificationStatus)
                .HasConversion<string>();


            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.ROPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.BBPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.HBPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.FBPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.AIPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.State)
                .HasConversion<string>();
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.Name)
                .HasConversion<string>();
            modelBuilder.Entity<HotelRoom>()
                .Property(r => r.BedType)
                .HasConversion<string>();

            modelBuilder.Entity<HotelBooking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelBookingRoom>()
                .Property(br => br.PricePerNight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelBookingRoom>()
                .Property(br => br.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelPolicy>()
                .Property(p => p.ServiceChargePct)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelCancellationRule>()
                .Property(r => r.PenaltyPct)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelPolicy>()
                .Property(p => p.CancellationStrategy)
                .HasConversion<string>();

            modelBuilder.Entity<HotelContact>()
                .Property(c => c.ContactType)
                .HasConversion<string>();

            modelBuilder.Entity<HotelFieldDefinition>()
                .Property(f => f.FieldType)
                .HasConversion<string>();

            modelBuilder.Entity<DocumentTypeDefinition>()
                .HasIndex(d => d.KeyName)
                .IsUnique();
            modelBuilder.Entity<HotelFieldDefinition>()
                .HasIndex(f => f.KeyName)
                .IsUnique();

        // Hotel - User relationship
        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Airline - User relationship
        modelBuilder.Entity<TravAi.Airline.Models.Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TravAi.Airline.Models.Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TourGuide - User relationship
        modelBuilder.Entity<TourBooking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TravAi.TourGuide.Models.Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Hotel>()

                .HasMany(h => h.Contacts)
                .WithOne(c => c.Hotel)
                .HasForeignKey(c => c.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Documents)
                .WithOne(d => d.Hotel)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.FieldValues)
                .WithOne(v => v.Hotel)
                .HasForeignKey(v => v.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelDocument>()
                .HasOne(d => d.DocumentType)
                .WithMany(t => t.HotelDocuments)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelFieldValue>()
                .HasOne(v => v.FieldDefinition)
                .WithMany(f => f.Values)
                .HasForeignKey(v => v.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Amenities many-to-many via HotelAmenities
            modelBuilder.Entity<HotelAmenity>()
                .HasOne(ha => ha.Hotel)
                .WithMany(h => h.HotelAmenities)
                .HasForeignKey(ha => ha.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<HotelAmenity>()
                .HasOne(ha => ha.Amenity)
                .WithMany(a => a.HotelAmenities)
                .HasForeignKey(ha => ha.AmenityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure cascade delete for bookings to prevent cycles
            modelBuilder.Entity<HotelBooking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // A booking must be attached to a hotel
            modelBuilder.Entity<HotelBooking>()
                .HasOne(b => b.Hotel)
                .WithMany(h => h.Bookings)
                .HasForeignKey(b => b.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelBookingRoom>()
                .HasOne(br => br.Booking)
                .WithMany(b => b.BookingRooms)
                .HasForeignKey(br => br.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<HotelBookingRoom>()
                .HasOne(br => br.Room)
                .WithMany()
                .HasForeignKey(br => br.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascade deletes for Hotel children
            modelBuilder.Entity<HotelRoom>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelImage>()
                .HasOne(i => i.Hotel)
                .WithMany(h => h.Images)
                .HasForeignKey(i => i.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelReview>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hotel Policy (One-to-One)
            modelBuilder.Entity<HotelPolicy>()
                .HasOne(p => p.Hotel)
                .WithOne(h => h.Policy)
                .HasForeignKey<HotelPolicy>(p => p.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<HotelCancellationRule>()
                .HasOne(r => r.HotelPolicy)
                .WithMany(p => p.CancellationRules)
                .HasForeignKey(r => r.HotelPolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Complaints Configuration
            modelBuilder.Entity<Complaint>()
                .Property(c => c.ComplaintType)
                .HasConversion<string>();
            modelBuilder.Entity<Complaint>()
                .Property(c => c.Status)
                .HasConversion<string>();
            modelBuilder.Entity<Complaint>()
                .Property(c => c.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.TourBooking)
                .WithMany()
                .HasForeignKey(c => c.TourBookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.AirlineBooking)
                .WithMany()
                .HasForeignKey(c => c.AirlineBookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ComplaintAttachment>()
                .HasOne(a => a.Complaint)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComplaintReply>()
                .HasOne(r => r.Complaint)
                .WithMany(c => c.Replies)
                .HasForeignKey(r => r.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            // Hotel Payment Configurations
            modelBuilder.Entity<HotelPayment>().ToTable("hotel_payment");

            modelBuilder.Entity<HotelPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HotelPayment>()
                .Property(p => p.PaymentMethod)
                .HasConversion<string>();

            modelBuilder.Entity<HotelPayment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<HotelPayment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();

            modelBuilder.Entity<HotelPayment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelPayment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelPayment>()
                .HasOne(p => p.Hotel)
                .WithMany()
                .HasForeignKey(p => p.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hotel Admin Inbox and To Admin Messages
            modelBuilder.Entity<HotelAdminInboxMessage>()
                .Property(m => m.Amount)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<HotelAdminInboxMessage>()
                .HasOne(m => m.Hotel)
                .WithMany()
                .HasForeignKey(m => m.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelAdminInboxReply>()
                .HasOne(r => r.InboxMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(r => r.InboxMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelAdminInboxReply>()
                .HasOne(r => r.Hotel)
                .WithMany()
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelAdminInboxReply>()
                .HasOne(r => r.FromUser)
                .WithMany()
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelToAdminMessage>()
                .ToTable("hotel_ToAdminMessages");
            
            modelBuilder.Entity<HotelToAdminMessage>()
                .Property(m => m.Category)
                .HasConversion<string>();

            modelBuilder.Entity<HotelToAdminMessage>()
                .HasOne(m => m.Hotel)
                .WithMany()
                .HasForeignKey(m => m.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelToAdminMessage>()
                .HasOne(m => m.FromUser)
                .WithMany()
                .HasForeignKey(m => m.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pending Configurations
            modelBuilder.Entity<HotelPendingProfile>()
                .HasOne(r => r.Hotel)
                .WithMany()
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelPendingPolicy>()
                .HasOne(r => r.Hotel)
                .WithMany()
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HotelPendingLegalDocument>()
                .HasOne(r => r.Hotel)
                .WithMany()
                .HasForeignKey(r => r.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Airline Flights
            modelBuilder.Entity<Flight>()
                .HasOne(f => f.DepartureAirport)
                .WithMany()
                .HasForeignKey(f => f.DepartureAirportCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.ArrivalAirport)
                .WithMany()
                .HasForeignKey(f => f.ArrivalAirportCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TravAi.Airline.Models.Booking>()
                .HasOne(b => b.Flight)
                .WithMany()
                .HasForeignKey(b => b.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            // TourGuide Tours
            modelBuilder.Entity<Tour>()
                .HasOne(t => t.TourGuide)
                .WithMany()
                .HasForeignKey(t => t.TourGuideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TourBooking>()
                .HasOne(b => b.Tour)
                .WithMany()
                .HasForeignKey(b => b.TourId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TourBooking>()
                .HasOne(b => b.TourGuide)
                .WithMany()
                .HasForeignKey(b => b.TourGuideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TravAi.TourGuide.Models.Review>()
                .HasOne(r => r.Tour)
                .WithMany()
                .HasForeignKey(r => r.TourId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TravAi.TourGuide.Models.Review>()
                .HasOne(r => r.TourGuide)
                .WithMany()
                .HasForeignKey(r => r.TourGuideId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tour Performance Indexes
            modelBuilder.Entity<Tour>().HasIndex(t => t.City);
            modelBuilder.Entity<Tour>().HasIndex(t => t.TourScore).IsDescending();
            modelBuilder.Entity<Tour>().HasIndex(t => t.BasePriceUsd);
            modelBuilder.Entity<Tour>().HasIndex(t => t.Rating);
            modelBuilder.Entity<Tour>().HasIndex(t => t.TourGuideId);
            modelBuilder.Entity<Tour>().HasIndex(t => t.TourType);
            modelBuilder.Entity<Tour>().HasIndex(t => t.AvailableDateTime);
            modelBuilder.Entity<Tour>().HasIndex(t => new { t.City, t.TourScore });

            modelBuilder.Entity<CommissionSetting>()
                .HasOne(c => c.CreatedByAdminUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionSetting>()
                .HasIndex(c => c.IsActive)
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            // Simple Stripe Checkout Configuration
            modelBuilder.Entity<CheckoutSession>().ToTable("CheckoutSessions");
            modelBuilder.Entity<CheckoutSessionItem>().ToTable("CheckoutSessionItems");
            modelBuilder.Entity<PaymentTransaction>().ToTable("PaymentTransactions");
            modelBuilder.Entity<PaymentTransactionItem>().ToTable("PaymentTransactionItems");
            modelBuilder.Entity<StripeWebhookEvent>().ToTable("StripeWebhookEvents");

            modelBuilder.Entity<CheckoutSession>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CheckoutSessionItem>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentTransaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentTransaction>()
                .Property(t => t.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentTransactionItem>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CheckoutSessionItem>()
                .HasOne(i => i.CheckoutSession)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.CheckoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.CheckoutSession)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.CheckoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentTransactionItem>()
                .HasOne(i => i.PaymentTransaction)
                .WithMany(t => t.Items)
                .HasForeignKey(i => i.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StripeWebhookEvent>()
                .HasOne(e => e.CheckoutSession)
                .WithMany()
                .HasForeignKey(e => e.CheckoutSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StripeWebhookEvent>()
                .HasOne(e => e.PaymentTransaction)
                .WithMany()
                .HasForeignKey(e => e.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // AI Chatbot
            modelBuilder.Entity<ChatSession>().ToTable("ai_ChatSessions");
            modelBuilder.Entity<ChatMessage>().ToTable("ai_ChatMessages");

            // Admin Platform Commissions
            modelBuilder.Entity<PlatformCommission>().ToTable("admin_PlatformCommissions");

            modelBuilder.Entity<ChatSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.ChatSession)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProviderFines Configuration
            modelBuilder.Entity<ProviderFine>()
                .Property(f => f.ProviderType)
                .HasConversion<string>();

            modelBuilder.Entity<ProviderFine>()
                .Property(f => f.SourceType)
                .HasConversion<string>();

            modelBuilder.Entity<ProviderFine>()
                .Property(f => f.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ProviderFine>()
                .HasIndex(f => new { f.ProviderType, f.ProviderId });

            modelBuilder.Entity<ProviderFine>()
                .HasIndex(f => f.ComplaintId);

            modelBuilder.Entity<ProviderFine>()
                .HasIndex(f => f.TourBookingId);

            modelBuilder.Entity<ProviderFine>()
                .HasIndex(f => f.Status);

            modelBuilder.Entity<ProviderFine>()
                .HasIndex(f => f.CreatedAt);

            modelBuilder.Entity<ProviderFine>()
                .HasOne(f => f.CreatedByAdminUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProviderFine>()
                .HasOne(f => f.CancelledByAdminUser)
                .WithMany()
                .HasForeignKey(f => f.CancelledByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProviderFine>()
                .HasOne(f => f.Complaint)
                .WithMany()
                .HasForeignKey(f => f.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payouts Configurations
            modelBuilder.Entity<PayoutBatch>()
                .HasIndex(p => new { p.ProviderType, p.ProviderId, p.WeekStartDate, p.WeekEndDate, p.Currency })
                .IsUnique();

            modelBuilder.Entity<PayoutBatch>()
                .HasOne(p => p.GeneratedByAdminUser)
                .WithMany()
                .HasForeignKey(p => p.GeneratedByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayoutBatch>()
                .HasOne(p => p.ConfirmedByAdminUser)
                .WithMany()
                .HasForeignKey(p => p.ConfirmedByAdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayoutItem>()
                .HasOne(i => i.PayoutBatch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.PayoutBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayoutFineDeduction>()
                .HasOne(d => d.PayoutBatch)
                .WithMany(b => b.Deductions)
                .HasForeignKey(d => d.PayoutBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PayoutFineDeduction>()
                .HasOne(d => d.ProviderFine)
                .WithMany()
                .HasForeignKey(d => d.ProviderFineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProviderStripePayoutAccount>()
                .HasIndex(a => new { a.ProviderType, a.ProviderId })
                .IsUnique();

            modelBuilder.Entity<PayoutStripePayment>()
                .HasOne(p => p.PayoutBatch)
                .WithMany()
                .HasForeignKey(p => p.PayoutBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayoutStripePayment>()
                .HasOne(p => p.ProviderStripePayoutAccount)
                .WithMany()
                .HasForeignKey(p => p.ProviderStripePayoutAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
