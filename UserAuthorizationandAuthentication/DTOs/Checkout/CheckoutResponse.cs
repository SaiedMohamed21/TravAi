namespace TravAi.DTOs.Checkout
{
    public class CheckoutResponse
    {
        public string CheckoutUrl { get; set; } = null!;
        public string CheckoutSessionId { get; set; } = null!;
        public string? StripeSessionId { get; set; }
        public string? StripePublicKey { get; set; }
    }
}
