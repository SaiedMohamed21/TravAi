using System.Threading.Tasks;

namespace TravAi.Services.Common
{
    public interface IWalletService
    {
        /// <summary>
        /// Idempotently refunds an amount to a user's wallet.
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="amount">The amount to refund</param>
        /// <param name="referenceId">A unique reference ID to ensure idempotency (e.g. "Refund-Hotel-123")</param>
        /// <param name="description">The description of the transaction</param>
        /// <returns>True if the refund was successfully processed, false if the reference ID was already processed.</returns>
        Task<bool> RefundToWalletAsync(long userId, decimal amount, string referenceId, string description);
    }
}
