using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravAi.Airline.DTOs.Passenger
{
    public class SaveBookingPassengersRequest
    {
        [Required]
        public List<PassengerDetailInput> Passengers { get; set; } = new();
    }

    public class PassengerDetailInput
    {
        public long? Id { get; set; } // If updating existing, otherwise null to create
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;
        
        [MaxLength(20)]
        public string PassengerType { get; set; } = "Adult";
        
        [MaxLength(20)]
        public string AgeType { get; set; } = "Adult";
        
        [Required]
        [MaxLength(50)]
        public string PassportNumber { get; set; } = null!;
        
        [MaxLength(50)]
        public string? Nationality { get; set; }

        public decimal Price { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public DateTime? PassportExpiryDate { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }
    }
}
