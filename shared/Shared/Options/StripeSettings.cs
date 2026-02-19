namespace Shared.Options
{
    public class StripeSettings
    {
        public string? ApiKey { get; set; }
        public string? RefreshUrl { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CheckoutSuccessUrl { get; set; }
        public string? CheckoutCancelUrl { get; set; }
        public string? WebhookSecret { get; set; }
    }
}
