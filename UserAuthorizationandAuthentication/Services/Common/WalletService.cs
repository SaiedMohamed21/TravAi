using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravAi.Airline.Models;
using TravAi.Data;

namespace TravAi.Services.Common
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RefundToWalletAsync(long userId, decimal amount, string referenceId, string description)
        {
            if (amount <= 0) return false;

            // Ensure idempotency
            var existingTx = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.UserId == userId && t.ReferenceId == referenceId && t.Type == "Refund");

            if (existingTx != null)
            {
                // Already processed
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            // Using Execution Strategy if configured, or just normal transaction
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Re-check inside transaction for concurrency safety
                    var doubleCheck = await _context.WalletTransactions
                        .FirstOrDefaultAsync(t => t.UserId == userId && t.ReferenceId == referenceId && t.Type == "Refund");
                        
                    if (doubleCheck == null)
                    {
                        user.WalletBalance += amount;

                        var walletTx = new WalletTransaction
                        {
                            UserId = userId,
                            Amount = amount,
                            Type = "Refund",
                            Description = description,
                            ReferenceId = referenceId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.WalletTransactions.Add(walletTx);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return true;
        }
    }
}
