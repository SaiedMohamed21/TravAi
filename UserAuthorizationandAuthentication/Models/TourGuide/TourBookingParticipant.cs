using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAuthorizationandAuthentication.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.Models
{
    public class TourBookingParticipant
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("Booking")]
        public long BookingId { get; set; }
        public TourBooking Booking { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public ParticipantType ParticipantType { get; set; }
        
        [Column(TypeName = "nvarchar(20)")]
        public AgeType AgeType { get; set; }

        [Required]
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [Column(TypeName = "nvarchar(10)")]
        public Gender? Gender { get; set; }
        
        public string? Nationality { get; set; }
        public string? SpecialNeeds { get; set; }
        public string? DietaryRequirements { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public ICollection<TourParticipantPhone> Phones { get; set; } = new List<TourParticipantPhone>();
        public ICollection<TourParticipantEmergencyNumber> EmergencyNumbers { get; set; } = new List<TourParticipantEmergencyNumber>();
    }
}



