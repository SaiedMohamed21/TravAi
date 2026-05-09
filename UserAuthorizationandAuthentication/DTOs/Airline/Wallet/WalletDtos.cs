using System;

namespace TravAi.Airline.DTOs.Wallet
{
    public class AddFundsDto
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class WalletTransactionDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = null!; // Deposit, Withdrawal, etc.
        public string Description { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }
    }
}


