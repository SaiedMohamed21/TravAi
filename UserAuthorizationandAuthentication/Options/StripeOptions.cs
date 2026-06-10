namespace TravAi.Options
{
    public class StripeOptions
    {
        public const string Stripe = "Stripe";

        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
    }
}
