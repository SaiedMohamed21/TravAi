using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravAi.Data;

using System.Security.Claims;
using TravAi.DTOs.Common;
using TravAi.DTOs.Auth;
using TravAi.Airline.DTOs.Wallet;
using TravAi.Airline.Models;

namespace TravAi.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/wallet")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TravAi.Services.ICheckoutService _checkoutService;

        public WalletController(ApplicationDbContext context, TravAi.Services.ICheckoutService checkoutService)
        {
            _context = context;
            _checkoutService = checkoutService;
        }

        private long GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return userId;
        }

        // GET: api/users/wallet
        [HttpGet("/api/users/wallet")]
        public async Task<IActionResult> GetWalletDashboard()
        {
            long userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>(null, "User not found"));

            var transactions = await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    ReferenceId = t.ReferenceId
                })
                .ToListAsync();

            return Ok(new ApiResponse<object>(new { Balance = user.WalletBalance, Transactions = transactions }, "Wallet dashboard retrieved successfully"));
        }

        // GET: api/airline/wallet/balance
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            long userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>(null, "User not found"));

            return Ok(new ApiResponse<object>(new { Balance = user.WalletBalance }, "Balance retrieved successfully"));
        }

        // GET: api/airline/wallet/transactions
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            long userId = GetCurrentUserId();
            var transactions = await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    ReferenceId = t.ReferenceId
                })
                .ToListAsync();

            return Ok(new ApiResponse<object>(transactions, "Transactions retrieved successfully"));
        }

        // POST: api/airline/wallet/add-funds
        [HttpPost("add-funds")]
        public async Task<IActionResult> AddFunds([FromBody] AddFundsDto dto)
        {
            if (dto.Amount <= 0)
            {
                return BadRequest(new ApiResponse<object>(null, "Amount must be greater than zero"));
            }

            long userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>(null, "User not found"));

            // Start transaction since we'll update two tables
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                user.WalletBalance += dto.Amount;

                var walletTx = new TravAi.Airline.Models.WalletTransaction
                {
                    UserId = userId,
                    Amount = dto.Amount,
                    Type = "Deposit",
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? "Wallet Top-up" : dto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(walletTx);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<object>(
                    new { Balance = user.WalletBalance, Transaction = walletTx }, 
                    "Funds added successfully"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<object>(null, "An error occurred while adding funds"));
            }
        }

        // POST: /api/users/wallet/topup
        [HttpPost("/api/users/wallet/topup")]
        public async Task<IActionResult> TopupWithStripe([FromBody] TopupRequestDto request)
        {
            if (request.Amount <= 0) return BadRequest(new ApiResponse<string>(false, "Amount must be greater than zero."));

            long userId = GetCurrentUserId();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            try
            {
                var response = await _checkoutService.CreateWalletTopupCheckoutAsync(userId, request.Amount, baseUrl);
                return Ok(new ApiResponse<TravAi.DTOs.Checkout.CheckoutResponse>(response, "Wallet top-up session created."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        // POST: /api/users/wallet/topup/confirm
        [HttpPost("/api/users/wallet/topup/confirm")]
        public async Task<IActionResult> ConfirmTopup([FromQuery] string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return BadRequest(new ApiResponse<string>(false, "Session ID is required."));

            try
            {
                var success = await _checkoutService.ConfirmWalletTopupAsync(session_id);
                if (success)
                {
                    return Ok(new ApiResponse<string>(true, "Wallet top-up confirmed and funds added."));
                }
                return BadRequest(new ApiResponse<string>(false, "Wallet top-up could not be confirmed or is already processed."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpGet("/api/users/wallet/refund-history")]
        public async Task<IActionResult> GetRefundHistory()
        {
            try
            {
                long userId = GetCurrentUserId();
                
                var walletRefundsRaw = await _context.WalletTransactions
                    .Where(w => w.UserId == userId && w.Type == "Refund")
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                var walletRefunds = walletRefundsRaw.Select(w => {
                    string bookingType = "Unknown";
                    string bookingId = "";
                    if (!string.IsNullOrEmpty(w.ReferenceId))
                    {
                        var parts = w.ReferenceId.Split('-');
                        if (parts.Length >= 3 && parts[0] == "Refund")
                        {
                            bookingType = parts[1]; // Hotel, Tour, Airline
                            bookingId = parts[2];
                        }
                    }
                    return new {
                        Date = w.CreatedAt,
                        BookingType = bookingType,
                        BookingId = bookingId,
                        RefundMethod = "Wallet",
                        Amount = w.Amount,
                        Status = "Completed",
                        Description = w.Description
                    };
                }).ToList();

                var stripeRefundsRaw = await _context.PaymentTransactionItems
                    .Include(pti => pti.PaymentTransaction)
                    .Where(pti => pti.PaymentTransaction.UserId == userId && pti.Status == "Refunded")
                    .ToListAsync();

                var stripeRefunds = stripeRefundsRaw.Select(pti => new {
                    Date = pti.CreatedAt,
                    BookingType = pti.BookingType,
                    BookingId = pti.BookingId.ToString(),
                    RefundMethod = "Original Payment Method",
                    Amount = pti.Amount,
                    Status = "Completed",
                    Description = $"Refunded to Stripe for {pti.BookingType} #{pti.BookingId}"
                }).ToList();

                var combined = walletRefunds.Concat(stripeRefunds)
                    .OrderByDescending(r => r.Date)
                    .ToList();

                return Ok(new ApiResponse<object>(combined, "Refund history retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpGet("/api/users/wallet/transaction-history")]
        public async Task<IActionResult> GetTransactionHistory()
        {
            try
            {
                long userId = GetCurrentUserId();

                var topupsRaw = await _context.WalletTransactions
                    .Where(w => w.UserId == userId && (w.Type == "Deposit" || w.Type == "TopUp"))
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                var topups = topupsRaw.Select(w => new {
                    Date = w.CreatedAt,
                    Amount = w.Amount,
                    Method = (w.Description != null && w.Description.Contains("Stripe", StringComparison.OrdinalIgnoreCase)) || (w.ReferenceId != null && w.ReferenceId.StartsWith("cs_")) ? "Stripe" : "Manual",
                    Status = "Completed",
                    ReferenceId = w.ReferenceId ?? "",
                    Description = w.Description
                }).ToList();

                return Ok(new ApiResponse<object>(topups, "Transaction history retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }
    }

    public class TopupRequestDto
    {
        public decimal Amount { get; set; }
    }
}
