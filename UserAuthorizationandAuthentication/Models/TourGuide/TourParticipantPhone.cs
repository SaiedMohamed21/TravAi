using UserAuthorizationandAuthentication.TourGuide.Models.Enums;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAuthorizationandAuthentication.TourGuide.Models
{
    public class TourParticipantPhone
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("Participant")]
        public long ParticipantId { get; set; }
        public TourBookingParticipant Participant { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
    }
}



