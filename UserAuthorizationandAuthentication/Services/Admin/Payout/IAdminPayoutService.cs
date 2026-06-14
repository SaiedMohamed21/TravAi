using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.Models.Admin;
using TravAi.Models.Admin.DTOs;

namespace TravAi.Services.Admin.Payout
{
    public interface IAdminPayoutService
    {
        Task<List<PayoutBatchDto>> GenerateWeeklyPayoutsAsync(long adminUserId, GenerateWeeklyPayoutsRequestDto request);
        Task<List<PayoutBatchDto>> GetPayoutBatchesAsync(ProviderType? providerType, string? status, DateTime? weekStart, DateTime? weekEnd, int? month, int? year);
        Task<PayoutBatchDetailDto?> GetPayoutBatchDetailsAsync(long id);
        Task<PayoutBatchDto> ConfirmPayoutAsync(long id, long adminUserId, ConfirmPayoutRequestDto request);
        Task<PayoutBatchDto> MarkPayoutFailedAsync(long id, long adminUserId, MarkPayoutFailedRequestDto request);
        Task<PayoutSummaryDto> GetPayoutSummaryAsync();
        Task<(bool skipped, string? checkoutUrl, string message)> CreatePayoutStripeSessionAsync(long id, long adminUserId, string successUrl, string cancelUrl);
        Task<PayoutBatchDto> VerifyPayoutStripePaymentAsync(long id, string sessionId, long adminUserId);
        Task<PayoutStripePaymentReceiptDto?> GetPayoutStripePaymentAsync(long id);
        Task<(bool CanSee, List<string> AvailableAccounts, string? ErrorMessage)> TestStripeConnectVisibilityAsync(string destinationAccount);
    }
}
