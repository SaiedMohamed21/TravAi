using System;
using System.Collections.Generic;

namespace TravAi.DTOs.AdminManagement
{
    public class UpdateUserStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateUserRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UserDetailResponseDto
    {
        public UserDetailProfileDto Profile { get; set; } = new();
        public UserDetailStatsDto Statistics { get; set; } = new();
        public List<UserDetailFlightBookingDto> FlightBookings { get; set; } = new();
        public List<UserDetailHotelBookingDto> HotelBookings { get; set; } = new();
        public List<UserDetailTourBookingDto> TourBookings { get; set; } = new();
        public List<UserDetailReviewDto> Reviews { get; set; } = new();
        public List<UserDetailComplaintDto> Complaints { get; set; } = new();
    }

    public class UserDetailProfileDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public List<string> Phones { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class UserDetailStatsDto
    {
        public int TotalBookings { get; set; }
        public decimal TotalSpending { get; set; }
        public int FlightBookingsCount { get; set; }
        public int HotelBookingsCount { get; set; }
        public int TourBookingsCount { get; set; }
    }

    public class UserDetailFlightBookingDto
    {
        public string BookingId { get; set; } = string.Empty;
        public string FlightInfo { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserDetailHotelBookingDto
    {
        public string BookingId { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserDetailTourBookingDto
    {
        public string BookingId { get; set; } = string.Empty;
        public string TourName { get; set; } = string.Empty;
        public string GuideName { get; set; } = string.Empty;
        public DateTime? TourDate { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UserDetailReviewDto
    {
        public string Type { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class UserDetailComplaintDto
    {
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
