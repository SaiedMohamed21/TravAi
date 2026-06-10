using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravAi.DTOs.Checkout
{
    public class CreateUnifiedCheckoutRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public List<CheckoutItemRequest> Items { get; set; } = new List<CheckoutItemRequest>();
    }

    public class CheckoutItemRequest
    {
        [Required]
        public string ItemType { get; set; } = null!; // AirlineBooking, HotelBooking, TourBooking

        [Required]
        public long ReferenceId { get; set; }
    }
}
