using System.ComponentModel.DataAnnotations;

namespace TravAi.DTOs.Checkout
{
    public class CreateAirlineCheckoutRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public long AirlineBookingId { get; set; }
    }
}
