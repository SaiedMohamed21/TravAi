using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Auth;
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
                    // context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} ON");
                    action();
                    context.SaveChanges();
                    // context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} OFF");
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
            Console.WriteLine("Cleanup: Starting...");
            Cleanup(context);
            Console.WriteLine("Seeding Users...");
            SeedUsers(context);
            // More seeding can be added here if needed
        }

        private static void Cleanup(ApplicationDbContext context)
        {
            context.Users.RemoveRange(context.Users);
            context.SaveChanges();
        }

        private static void SeedUsers(ApplicationDbContext context)
        {
            var users = new List<User>
            {
                new User { Id = 1, UserName = "admin1", Name = "Admin 1", Email = "admin1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.Admin, Status = UserStatus.Active, CreatedAt = SeedingTime },
                new User { Id = 7, UserName = "user1", Name = "User 1", Email = "user1@gmail.com", PasswordHash = CommonPasswordHash, Role = UserRole.User, Status = UserStatus.Active, WalletBalance = 5000, CreatedAt = SeedingTime }
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }

        private static string SimpleHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
