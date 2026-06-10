using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravAi.DTOs.Checkout
{
    public class CreateHotelTourCheckoutRequest
    {
        [Required]
        public long UserId { get; set; }

        public List<long> HotelBookingIds { get; set; } = new List<long>();

        public List<long> TourBookingIds { get; set; } = new List<long>();
    }
}
