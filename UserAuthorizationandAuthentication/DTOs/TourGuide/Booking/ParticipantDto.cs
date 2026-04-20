using UserAuthorizationandAuthentication.TourGuide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.Booking
{
    public class ParticipantDto
    {
        [Required]
        public ParticipantType ParticipantType { get; set; }
        
        [Required]
        public AgeType AgeType { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        public string? MiddleName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? SpecialNeeds { get; set; }
        public string? DietaryRequirements { get; set; }
        
        [Required]
        public decimal Price { get; set; }

        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<EmergencyContactDto> EmergencyContacts { get; set; } = new List<EmergencyContactDto>();
    }

    public class EmergencyContactDto
    {
        [Required]
        public string EmergencyName { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
    }
}



