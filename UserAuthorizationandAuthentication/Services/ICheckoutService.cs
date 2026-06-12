using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.DTOs.Checkout;
using TravAi.Models;

namespace TravAi.Services
{
    public interface ICheckoutService
    {
        Task<List<PendingCheckoutItemDto>> GetPendingBookingsAsync(long userId);
        Task<CheckoutResponse> CreateAirlineCheckoutAsync(CreateAirlineCheckoutRequest request, string baseUrl);
        Task<CheckoutResponse> CreateHotelTourCheckoutAsync(CreateHotelTourCheckoutRequest request, string baseUrl);
        Task<CheckoutResponse> CreateUnifiedCheckoutAsync(CreateUnifiedCheckoutRequest request, string baseUrl);
        Task<bool> PayWithWalletAsync(CreateUnifiedCheckoutRequest request, long userId);
        Task<CheckoutResponse> CreateWalletTopupCheckoutAsync(long userId, decimal amount, string baseUrl);
        Task<bool> ConfirmWalletTopupAsync(string stripeSessionId);
        Task<object> GetCheckoutSessionDetailsAsync(long id);
        Task<object> ConfirmPaymentAsync(string stripeSessionId);
        Task<bool> HandleStripeWebhookAsync(string jsonPayload, string stripeSignature);
        Task ExpireAndDeleteUnpaidBookingsAsync(long userId);
        Task<object> GetPaymentTransactionDetailsAsync(long paymentTransactionId);
    }
}
