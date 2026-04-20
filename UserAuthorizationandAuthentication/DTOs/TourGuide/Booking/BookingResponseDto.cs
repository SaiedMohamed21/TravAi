using UserAuthorizationandAuthentication.TourGuide.Models;
using System;
using System.Collections.Generic;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using BookingStatus = UserAuthorizationandAuthentication.TourGuide.Models.Enums.BookingStatus;
using PaymentStatus = UserAuthorizationandAuthentication.TourGuide.Models.Enums.PaymentStatus;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Booking
{
    public class BookingResponseDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public long TourId { get; set; }
        public string TourTitle { get; set; }
        public long TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public DateTime? BookingDate { get; set; }
        public DateTime? TourDate { get; set; }
        public TimeSpan? TourTime { get; set; }
        public int ParticipantsCount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; }
        public string? SpecialRequests { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ParticipantResponseDto> Participants { get; set; } = new List<ParticipantResponseDto>();
    }

    public class ParticipantResponseDto
    {
        public long Id { get; set; }
        public ParticipantType ParticipantType { get; set; }
        public AgeType AgeType { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? SpecialNeeds { get; set; }
        public string? DietaryRequirements { get; set; }
        public decimal Price { get; set; }

        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<EmergencyContactResponseDto> EmergencyContacts { get; set; } = new List<EmergencyContactResponseDto>();
    }

    public class EmergencyContactResponseDto
    {
        public long Id { get; set; }
        public string EmergencyName { get; set; }
        public string PhoneNumber { get; set; }
    }
}



